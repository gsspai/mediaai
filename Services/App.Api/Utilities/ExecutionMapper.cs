using Contracts.Domain.Database;
using Contracts.Dto;

namespace App.Api.Utilities
{
    /// <summary>
    /// 商务单 / 内容计划 → DTO 的唯一转换入口。首页、列表页、详情页都走这里，
    /// 避免同一个字段在三处被格式化成三种样子。
    /// </summary>
    public static class ExecutionMapper
    {
        public static DealDto DealToDto(Deal deal) => new()
        {
            Id = deal.Id,
            ClientName = deal.ClientName,
            Title = string.IsNullOrWhiteSpace(deal.Title) ? deal.ClientName : deal.Title,
            Contact = deal.Contact,
            Amount = deal.Amount,
            AmountDisplay = DealStageText.FormatAmount(deal.Amount, deal.Currency),
            Currency = deal.Currency,
            Stage = deal.Stage,
            StageLabel = DealStageText.Label(deal.Stage),
            Tone = DealStageText.Tone(deal.Stage),
            OwnerName = deal.OwnerName,
            OwnerAuthId = deal.OwnerAuthId,
            NextStep = string.IsNullOrWhiteSpace(deal.NextStep)
                ? DealStage.DefaultNextStep(deal.Stage)
                : deal.NextStep,
            ExpectedCloseAt = deal.ExpectedCloseAt?.ToString("yyyy-MM-dd"),
            Notes = deal.Notes,
            CreatedBy = deal.CreatedBy,
            CreatedAt = deal.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            UpdatedAt = deal.UpdatedAt.ToString("yyyy-MM-dd HH:mm"),
            IsSample = SampleData.IsSample(deal.CreatedBy),
            NextStage = DealStageText.NextStage(deal.Stage)
        };

        public static ContentPlanDto PlanToDto(ContentPlan plan, string? dealTitle) => new()
        {
            Id = plan.Id,
            Title = plan.Title,
            Platforms = plan.Platforms,
            PlannedAt = plan.PlannedAt?.ToString("yyyy-MM-dd"),
            CycleDisplay = PlanStatusText.FormatCycle(plan.PlannedAt),
            Status = plan.Status,
            StatusLabel = PlanStatusText.Label(plan.Status),
            Tone = PlanStatusText.Tone(plan.Status),
            Direction = plan.Direction,
            Score = plan.Score,
            ScoreDisplay = plan.Score.HasValue ? $"{plan.Score.Value:0.#} / 10" : "未评分",
            AssigneeName = plan.AssigneeName,
            DealId = plan.DealId,
            DealTitle = string.IsNullOrWhiteSpace(dealTitle) ? string.Empty : dealTitle,
            Link = plan.Link,
            CreatedBy = plan.CreatedBy,
            CreatedAt = plan.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            UpdatedAt = plan.UpdatedAt.ToString("yyyy-MM-dd HH:mm"),
            IsSample = SampleData.IsSample(plan.CreatedBy),
            NextStatus = PlanStatusText.NextStatus(plan.Status)
        };

        /// <summary>
        /// 商务单排序权重：待报价审核最急，其次待制作 / 发布、报价中，已结算与已丢失垫底。
        /// 首页与列表页共用，避免两处顺序不一致。
        /// </summary>
        public static int DealPriority(string? stage) => stage switch
        {
            DealStage.PricingReview => 0,
            DealStage.Delivering => 1,
            DealStage.Quoting => 2,
            DealStage.Confirmed => 3,
            DealStage.Lead => 4,
            DealStage.Settled => 5,
            _ => 6
        };

        /// <summary>
        /// 内容计划排序权重：待制作 / 发布最急，其次评分中、选题中，已发布与已归档排后。
        /// </summary>
        public static int PlanPriority(string? status) => status switch
        {
            ContentPlanStatus.Scheduled => 0,
            ContentPlanStatus.Scoring => 1,
            ContentPlanStatus.Draft => 2,
            ContentPlanStatus.Published => 3,
            ContentPlanStatus.Archived => 4,
            _ => 5
        };

        public static TodoDto TodoToDto(TodoItem todo) => new()
        {
            Id = todo.Id,
            Key = $"todo-{todo.Id}",
            Auto = false,
            Title = todo.Title,
            OwnerRole = todo.OwnerRole,
            OwnerRoleLabel = RoleText.Label(todo.OwnerRole),
            Urgency = TodoText.Urgency(todo.DueDate, todo.Priority),
            Tone = TodoText.Tone(todo.DueDate, todo.Priority),
            Href = string.IsNullOrWhiteSpace(todo.Href) ? "/" : todo.Href,
            DueDate = todo.DueDate?.ToString("yyyy-MM-dd"),
            Status = todo.Status,
            CreatedBy = todo.CreatedBy,
            CreatedAt = todo.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            DoneAt = todo.DoneAt?.ToString("yyyy-MM-dd HH:mm"),
            DoneBy = todo.DoneBy
        };
    }
}
