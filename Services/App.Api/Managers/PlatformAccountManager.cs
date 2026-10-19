using App.Api.Accessors;
using App.Api.Utilities;
using App.ServiceInvoker.Attributes;
using App.ServiceInvoker.Interfaces;
using Contracts.Domain.Database;
using Contracts.Dto;

namespace App.Api.Managers
{
    /// <summary>
    /// 平台账号的增删改查。当前只支持手工模式（Status = manual / connected / paused），
    /// OAuth 授权与 token 管理留到 M4，因此返回的 DTO 里没有任何密钥字段。
    /// </summary>
    public class PlatformAccountManager : IManagerService
    {
        private readonly IDatabaseAccessor _databaseAccessor;
        private readonly UserContextService _userContextService;

        public PlatformAccountManager(IDatabaseAccessor databaseAccessor, UserContextService userContextService)
        {
            _databaseAccessor = databaseAccessor;
            _userContextService = userContextService;
        }

        [RequireAuthenticated]
        public async Task<PlatformAccountListDto> ListAccounts(ListAccountsRequestDto request)
        {
            var accounts = await LoadVisibleAccounts();
            var metrics = (await _databaseAccessor.GetAllDocuments<MetricDaily>()).ToList();
            var recentStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-6);

            var result = accounts
                .Where(account => request?.IncludePaused == true || account.Status != PlatformAccountStatus.Paused)
                .Select(account =>
                {
                    var own = metrics.Where(metric => metric.AccountId == account.Id).ToList();
                    return ToDto(
                        account,
                        own.Count == 0 ? null : own.Max(metric => metric.StatDate),
                        own.Count(metric => metric.StatDate >= recentStart && metric.StatDate <= DateOnly.FromDateTime(DateTime.UtcNow)));
                })
                .OrderBy(dto => dto.Platform)
                .ThenBy(dto => dto.DisplayName)
                .ToList();

            return new PlatformAccountListDto { Items = result };
        }

        /// <summary>
        /// 新增或编辑账号。校验失败返回 Success = false + Message，不抛异常，
        /// 前端可以把原因直接显示在表单下方。
        /// </summary>
        [RequireAuthenticated]
        public async Task<SaveAccountResultDto> SaveAccount(SaveAccountRequestDto request)
        {
            if (request == null)
            {
                return Fail("请求内容为空。");
            }

            var platform = PlatformCatalog.Normalize(request.Platform ?? string.Empty);
            if (!PlatformCatalog.IsSupported(platform))
            {
                return Fail($"不支持的平台标识「{request.Platform}」。");
            }

            var handle = (request.Handle ?? string.Empty).Trim();
            if (handle.Length == 0)
            {
                return Fail("账号标识不能为空。");
            }

            var displayName = (request.DisplayName ?? string.Empty).Trim();
            if (displayName.Length == 0)
            {
                return Fail("展示名不能为空。");
            }

            if (request.FollowerCount < 0)
            {
                return Fail("粉丝数不能为负。");
            }

            var status = string.IsNullOrWhiteSpace(request.Status)
                ? PlatformAccountStatus.Manual
                : request.Status.Trim();
            if (!PlatformCatalog.IsSupportedStatus(status))
            {
                return Fail($"不支持的接入状态「{request.Status}」。");
            }

            var userId = _userContextService.UserId ?? string.Empty;
            var accounts = await LoadVisibleAccounts();

            var duplicated = accounts.Any(account =>
                account.Id != request.Id &&
                string.Equals(account.Platform, platform, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(account.Handle, handle, StringComparison.OrdinalIgnoreCase));
            if (duplicated)
            {
                return Fail($"该平台上已存在账号「{handle}」，请直接编辑原账号。");
            }

            PlatformAccount account;
            if (request.Id.HasValue && request.Id.Value != Guid.Empty)
            {
                var existing = accounts.FirstOrDefault(item => item.Id == request.Id.Value);
                if (existing == null)
                {
                    return Fail("账号不存在，或不属于当前用户。");
                }

                account = existing;

                if (!string.Equals(account.Platform, platform, StringComparison.OrdinalIgnoreCase))
                {
                    var hasMetrics = (await _databaseAccessor
                        .GetDocumentsByProperty<MetricDaily>(metric => metric.AccountId == account.Id, 0, 1)).Any();
                    if (hasMetrics)
                    {
                        return Fail("该账号已有日记录，不能修改平台；请新建账号并停用旧账号。");
                    }
                }
            }
            else
            {
                account = new PlatformAccount
                {
                    OwnerId = userId,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                accounts.Add(account);
            }

            account.Platform = platform;
            account.Handle = handle;
            account.DisplayName = displayName;
            account.Status = status;
            account.FollowerCount = request.FollowerCount;
            account.UpdatedAt = DateTimeOffset.UtcNow;

            await _databaseAccessor.UpsertDocument(account);

            var metrics = (await _databaseAccessor
                .GetDocumentsByProperty<MetricDaily>(metric => metric.AccountId == account.Id)).ToList();
            var lastDate = metrics.Count == 0 ? (DateOnly?)null : metrics.Max(metric => metric.StatDate);

            return new SaveAccountResultDto
            {
                Success = true,
                Message = request.Id.HasValue ? "账号已更新。" : "账号已创建，可以开始录入数据。",
                Account = ToDto(account, lastDate, metrics.Count)
            };
        }

        /// <summary>
        /// 停用账号：不做物理删除，历史日记录仍然保留，首页仍可回溯。
        /// </summary>
        [RequireAuthenticated]
        public async Task<OperationResultDto> DisableAccount(AccountIdRequestDto request)
        {
            if (request == null || request.AccountId == Guid.Empty)
            {
                return new OperationResultDto { Success = false, Message = "缺少账号 ID。" };
            }

            var accounts = await LoadVisibleAccounts();
            var account = accounts.FirstOrDefault(item => item.Id == request.AccountId);
            if (account == null)
            {
                return new OperationResultDto { Success = false, Message = "账号不存在，或不属于当前用户。" };
            }

            if (account.Status == PlatformAccountStatus.Paused)
            {
                return new OperationResultDto { Success = true, Message = "账号已处于停用状态。" };
            }

            account.Status = PlatformAccountStatus.Paused;
            account.UpdatedAt = DateTimeOffset.UtcNow;
            await _databaseAccessor.UpsertDocument(account);

            return new OperationResultDto { Success = true, Message = $"已停用「{account.DisplayName}」，历史记录保留。" };
        }

        private async Task<List<PlatformAccount>> LoadVisibleAccounts()
        {
            var userId = _userContextService.UserId;
            var all = (await _databaseAccessor.GetAllDocuments<PlatformAccount>()).ToList();
            return all.Where(account => AccountVisibility.IsVisible(account.OwnerId, userId)).ToList();
        }

        private static PlatformAccountDto ToDto(PlatformAccount account, DateOnly? lastMetricDate, int recentMetricCount)
        {
            return new PlatformAccountDto
            {
                Id = account.Id,
                Platform = account.Platform,
                Handle = account.Handle,
                DisplayName = account.DisplayName,
                Status = account.Status,
                FollowerCount = account.FollowerCount,
                OwnerId = account.OwnerId,
                LastMetricDate = lastMetricDate?.ToString("yyyy-MM-dd"),
                RecentMetricCount = recentMetricCount,
                CreatedAt = account.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                UpdatedAt = account.UpdatedAt.ToString("yyyy-MM-dd HH:mm")
            };
        }

        private static SaveAccountResultDto Fail(string message) =>
            new() { Success = false, Message = message };
    }
}
