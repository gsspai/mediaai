using Contracts.Domain.Database;

namespace App.Api.Utilities
{
    /// <summary>
    /// 商务单阶段的展示文案与色板。前端不再自己拼文案，改文案只改这里。
    /// </summary>
    public static class DealStageText
    {
        private static readonly Dictionary<string, (string Label, string Tone)> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            [DealStage.Lead] = ("线索", "default"),
            [DealStage.Quoting] = ("报价中", "info"),
            [DealStage.PricingReview] = ("待报价审核", "warning"),
            [DealStage.Confirmed] = ("已确认", "success"),
            [DealStage.Delivering] = ("待制作 / 发布", "info"),
            [DealStage.Settled] = ("已结算", "success"),
            [DealStage.Lost] = ("已丢失", "destructive")
        };

        public static string Label(string? stage) =>
            stage != null && Map.TryGetValue(stage, out var meta) ? meta.Label : "未知阶段";

        public static string Tone(string? stage) =>
            stage != null && Map.TryGetValue(stage, out var meta) ? meta.Tone : "default";

        /// <summary>
        /// 下一个可推进阶段；已结算 / 已丢失返回 null。
        /// </summary>
        public static string? NextStage(string? stage)
        {
            if (string.IsNullOrWhiteSpace(stage)) return DealStage.Lead;
            var index = Array.IndexOf(DealStage.Pipeline, stage);
            if (index < 0 || index >= DealStage.Pipeline.Length - 1) return null;
            return DealStage.Pipeline[index + 1];
        }

        public static string FormatAmount(decimal amount, string? currency)
        {
            if (amount <= 0) return "未报价";
            var symbol = string.Equals(currency, "CNY", StringComparison.OrdinalIgnoreCase) ? "¥" : $"{currency} ";
            return $"{symbol}{amount:N0}";
        }
    }

    /// <summary>
    /// 内容计划状态的展示文案与色板。
    /// </summary>
    public static class PlanStatusText
    {
        private static readonly Dictionary<string, (string Label, string Tone)> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            [ContentPlanStatus.Draft] = ("选题中", "default"),
            [ContentPlanStatus.Scoring] = ("评分中", "info"),
            [ContentPlanStatus.Scheduled] = ("待制作 / 发布", "warning"),
            [ContentPlanStatus.Published] = ("已发布", "success"),
            [ContentPlanStatus.Archived] = ("已归档", "default")
        };

        public static string Label(string? status) =>
            status != null && Map.TryGetValue(status, out var meta) ? meta.Label : "未知状态";

        public static string Tone(string? status) =>
            status != null && Map.TryGetValue(status, out var meta) ? meta.Tone : "default";

        public static string? NextStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return ContentPlanStatus.Draft;
            var index = Array.IndexOf(ContentPlanStatus.Pipeline, status);
            if (index < 0 || index >= ContentPlanStatus.Pipeline.Length - 1) return null;
            return ContentPlanStatus.Pipeline[index + 1];
        }

        /// <summary>
        /// 把排期日翻译成列表里能一眼看懂的周期文案。
        /// </summary>
        public static string FormatCycle(DateOnly? plannedAt)
        {
            if (!plannedAt.HasValue) return "待排期";
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var diff = plannedAt.Value.DayNumber - today.DayNumber;

            return diff switch
            {
                0 => "今日",
                1 => "明日",
                > 1 and < 7 => $"{plannedAt.Value:MM-dd}（本周）",
                _ => plannedAt.Value.ToString("MM-dd")
            };
        }
    }

    /// <summary>
    /// 角色中文名。与 Roles.cs 一起维护，前端 RoleText 保持同名映射。
    /// </summary>
    public static class RoleText
    {
        private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["admin"] = "管理员",
            ["sales"] = "商务",
            ["finance"] = "财务",
            ["planner"] = "策划",
            ["operations"] = "运营",
            ["editor"] = "剪辑",
            ["analyst"] = "分析",
            ["user"] = "访客"
        };

        public static string Label(string? role) =>
            role != null && Map.TryGetValue(role, out var label) ? label : "访客";
    }

    /// <summary>
    /// 待办紧急度文案。
    /// </summary>
    public static class TodoText
    {
        public static string Urgency(DateOnly? dueDate, string priority)
        {
            if (!dueDate.HasValue)
            {
                return string.Equals(priority, TodoPriority.High, StringComparison.OrdinalIgnoreCase) ? "高优先" : "随时处理";
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var diff = dueDate.Value.DayNumber - today.DayNumber;
            if (diff < 0) return "已逾期";
            if (diff == 0) return "今日处理";
            if (diff == 1) return "明日到期";
            return $"{dueDate.Value:MM-dd} 到期";
        }

        public static string Tone(DateOnly? dueDate, string priority)
        {
            if (!dueDate.HasValue)
            {
                return string.Equals(priority, TodoPriority.High, StringComparison.OrdinalIgnoreCase) ? "warning" : "default";
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var diff = dueDate.Value.DayNumber - today.DayNumber;
            if (diff < 0) return "destructive";
            if (diff == 0) return "warning";
            return "default";
        }
    }

    /// <summary>
    /// 判断一条记录是否开发态示例数据。示例数据一律由 DashboardSeeder / ExecutionSeeder 写入，
    /// 创建人以 seed: 开头，界面上会明确标注，不与真实业务数据混排。
    /// </summary>
    public static class SampleData
    {
        public const string SeedPrefix = "seed:";

        public static bool IsSample(string? createdBy) =>
            !string.IsNullOrEmpty(createdBy) && createdBy.StartsWith(SeedPrefix, StringComparison.Ordinal);
    }
}
