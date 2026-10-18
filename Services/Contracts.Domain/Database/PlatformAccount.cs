namespace Contracts.Domain.Database;

/// <summary>
/// 需要纳入统计的平台账号。
/// 一个账号对应一个平台上的一个发布身份，是 MetricDaily 的归属主体。
/// Status 为 manual 时表示该账号的数据靠手工录入（平台未提供接口）。
/// </summary>
public class PlatformAccount : BaseEntity
{
    /// <summary>
    /// 平台标识：douyin / xiaohongshu / wechat-video / wechat-official / weibo / toutiao / bilibili。
    /// 存字符串而非枚举，避免新增平台时改动序列化契约。
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// 平台内的账号标识，例如 @shuishui。
    /// </summary>
    public string Handle { get; set; } = string.Empty;

    /// <summary>
    /// 界面展示名。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 接入状态：connected（接口已授权）/ manual（手工录入）/ paused（暂停统计）/ error（授权失效）。
    /// </summary>
    public string Status { get; set; } = PlatformAccountStatus.Manual;

    /// <summary>
    /// 最近一次记录的粉丝数，仅作展示，趋势以 MetricDaily 为准。
    /// </summary>
    public int FollowerCount { get; set; }

    /// <summary>
    /// 归属用户；空字符串表示共享账号（M1 之前先不做数据隔离）。
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// PlatformAccount.Status 的取值。
/// </summary>
public static class PlatformAccountStatus
{
    public const string Connected = "connected";
    public const string Manual = "manual";
    public const string Paused = "paused";
    public const string Error = "error";
}
