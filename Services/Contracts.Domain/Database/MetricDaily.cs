namespace Contracts.Domain.Database;

/// <summary>
/// 单个平台账号在某一天的指标快照。
/// 每个数字都必须能回答「谁、哪个平台、哪天、同步来的还是手工录的」，
/// 因此 Source / RecordedBy / RecordedAt 是必填语义，不允许留空默认值上线。
/// </summary>
public class MetricDaily : BaseEntity
{
    /// <summary>
    /// 归属账号。
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// 冗余平台标识，便于按平台聚合时不必回表。
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// 统计日期（平台侧的自然日）。
    /// </summary>
    public DateOnly StatDate { get; set; }

    /// <summary>
    /// 当日播放 / 阅读数。
    /// </summary>
    public long Views { get; set; }

    public long Likes { get; set; }

    public long Comments { get; set; }

    public long Shares { get; set; }

    /// <summary>
    /// 当日净增粉丝，可为负。
    /// </summary>
    public int NewFollowers { get; set; }

    /// <summary>
    /// 数据来源：api（接口同步）/ manual（手工录入）/ csv（批量导入）/ seed（开发态示例）。
    /// </summary>
    public string Source { get; set; } = MetricSource.Manual;

    /// <summary>
    /// 录入人或同步任务标识。
    /// </summary>
    public string RecordedBy { get; set; } = string.Empty;

    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// MetricDaily.Source 的取值。
/// </summary>
public static class MetricSource
{
    public const string Api = "api";
    public const string Manual = "manual";

    /// <summary>
    /// CSV 批量导入。与 manual 分开记，是为了回答「这 800 行是谁一条条录的」时能一眼看出不是人干的。
    /// </summary>
    public const string Csv = "csv";

    public const string Seed = "seed";
}
