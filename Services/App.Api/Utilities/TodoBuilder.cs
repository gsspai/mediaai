using Contracts.Domain.Database;
using Contracts.Dto;

namespace App.Api.Utilities
{
    /// <summary>
    /// 跨角色待办的统一装配：人工待办来自 TodoItem，自动待办由商务单阶段与内容计划推导。
    /// 自动待办不落库、不能勾选，底层条件修好后自然消失，避免「勾掉了但问题还在」。
    /// 首页与待办页共用这里，保证两处条数与内容一致。
    /// </summary>
    public static class TodoBuilder
    {
        public const int DefaultTake = 20;

        public static List<TodoDto> Build(
            IEnumerable<TodoItem> stored,
            IEnumerable<Deal> deals,
            IEnumerable<ContentPlan> plans,
            bool includeDone,
            int take)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dealList = deals.ToList();
            var planList = plans.ToList();

            var items = new List<TodoDto>();
            items.AddRange(BuildDerived(dealList, planList, today));
            items.AddRange(stored
                .Where(todo => includeDone || !string.Equals(todo.Status, TodoStatus.Done, StringComparison.OrdinalIgnoreCase))
                .Select(ToDto));

            return items
                .OrderBy(item => SeverityRank(item))
                .ThenBy(item => item.DueDate ?? "9999-12-31", StringComparer.Ordinal)
                .ThenBy(item => item.Title, StringComparer.Ordinal)
                .Take(Math.Clamp(take <= 0 ? DefaultTake : take, 1, 200))
                .ToList();
        }

        /// <summary>
        /// 首页只取前 N 条，且固定显示未完成的。
        /// </summary>
        public static List<TodoDto> BuildTop(
            IEnumerable<TodoItem> stored,
            IEnumerable<Deal> deals,
            IEnumerable<ContentPlan> plans,
            int take) => Build(stored, deals, plans, false, take);

        private static IEnumerable<TodoDto> BuildDerived(List<Deal> deals, List<ContentPlan> plans, DateOnly today)
        {
            var pricingReview = deals.Where(deal => deal.Stage == DealStage.PricingReview).ToList();
            if (pricingReview.Count > 0)
            {
                yield return new TodoDto
                {
                    Key = "auto-pricing-review",
                    Auto = true,
                    Title = $"{pricingReview.Count} 条商务单等待财务复核报价",
                    OwnerRole = "finance",
                    OwnerRoleLabel = RoleText.Label("finance"),
                    Urgency = "今日处理",
                    Tone = "warning",
                    Href = "/business?stage=pricing-review",
                    Status = TodoStatus.Open,
                    CreatedAt = pricingReview.Max(deal => deal.UpdatedAt).ToString("yyyy-MM-dd HH:mm")
                };
            }

            var staleQuoting = deals
                .Where(deal => deal.Stage == DealStage.Quoting && deal.UpdatedAt < DateTimeOffset.UtcNow.AddDays(-3))
                .ToList();
            if (staleQuoting.Count > 0)
            {
                yield return new TodoDto
                {
                    Key = "auto-quoting-stale",
                    Auto = true,
                    Title = $"{staleQuoting.Count} 条商务单报价超过 3 天未提交复核",
                    OwnerRole = "sales",
                    OwnerRoleLabel = RoleText.Label("sales"),
                    Urgency = "需跟进",
                    Tone = "warning",
                    Href = "/business?stage=quoting",
                    Status = TodoStatus.Open,
                    CreatedAt = staleQuoting.Min(deal => deal.UpdatedAt).ToString("yyyy-MM-dd HH:mm")
                };
            }

            var missingDirection = plans
                .Where(plan => string.IsNullOrWhiteSpace(plan.Direction) &&
                               plan.Status != ContentPlanStatus.Published &&
                               plan.Status != ContentPlanStatus.Archived)
                .ToList();
            if (missingDirection.Count > 0)
            {
                yield return new TodoDto
                {
                    Key = "auto-plan-direction",
                    Auto = true,
                    Title = $"{missingDirection.Count} 条内容计划缺少方向说明",
                    OwnerRole = "planner",
                    OwnerRoleLabel = RoleText.Label("planner"),
                    Urgency = "需补全",
                    Tone = "warning",
                    Href = $"/plans/{missingDirection[0].Id}",
                    Status = TodoStatus.Open,
                    CreatedAt = missingDirection[0].CreatedAt.ToString("yyyy-MM-dd HH:mm")
                };
            }

            var overduePlans = plans
                .Where(plan => plan.Status == ContentPlanStatus.Scheduled &&
                               plan.PlannedAt.HasValue &&
                               plan.PlannedAt.Value < today)
                .ToList();
            if (overduePlans.Count > 0)
            {
                yield return new TodoDto
                {
                    Key = "auto-plan-overdue",
                    Auto = true,
                    Title = $"{overduePlans.Count} 条内容计划已过排期仍未发布",
                    OwnerRole = "operations",
                    OwnerRoleLabel = RoleText.Label("operations"),
                    Urgency = "已逾期",
                    Tone = "destructive",
                    Href = $"/plans/{overduePlans[0].Id}",
                    Status = TodoStatus.Open,
                    CreatedAt = overduePlans.Min(plan => plan.UpdatedAt).ToString("yyyy-MM-dd HH:mm")
                };
            }
        }

        private static TodoDto ToDto(TodoItem todo) => ExecutionMapper.TodoToDto(todo);

        /// <summary>
        /// 排序权重：逾期 > 今日 > 高优先 > 普通。自动待办本身就是待处理，排在同级前面。
        /// </summary>
        private static int SeverityRank(TodoDto item)
        {
            if (string.Equals(item.Urgency, "已逾期", StringComparison.Ordinal)) return 0;
            if (string.Equals(item.Urgency, "今日处理", StringComparison.Ordinal)) return 1;
            if (string.Equals(item.Urgency, "需补全", StringComparison.Ordinal)) return 1;
            if (string.Equals(item.Urgency, "需跟进", StringComparison.Ordinal)) return 2;
            if (string.Equals(item.Urgency, "高优先", StringComparison.Ordinal)) return 2;
            return item.Auto ? 2 : 3;
        }
    }
}
