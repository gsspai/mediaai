using System.Globalization;
using App.Api.Accessors;
using App.Api.Utilities;
using App.Api.Utilities.Roles;
using App.ServiceInvoker.Attributes;
using App.ServiceInvoker.Interfaces;
using Contracts.Domain.Database;
using Contracts.Dto;

namespace App.Api.Managers
{
    /// <summary>
    /// 商务合作单。按权限矩阵：管理员与商务可增改，财务只做报价复核（推进 / 退回），
    /// 其他角色只能看。飞书审批回调接入后，阶段推进改由回调触发，这里保留手工兜底。
    /// </summary>
    public class DealManager : IManagerService
    {
        private const int DefaultTake = 50;
        private const int MaxTake = 200;

        private readonly IDatabaseAccessor _databaseAccessor;
        private readonly UserContextService _userContextService;

        public DealManager(IDatabaseAccessor databaseAccessor, UserContextService userContextService)
        {
            _databaseAccessor = databaseAccessor;
            _userContextService = userContextService;
        }

        [RequireAuthenticated]
        public async Task<DealListDto> ListDeals(DealListRequestDto request)
        {
            var deals = (await _databaseAccessor.GetAllDocuments<Deal>()).ToList();
            var stage = request?.Stage?.Trim();
            var keyword = request?.Keyword?.Trim();
            var take = Math.Clamp(request?.Take ?? DefaultTake, 1, MaxTake);

            var query = deals.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(stage))
            {
                query = query.Where(deal => string.Equals(deal.Stage, stage, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(deal =>
                    Contains(deal.ClientName, keyword) ||
                    Contains(deal.Title, keyword) ||
                    Contains(deal.Contact, keyword) ||
                    Contains(deal.OwnerName, keyword));
            }

            var items = query
                .OrderBy(deal => ExecutionMapper.DealPriority(deal.Stage))
                .ThenByDescending(deal => deal.UpdatedAt)
                .Take(take)
                .Select(ExecutionMapper.DealToDto)
                .ToList();

            return new DealListDto { Items = items, Total = deals.Count };
        }

        [RequireAuthenticated]
        public async Task<DealDto?> GetDeal(DealByIdRequestDto request)
        {
            if (request == null || request.DealId == Guid.Empty) return null;
            var deal = await _databaseAccessor.GetDocumentById<Deal>(request.DealId);
            return deal == null ? null : ExecutionMapper.DealToDto(deal);
        }

        [RequireRole(Roles.Admin, Roles.Sales)]
        public async Task<SaveDealResultDto> SaveDeal(SaveDealRequestDto request)
        {
            if (request == null)
            {
                return Fail("请求内容为空。");
            }

            var clientName = (request.ClientName ?? string.Empty).Trim();
            if (clientName.Length == 0)
            {
                return Fail("客户名不能为空。");
            }

            var title = (request.Title ?? string.Empty).Trim();
            if (title.Length == 0) title = clientName;

            if (request.Amount < 0)
            {
                return Fail("报价金额不能为负。");
            }

            var stage = (request.Stage ?? string.Empty).Trim();
            if (stage.Length > 0 && !DealStage.IsValid(stage))
            {
                return Fail($"不支持的阶段「{request.Stage}」。");
            }

            DateOnly? expectedCloseAt = null;
            if (!string.IsNullOrWhiteSpace(request.ExpectedCloseAt))
            {
                if (!DateOnly.TryParseExact(request.ExpectedCloseAt.Trim(), "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    return Fail("预计签约日期格式应为 yyyy-MM-dd。");
                }

                expectedCloseAt = parsed;
            }

            var email = _userContextService.UserEmail ?? string.Empty;
            Deal deal;

            if (request.Id.HasValue && request.Id.Value != Guid.Empty)
            {
                var existing = await _databaseAccessor.GetDocumentById<Deal>(request.Id.Value);
                if (existing == null)
                {
                    return Fail("商务单不存在。");
                }

                deal = existing;
            }
            else
            {
                deal = new Deal
                {
                    Id = Guid.NewGuid(),
                    Stage = stage.Length > 0 ? stage : DealStage.Lead,
                    CreatedBy = email,
                    CreatedAt = DateTimeOffset.UtcNow
                };
            }

            deal.ClientName = clientName;
            deal.Title = title;
            deal.Contact = (request.Contact ?? string.Empty).Trim();
            deal.Amount = request.Amount;
            deal.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "CNY" : request.Currency.Trim();
            deal.OwnerName = string.IsNullOrWhiteSpace(request.OwnerName) ? DefaultOwnerName() : request.OwnerName.Trim();
            deal.OwnerAuthId = string.IsNullOrWhiteSpace(deal.OwnerAuthId) ? email : deal.OwnerAuthId;
            deal.NextStep = string.IsNullOrWhiteSpace(request.NextStep)
                ? DealStage.DefaultNextStep(deal.Stage)
                : request.NextStep.Trim();
            deal.ExpectedCloseAt = expectedCloseAt;
            deal.Notes = (request.Notes ?? string.Empty).Trim();
            deal.UpdatedAt = DateTimeOffset.UtcNow;

            await _databaseAccessor.UpsertDocument(deal);

            return new SaveDealResultDto
            {
                Success = true,
                Message = request.Id.HasValue ? "商务单已更新。" : "商务单已创建。",
                Deal = ExecutionMapper.DealToDto(deal)
            };
        }

        /// <summary>
        /// 推进阶段。管理员 / 商务按正常顺序推进或标记丢失；财务只做报价复核：
        /// 待审核 → 已确认（通过），待审核 → 报价中（退回）。
        /// </summary>
        [RequireRole(Roles.Admin, Roles.Sales, Roles.Finance)]
        public async Task<UpdateDealStageResultDto> UpdateDealStage(UpdateDealStageRequestDto request)
        {
            if (request == null || request.DealId == Guid.Empty)
            {
                return FailStage("缺少商务单 ID。");
            }

            var deal = await _databaseAccessor.GetDocumentById<Deal>(request.DealId);
            if (deal == null)
            {
                return FailStage("商务单不存在。");
            }

            var target = (request.Stage ?? string.Empty).Trim();
            var roles = _userContextService.UserRoles ?? new List<string>();
            var financeOnly = !roles.Contains(Roles.Admin) && !roles.Contains(Roles.Sales) &&
                              roles.Contains(Roles.Finance);

            if (financeOnly)
            {
                var allowed = deal.Stage == DealStage.PricingReview &&
                              (target == DealStage.Confirmed || target == DealStage.Quoting);
                if (!allowed)
                {
                    return FailStage("财务只能复核报价：待报价审核 → 已确认，或退回报价中。");
                }
            }
            else
            {
                var error = DealStage.ValidateTransition(deal.Stage, target);
                if (error != null)
                {
                    return FailStage(error);
                }
            }

            var from = deal.Stage;
            deal.Stage = target;
            deal.NextStep = string.IsNullOrWhiteSpace(request.NextStep)
                ? DealStage.DefaultNextStep(target)
                : request.NextStep.Trim();
            deal.UpdatedAt = DateTimeOffset.UtcNow;

            await _databaseAccessor.UpsertDocument(deal);

            return new UpdateDealStageResultDto
            {
                Success = true,
                Message = $"阶段已从「{DealStageText.Label(from)}」推进到「{DealStageText.Label(target)}」。",
                Deal = ExecutionMapper.DealToDto(deal)
            };
        }

        private static bool Contains(string? source, string keyword) =>
            !string.IsNullOrEmpty(source) && source.Contains(keyword, StringComparison.OrdinalIgnoreCase);

        private string DefaultOwnerName()
        {
            var email = _userContextService.UserEmail ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email)) return "未指定";
            var at = email.IndexOf('@');
            return at > 0 ? email[..at] : email;
        }

        private static SaveDealResultDto Fail(string message) => new() { Success = false, Message = message };

        private static UpdateDealStageResultDto FailStage(string message) => new() { Success = false, Message = message };
    }
}
