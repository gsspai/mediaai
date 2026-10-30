namespace Contracts.Domain.Database;

/// <summary>
/// 跨角色待办。只存「需要人点一下才会消失」的事项；
/// 由商务单阶段、内容计划缺失推导出来的待办不落库，见 Utilities/TodoBuilder，
/// 底层条件修好后自动消失，不需要人工勾选。
/// </summary>
public class TodoItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 负责角色标识（finance / planner / operations / editor / sales / admin），
    /// 中文名由前端映射，避免后端存展示文案。
    /// </summary>
    public string OwnerRole { get; set; } = "operations";

    /// <summary>
    /// high / normal。
    /// </summary>
    public string Priority { get; set; } = TodoPriority.Normal;

    public DateOnly? DueDate { get; set; }

    /// <summary>
    /// 点击待办跳转的相对路径，例如 /business/xxx。
    /// </summary>
    public string Href { get; set; } = "/";

    /// <summary>
    /// open / done。
    /// </summary>
    public string Status { get; set; } = TodoStatus.Open;

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DoneAt { get; set; }

    public string DoneBy { get; set; } = string.Empty;
}

public static class TodoStatus
{
    public const string Open = "open";
    public const string Done = "done";
}

public static class TodoPriority
{
    public const string High = "high";
    public const string Normal = "normal";

    public static bool IsValid(string? priority) =>
        string.Equals(priority, High, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(priority, Normal, StringComparison.OrdinalIgnoreCase);
}
