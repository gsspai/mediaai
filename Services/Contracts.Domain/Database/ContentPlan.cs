namespace Contracts.Domain.Database;

/// <summary>
/// 内容计划（选题 → 排期 → 发布）。字段与《实施计划 · 3.5 ContentPlan》对齐。
/// </summary>
public class ContentPlan : BaseEntity
{
    /// <summary>
    /// 计划名，例如「今日热点窗口」。
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 覆盖平台，存标识（douyin / xiaohongshu / ...），中文名由前端映射。
    /// </summary>
    public List<string> Platforms { get; set; } = new();

    /// <summary>
    /// 排期日期。为空表示尚未排期。
    /// </summary>
    public DateOnly? PlannedAt { get; set; }

    /// <summary>
    /// 状态：draft / scoring / scheduled / published / archived。
    /// </summary>
    public string Status { get; set; } = ContentPlanStatus.Draft;

    /// <summary>
    /// 内容方向，例如「抖音故事化、公众号深度拆解」。
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// 机会分 0-10，为空表示尚未评分。
    /// </summary>
    public double? Score { get; set; }

    /// <summary>
    /// 责任人展示名。
    /// </summary>
    public string AssigneeName { get; set; } = string.Empty;

    /// <summary>
    /// 关联商务单（可选）。一个商务单可以拆出多条内容。
    /// </summary>
    public Guid? DealId { get; set; }

    /// <summary>
    /// 成品链接，发布后回填。
    /// </summary>
    public string Link { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// ContentPlan.Status 的取值。顺序即推进顺序，archived 可从任意阶段进入。
/// </summary>
public static class ContentPlanStatus
{
    public const string Draft = "draft";
    public const string Scoring = "scoring";
    public const string Scheduled = "scheduled";
    public const string Published = "published";
    public const string Archived = "archived";

    public static readonly string[] Pipeline = { Draft, Scoring, Scheduled, Published };

    public static readonly string[] All = { Draft, Scoring, Scheduled, Published, Archived };

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status) && Array.IndexOf(All, status.Trim()) >= 0;

    /// <summary>
    /// 返回 null 表示允许，否则是中文原因。
    /// </summary>
    public static string? ValidateTransition(string from, string to)
    {
        if (!IsValid(to))
        {
            return $"不支持的状态「{to}」。";
        }

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            return "状态没有变化。";
        }

        if (string.Equals(to, Archived, StringComparison.Ordinal))
        {
            return null;
        }

        if (string.Equals(from, Archived, StringComparison.Ordinal))
        {
            return "已归档的计划不能再改状态。";
        }

        var fromIndex = Array.IndexOf(Pipeline, from);
        var toIndex = Array.IndexOf(Pipeline, to);
        if (fromIndex < 0 || toIndex < 0 || toIndex != fromIndex + 1)
        {
            return $"只能推进到下一状态，不能从 {from} 直接跳到 {to}。";
        }

        return null;
    }
}
