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
    /// 内容计划。按权限矩阵：管理员、策划、运营、剪辑可增改，商务与分析只能看。
    /// 状态只能顺着 选题 → 评分 → 待制作 → 已发布 推进，或从任意状态归档。
    /// </summary>
    public class ContentPlanManager : IManagerService
    {
        private const int DefaultTake = 50;
        private const int MaxTake = 200;

        private readonly IDatabaseAccessor _databaseAccessor;
        private readonly UserContextService _userContextService;

        public ContentPlanManager(IDatabaseAccessor databaseAccessor, UserContextService userContextService)
        {
            _databaseAccessor = databaseAccessor;
            _userContextService = userContextService;
        }

        [RequireAuthenticated]
        public async Task<ContentPlanListDto> ListPlans(PlanListRequestDto request)
        {
            var plans = (await _databaseAccessor.GetAllDocuments<ContentPlan>()).ToList();
            var status = request?.Status?.Trim();
            var keyword = request?.Keyword?.Trim();
            var take = Math.Clamp(request?.Take ?? DefaultTake, 1, MaxTake);

            var query = plans.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(plan => string.Equals(plan.Status, status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(plan =>
                    Contains(plan.Title, keyword) ||
                    Contains(plan.Direction, keyword) ||
                    Contains(plan.AssigneeName, keyword));
            }

            var dealTitles = await LoadDealTitlesAsync();
            var items = query
                .OrderBy(plan => ExecutionMapper.PlanPriority(plan.Status))
                .ThenBy(plan => plan.PlannedAt ?? DateOnly.MaxValue)
                .ThenByDescending(plan => plan.UpdatedAt)
                .Take(take)
                .Select(plan => ExecutionMapper.PlanToDto(plan, ResolveDealTitle(plan, dealTitles)))
                .ToList();

            return new ContentPlanListDto { Items = items, Total = plans.Count };
        }

        [RequireAuthenticated]
        public async Task<ContentPlanDto?> GetPlan(PlanByIdRequestDto request)
        {
            if (request == null || request.PlanId == Guid.Empty) return null;
            var plan = await _databaseAccessor.GetDocumentById<ContentPlan>(request.PlanId);
            if (plan == null) return null;

            var dealTitles = await LoadDealTitlesAsync();
            return ExecutionMapper.PlanToDto(plan, ResolveDealTitle(plan, dealTitles));
        }

        [RequireRole(Roles.Admin, Roles.Planner, Roles.Operations, Roles.Editor)]
        public async Task<SavePlanResultDto> SavePlan(SavePlanRequestDto request)
        {
            if (request == null)
            {
                return Fail("请求内容为空。");
            }

            var title = (request.Title ?? string.Empty).Trim();
            if (title.Length == 0)
            {
                return Fail("计划名不能为空。");
            }

            if (request.Score.HasValue && (request.Score.Value < 0 || request.Score.Value > 10))
            {
                return Fail("机会分必须在 0 到 10 之间。");
            }

            var platforms = new List<string>();
            foreach (var raw in request.Platforms ?? new List<string>())
            {
                var platform = PlatformCatalog.Normalize(raw ?? string.Empty);
                if (platform.Length == 0) continue;
                if (!PlatformCatalog.IsSupported(platform))
                {
                    return Fail($"不支持的平台标识「{raw}」。");
                }

                if (!platforms.Contains(platform)) platforms.Add(platform);
            }

            DateOnly? plannedAt = null;
            if (!string.IsNullOrWhiteSpace(request.PlannedAt))
            {
                if (!DateOnly.TryParseExact(request.PlannedAt.Trim(), "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    return Fail("排期日期格式应为 yyyy-MM-dd。");
                }

                plannedAt = parsed;
            }

            if (request.DealId.HasValue && request.DealId.Value != Guid.Empty)
            {
                var deal = await _databaseAccessor.GetDocumentById<Deal>(request.DealId.Value);
                if (deal == null)
                {
                    return Fail("关联的商务单不存在。");
                }
            }

            var email = _userContextService.UserEmail ?? string.Empty;
            ContentPlan plan;

            if (request.Id.HasValue && request.Id.Value != Guid.Empty)
            {
                var existing = await _databaseAccessor.GetDocumentById<ContentPlan>(request.Id.Value);
                if (existing == null)
                {
                    return Fail("内容计划不存在。");
                }

                plan = existing;
            }
            else
            {
                plan = new ContentPlan
                {
                    Id = Guid.NewGuid(),
                    CreatedBy = email,
                    CreatedAt = DateTimeOffset.UtcNow
                };
            }

            plan.Title = title;
            plan.Platforms = platforms;
            plan.PlannedAt = plannedAt;
            plan.Direction = (request.Direction ?? string.Empty).Trim();
            plan.Score = request.Score;
            plan.AssigneeName = string.IsNullOrWhiteSpace(request.AssigneeName)
                ? DefaultAssigneeName()
                : request.AssigneeName.Trim();
            plan.DealId = request.DealId.HasValue && request.DealId.Value != Guid.Empty ? request.DealId : null;
            plan.Link = (request.Link ?? string.Empty).Trim();
            plan.UpdatedAt = DateTimeOffset.UtcNow;

            await _databaseAccessor.UpsertDocument(plan);

            var dealTitles = await LoadDealTitlesAsync();
            return new SavePlanResultDto
            {
                Success = true,
                Message = request.Id.HasValue ? "内容计划已更新。" : "内容计划已创建。",
                Plan = ExecutionMapper.PlanToDto(plan, ResolveDealTitle(plan, dealTitles))
            };
        }

        [RequireRole(Roles.Admin, Roles.Planner, Roles.Operations, Roles.Editor)]
        public async Task<UpdatePlanStatusResultDto> UpdatePlanStatus(UpdatePlanStatusRequestDto request)
        {
            if (request == null || request.PlanId == Guid.Empty)
            {
                return FailStatus("缺少内容计划 ID。");
            }

            var plan = await _databaseAccessor.GetDocumentById<ContentPlan>(request.PlanId);
            if (plan == null)
            {
                return FailStatus("内容计划不存在。");
            }

            var target = (request.Status ?? string.Empty).Trim();
            var error = ContentPlanStatus.ValidateTransition(plan.Status, target);
            if (error != null)
            {
                return FailStatus(error);
            }

            var from = plan.Status;
            plan.Status = target;
            plan.UpdatedAt = DateTimeOffset.UtcNow;

            await _databaseAccessor.UpsertDocument(plan);

            var dealTitles = await LoadDealTitlesAsync();
            return new UpdatePlanStatusResultDto
            {
                Success = true,
                Message = $"状态已从「{PlanStatusText.Label(from)}」推进到「{PlanStatusText.Label(target)}」。",
                Plan = ExecutionMapper.PlanToDto(plan, ResolveDealTitle(plan, dealTitles))
            };
        }

        private async Task<Dictionary<Guid, string>> LoadDealTitlesAsync()
        {
            var deals = await _databaseAccessor.GetAllDocuments<Deal>();
            return deals.ToDictionary(
                deal => deal.Id,
                deal => string.IsNullOrWhiteSpace(deal.Title) ? deal.ClientName : deal.Title);
        }

        private static string ResolveDealTitle(ContentPlan plan, IReadOnlyDictionary<Guid, string> dealTitles) =>
            plan.DealId.HasValue && dealTitles.TryGetValue(plan.DealId.Value, out var title) ? title : string.Empty;

        private static bool Contains(string? source, string keyword) =>
            !string.IsNullOrEmpty(source) && source.Contains(keyword, StringComparison.OrdinalIgnoreCase);

        private string DefaultAssigneeName()
        {
            var email = _userContextService.UserEmail ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email)) return "未指定";
            var at = email.IndexOf('@');
            return at > 0 ? email[..at] : email;
        }

        private static SavePlanResultDto Fail(string message) => new() { Success = false, Message = message };

        private static UpdatePlanStatusResultDto FailStatus(string message) => new() { Success = false, Message = message };
    }
}
