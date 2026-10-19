namespace App.Api.Utilities
{
    /// <summary>
    /// 平台标识白名单与写入校验。后端只认标识（douyin），中文名由前端映射，
    /// 与 <c>react-app/src/utils/platforms.ts</c> 保持一致；新增平台需同时改两处。
    /// </summary>
    public static class PlatformCatalog
    {
        public static readonly IReadOnlyList<string> Supported = new[]
        {
            "douyin",
            "xiaohongshu",
            "wechat-official",
            "wechat-video",
            "weibo",
            "toutiao",
            "bilibili"
        };

        public static readonly IReadOnlyList<string> SupportedStatuses = new[]
        {
            Contracts.Domain.Database.PlatformAccountStatus.Connected,
            Contracts.Domain.Database.PlatformAccountStatus.Manual,
            Contracts.Domain.Database.PlatformAccountStatus.Paused,
            Contracts.Domain.Database.PlatformAccountStatus.Error
        };

        public static bool IsSupported(string? platform) =>
            !string.IsNullOrWhiteSpace(platform) && Supported.Contains(platform.Trim());

        public static bool IsSupportedStatus(string? status) =>
            !string.IsNullOrWhiteSpace(status) && SupportedStatuses.Contains(status.Trim());

        /// <summary>
        /// 界面可直接展示的平台清单，按「短视频 → 图文 → 资讯」的习惯顺序排列。
        /// </summary>
        public static string Normalize(string platform) => platform.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// 数据归属规则：OwnerId 为空表示共享账号（开发态种子账号、团队公用号），
    /// 非空则只对本人可见。后续接入飞书后再收紧为严格的单人多账号。
    /// </summary>
    public static class AccountVisibility
    {
        public static bool IsVisible(string? ownerId, string? currentUserId)
        {
            if (string.IsNullOrEmpty(ownerId)) return true;
            return !string.IsNullOrEmpty(currentUserId) && ownerId == currentUserId;
        }
    }
}
