namespace Contracts.Dto;

/// <summary>
/// 读取阈值配置的请求。当前没有筛选条件，保留这个类是为了符合一方法一入参的约定。
/// </summary>
public class GetSignalThresholdsRequestDto
{
}

/// <summary>
/// 机会 / 风险阈值。IsCustomized 为 false 表示库里没有配置、当前走默认值。
/// </summary>
public class SignalThresholdDto
{
    public double BreakoutMultiplier { get; set; }

    public long BreakoutMinViews { get; set; }

    /// <summary>
    /// 正数，表示「下降 ≥ N%」。
    /// </summary>
    public double DeclineThresholdPercent { get; set; }

    public double LowInteractionRatePercent { get; set; }

    public int StaleDays { get; set; }

    public bool IsCustomized { get; set; }

    /// <summary>
    /// 最后保存时间，格式 yyyy-MM-dd HH:mm。默认阈值时为空字符串。
    /// </summary>
    public string UpdatedAt { get; set; } = string.Empty;

    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// 按当前阈值生成的判定规则文案，首页与设置页预览共用同一份，避免两边口径不一致。
    /// </summary>
    public string Policy { get; set; } = string.Empty;
}

/// <summary>
/// 保存阈值。所有字段必填，校验失败返回 Success = false + Message，不抛异常。
/// </summary>
public class SaveSignalThresholdRequestDto
{
    public double BreakoutMultiplier { get; set; }

    public long BreakoutMinViews { get; set; }

    public double DeclineThresholdPercent { get; set; }

    public double LowInteractionRatePercent { get; set; }

    public int StaleDays { get; set; }
}

/// <summary>
/// 保存结果。成功时回带落库后的阈值，前端直接用它刷新页面，不再单独查一次。
/// </summary>
public class SaveSignalThresholdResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public SignalThresholdDto Thresholds { get; set; } = new();
}
