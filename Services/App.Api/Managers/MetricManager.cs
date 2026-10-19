using System.Globalization;
using App.Api.Accessors;
using App.Api.Utilities;
using App.ServiceInvoker.Attributes;
using App.ServiceInvoker.Interfaces;
using Contracts.Domain.Database;
using Contracts.Dto;

namespace App.Api.Managers
{
    /// <summary>
    /// 日指标写入与查询。当前只做手工录入；接口同步（平台 API / CSV）留到 M1-4 与 M4，
    /// 但落库时已经把 Source / RecordedBy 写入，后续两种来源可以共存且可区分。
    /// </summary>
    public class MetricManager : IManagerService
    {
        private const string DateFormat = "yyyy-MM-dd";
        private const int DefaultTake = 20;
        private const int MaxTake = 100;

        private readonly IDatabaseAccessor _databaseAccessor;
        private readonly UserContextService _userContextService;

        public MetricManager(IDatabaseAccessor databaseAccessor, UserContextService userContextService)
        {
            _databaseAccessor = databaseAccessor;
            _userContextService = userContextService;
        }

        /// <summary>
        /// 单日手工录入。同一账号同一天重复提交按覆盖处理，不会产生重复行，
        /// 并把 RecordedBy 更新为本次录入人，保证「谁改的」可回溯。
        /// </summary>
        [RequireAuthenticated]
        public async Task<MetricIngestResultDto> IngestManual(IngestManualMetricsRequestDto request)
        {
            if (request == null || request.AccountId == Guid.Empty)
            {
                return Fail("缺少账号 ID。");
            }

            if (!DateOnly.TryParseExact(request.StatDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var statDate))
            {
                return Fail($"日期格式应为 yyyy-MM-dd，收到「{request.StatDate}」。");
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (statDate > today)
            {
                return Fail("不能录入未来日期的数据。");
            }

            if (statDate < today.AddYears(-2))
            {
                return Fail("只接受近两年的数据，更早的记录请走历史导入。");
            }

            if (request.Views < 0 || request.Likes < 0 || request.Comments < 0 || request.Shares < 0)
            {
                return Fail("播放、点赞、评论、转发都不能为负。");
            }

            if (request.Likes + request.Comments + request.Shares > request.Views && request.Views > 0)
            {
                return Fail("互动数之和不能超过播放数，请检查是否填错了列。");
            }

            var account = await FindVisibleAccount(request.AccountId);
            if (account == null)
            {
                return Fail("账号不存在，或不属于当前用户。");
            }

            if (account.Status == PlatformAccountStatus.Paused)
            {
                return Fail($"「{account.DisplayName}」已停用，不能继续录入。");
            }

            var existing = await _databaseAccessor
                .GetDocumentByProperty<MetricDaily>(metric => metric.AccountId == account.Id && metric.StatDate == statDate);

            var recordedBy = _userContextService.UserEmail
                             ?? _userContextService.UserId
                             ?? "unknown";

            var updated = existing != null;
            var document = existing ?? new MetricDaily
            {
                AccountId = account.Id,
                StatDate = statDate
            };

            document.Platform = account.Platform;
            document.Views = request.Views;
            document.Likes = request.Likes;
            document.Comments = request.Comments;
            document.Shares = request.Shares;
            document.NewFollowers = request.NewFollowers;
            document.Source = MetricSource.Manual;
            document.RecordedBy = recordedBy;
            document.RecordedAt = DateTimeOffset.UtcNow;

            if (updated)
            {
                await _databaseAccessor.UpsertDocument(document);
            }
            else
            {
                await _databaseAccessor.InsertDocument(document);
            }

            return new MetricIngestResultDto
            {
                Success = true,
                Updated = updated,
                Message = updated
                    ? $"{account.DisplayName} {statDate:yyyy-MM-dd} 的记录已覆盖。"
                    : $"{account.DisplayName} {statDate:yyyy-MM-dd} 的记录已保存。",
                Entry = ToEntry(document, account)
            };
        }

        /// <summary>
        /// 最近录入记录，用于核对「这个数字是谁哪天录的」。
        /// 只返回当前用户可见账号的数据。
        /// </summary>
        [RequireAuthenticated]
        public async Task<MetricEntryListDto> ListRecentMetrics(ListRecentMetricsRequestDto request)
        {
            var accounts = await LoadVisibleAccounts();
            var accountMap = accounts.ToDictionary(account => account.Id);
            var filterId = request?.AccountId ?? Guid.Empty;

            if (filterId != Guid.Empty && !accountMap.ContainsKey(filterId))
            {
                return new MetricEntryListDto();
            }

            var take = Math.Clamp(request?.Take ?? DefaultTake, 1, MaxTake);
            var metrics = (await _databaseAccessor.GetAllDocuments<MetricDaily>()).ToList();

            return new MetricEntryListDto
            {
                Items = metrics
                    .Where(metric => accountMap.ContainsKey(metric.AccountId))
                    .Where(metric => filterId == Guid.Empty || metric.AccountId == filterId)
                    .OrderByDescending(metric => metric.StatDate)
                    .ThenByDescending(metric => metric.RecordedAt)
                    .Take(take)
                    .Select(metric => ToEntry(metric, accountMap[metric.AccountId]))
                    .ToList()
            };
        }

        private async Task<PlatformAccount?> FindVisibleAccount(Guid accountId)
        {
            var account = await _databaseAccessor.GetDocumentById<PlatformAccount>(accountId);
            if (account == null) return null;

            return AccountVisibility.IsVisible(account.OwnerId, _userContextService.UserId) ? account : null;
        }

        private async Task<List<PlatformAccount>> LoadVisibleAccounts()
        {
            var userId = _userContextService.UserId;
            var all = (await _databaseAccessor.GetAllDocuments<PlatformAccount>()).ToList();
            return all.Where(account => AccountVisibility.IsVisible(account.OwnerId, userId)).ToList();
        }

        private static MetricEntryDto ToEntry(MetricDaily metric, PlatformAccount account) =>
            new()
            {
                Id = metric.Id,
                AccountId = metric.AccountId,
                Platform = metric.Platform,
                AccountName = account.DisplayName,
                StatDate = metric.StatDate.ToString(DateFormat),
                Views = metric.Views,
                Likes = metric.Likes,
                Comments = metric.Comments,
                Shares = metric.Shares,
                NewFollowers = metric.NewFollowers,
                Source = metric.Source,
                RecordedBy = metric.RecordedBy,
                RecordedAt = metric.RecordedAt.ToString("yyyy-MM-dd HH:mm")
            };

        private static MetricIngestResultDto Fail(string message) =>
            new() { Success = false, Message = message };
    }
}
