namespace Contracts.Dto;

/// <summary>
/// 内容计划视图。StatusLabel / Tone 与 Status 一起返回，前端不自己拼文案。
/// </summary>
public class ContentPlanDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 平台标识集合（douyin / xiaohongshu / ...），中文名由前端映射。
    /// </summary>
    public List<string> Platforms { get; set; } = new();

    /// <summary>
    /// 排期日（yyyy-MM-dd），为空表示尚未排期。
    /// </summary>
    public string? PlannedAt { get; set; }

    /// <summary>
    /// 展示用的周期文案，例如「今日 14:00」「本周三」；由 PlannedAt 与今天的关系算出来。
    /// </summary>
    public string CycleDisplay { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public string Tone { get; set; } = "default";

    public string Direction { get; set; } = string.Empty;

    public double? Score { get; set; }

    /// <summary>
    /// 已格式化的分数，例如「8.7 / 10」；未评分时为「未评分」。
    /// </summary>
    public string ScoreDisplay { get; set; } = string.Empty;

    public string AssigneeName { get; set; } = string.Empty;

    public Guid? DealId { get; set; }

    /// <summary>
    /// 关联商务单的标题，方便详情页直接显示，不必再查一次。
    /// </summary>
    public string DealTitle { get; set; } = string.Empty;

    public string Link { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public string CreatedAt { get; set; } = string.Empty;

    public string UpdatedAt { get; set; } = string.Empty;

    public bool IsSample { get; set; }

    /// <summary>
    /// 允许推进到的下一状态标识，已发布 / 已归档时为空。
    /// </summary>
    public string? NextStatus { get; set; }
}

public class ContentPlanListDto
{
    public List<ContentPlanDto> Items { get; set; } = new();

    public int Total { get; set; }
}

public class PlanListRequestDto
{
    public string? Status { get; set; }

    public string? Keyword { get; set; }

    public int Take { get; set; }
}

public class PlanByIdRequestDto
{
    public Guid PlanId { get; set; }
}

public class SavePlanRequestDto
{
    public Guid? Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<string> Platforms { get; set; } = new();

    /// <summary>
    /// yyyy-MM-dd，留空表示尚未排期。
    /// </summary>
    public string? PlannedAt { get; set; }

    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// 0-10，留空表示尚未评分。
    /// </summary>
    public double? Score { get; set; }

    public string AssigneeName { get; set; } = string.Empty;

    public Guid? DealId { get; set; }

    public string Link { get; set; } = string.Empty;
}

public class SavePlanResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public ContentPlanDto? Plan { get; set; }
}

public class UpdatePlanStatusRequestDto
{
    public Guid PlanId { get; set; }

    public string Status { get; set; } = string.Empty;
}

public class UpdatePlanStatusResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public ContentPlanDto? Plan { get; set; }
}
