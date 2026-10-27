using App.Api.Accessors;
using Contracts.Domain.Database;

namespace App.Api.Utilities;

/// <summary>
/// 阈值配置的读取与来源说明。库里没有配置时直接回落到默认值，不读也不写库，
/// 避免为了「用默认值」先往库里塞一条空配置。
/// </summary>
public static class SignalThresholdStore
{
    public static async Task<SignalThresholdConfig> LoadAsync(IDatabaseAccessor databaseAccessor)
    {
        var stored = await databaseAccessor.GetDocumentById<SignalThresholdConfig>(SignalThresholdDefaults.SingletonId);
        return stored ?? SignalThresholdDefaults.Create();
    }

    public static bool IsCustomized(SignalThresholdConfig config) => config.UpdatedAt != default;

    /// <summary>
    /// 一句话说明当前阈值从哪来：默认值，或者谁在什么时候改的。
    /// </summary>
    public static string DescribeSource(SignalThresholdConfig config)
    {
        if (!IsCustomized(config))
        {
            return "当前使用默认阈值";
        }

        var when = config.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        return string.IsNullOrWhiteSpace(config.UpdatedBy)
            ? $"阈值更新于 {when}"
            : $"阈值更新于 {when} · {config.UpdatedBy}";
    }
}
