namespace Contracts.Dto;

/// <summary>
/// 商务单视图。Stage 原样返回标识，同时带上中文 StageLabel 与颜色 Tone，
/// 保证列表、详情、首页三处文案一致（文案只在后端 DealStageText 维护一份）。
/// </summary>
public class DealDto
{
    public Guid Id { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Contact { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    /// <summary>
    /// 已格式化的金额，例如「¥48,000」；未报价时为「未报价」。
    /// </summary>
    public string AmountDisplay { get; set; } = string.Empty;

    public string Currency { get; set; } = "CNY";

    public string Stage { get; set; } = string.Empty;

    public string StageLabel { get; set; } = string.Empty;

    /// <summary>
    /// success / info / warning / destructive / default，对应前端 StatusPill 色板。
    /// </summary>
    public string Tone { get; set; } = "default";

    public string OwnerName { get; set; } = string.Empty;

    public string OwnerAuthId { get; set; } = string.Empty;

    public string NextStep { get; set; } = string.Empty;

    /// <summary>
    /// 预计签约 / 交付日（yyyy-MM-dd）。
    /// </summary>
    public string? ExpectedCloseAt { get; set; }

    public string Notes { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public string CreatedAt { get; set; } = string.Empty;

    public string UpdatedAt { get; set; } = string.Empty;

    /// <summary>
    /// 是否开发态示例数据（CreatedBy 以 seed: 开头）。界面据此打「示例」标记。
    /// </summary>
    public bool IsSample { get; set; }

    /// <summary>
    /// 允许推进到的下一阶段标识，已结算 / 已丢失时为空。
    /// </summary>
    public string? NextStage { get; set; }
}

public class DealListDto
{
    public List<DealDto> Items { get; set; } = new();

    /// <summary>
    /// 不受筛选影响的总条数，用于「共 N 条」与空态判断。
    /// </summary>
    public int Total { get; set; }
}

public class DealListRequestDto
{
    /// <summary>
    /// 按阶段筛选，留空表示全部。
    /// </summary>
    public string? Stage { get; set; }

    /// <summary>
    /// 客户名 / 事项名 / 对接人关键词。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 最多返回条数，默认 50，上限 200。
    /// </summary>
    public int Take { get; set; }
}

public class DealByIdRequestDto
{
    public Guid DealId { get; set; }
}

public class SaveDealRequestDto
{
    /// <summary>
    /// 为空表示新增，非空表示编辑。
    /// </summary>
    public Guid? Id { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Contact { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "CNY";

    /// <summary>
    /// 新建时可指定起始阶段，留空默认 lead。编辑时不能用它改阶段，改阶段走 UpdateDealStage。
    /// </summary>
    public string? Stage { get; set; }

    public string OwnerName { get; set; } = string.Empty;

    public string? NextStep { get; set; }

    /// <summary>
    /// yyyy-MM-dd，留空表示尚未确定。
    /// </summary>
    public string? ExpectedCloseAt { get; set; }

    public string Notes { get; set; } = string.Empty;
}

public class SaveDealResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public DealDto? Deal { get; set; }
}

public class UpdateDealStageRequestDto
{
    public Guid DealId { get; set; }

    public string Stage { get; set; } = string.Empty;

    /// <summary>
    /// 可选的下一步说明，留空时按新阶段给默认文案。
    /// </summary>
    public string? NextStep { get; set; }
}

public class UpdateDealStageResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public DealDto? Deal { get; set; }
}
