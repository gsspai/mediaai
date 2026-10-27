namespace Contracts.Domain.Database;

/// <summary>
/// 机会 / 风险判定阈值。全库只存一条（主键固定为 <see cref="SignalThresholdDefaults.SingletonId"/>），
/// 库里没有这条文档时按默认值计算，所以「阈值可配」不会让首次启动变成空口径。
/// </summary>
public class SignalThresholdConfig : BaseEntity
{
    /// <summary>
    /// 机会：单日播放 ≥ 账号区间日均的多少倍。
    /// </summary>
    public double BreakoutMultiplier { get; set; } = SignalThresholdDefaults.BreakoutMultiplier;

    /// <summary>
    /// 机会：单日播放的绝对下限，避免小账号的正常波动被判成爆款。
    /// </summary>
    public long BreakoutMinViews { get; set; } = SignalThresholdDefaults.BreakoutMinViews;

    /// <summary>
    /// 风险：前后两段区间日均播放的下降幅度。存正数，表示「下降 ≥ N%」。
    /// </summary>
    public double DeclineThresholdPercent { get; set; } = SignalThresholdDefaults.DeclineThresholdPercent;

    /// <summary>
    /// 风险：互动率（点赞 + 评论 + 转发 / 播放）低于多少百分比算偏低。
    /// </summary>
    public double LowInteractionRatePercent { get; set; } = SignalThresholdDefaults.LowInteractionRatePercent;

    /// <summary>
    /// 风险：连续多少天没有日记录算停止更新。
    /// </summary>
    public int StaleDays { get; set; } = SignalThresholdDefaults.StaleDays;

    /// <summary>
    /// 最后一次保存时间。默认值（0001-01-01）表示库里没有配置、当前走的是默认阈值。
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// 最后修改人的邮箱，用于界面上说明「这个口径是谁改的」。
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;
}

/// <summary>
/// 阈值默认值与单例主键。默认值也是「恢复默认」的取值来源，不要在别处再抄一份。
/// </summary>
public static class SignalThresholdDefaults
{
    /// <summary>
    /// 固定主键：保证阈值配置是单例，保存时 Upsert 不会写出第二条。
    /// </summary>
    public static readonly Guid SingletonId = Guid.Parse("8c1f5c4a-3d2e-4b1a-9f77-2a6d0b5e11c3");

    public const double BreakoutMultiplier = 2.0;
    public const long BreakoutMinViews = 1000;
    public const double DeclineThresholdPercent = 30;
    public const double LowInteractionRatePercent = 1.0;
    public const int StaleDays = 3;

    public static SignalThresholdConfig Create() => new()
    {
        Id = SingletonId,
        BreakoutMultiplier = SignalThresholdDefaults.BreakoutMultiplier,
        BreakoutMinViews = SignalThresholdDefaults.BreakoutMinViews,
        DeclineThresholdPercent = SignalThresholdDefaults.DeclineThresholdPercent,
        LowInteractionRatePercent = SignalThresholdDefaults.LowInteractionRatePercent,
        StaleDays = SignalThresholdDefaults.StaleDays
    };
}
