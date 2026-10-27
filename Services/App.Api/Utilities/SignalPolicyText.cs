using Contracts.Domain.Database;

namespace App.Api.Utilities;

/// <summary>
/// 判定规则文案的唯一来源。首页展示、设置页预览都调这里，
/// 保证「界面上写的阈值」和「后端真正在算的阈值」永远是同一份。
/// </summary>
public static class SignalPolicyText
{
    /// <summary>
    /// 设置页预览用的区间天数。首页会按实际区间重新生成，这里只是给表单一个具体语境。
    /// </summary>
    public const int PreviewDays = 7;

    public static string Describe(int days, SignalThresholdConfig config)
    {
        var firstHalf = days / 2;
        var secondHalf = days - firstHalf;

        return $"机会：单日播放 ≥ {config.BreakoutMultiplier:0.#} 倍账号日均且 ≥ {config.BreakoutMinViews:N0}；"
            + $"风险：前 {firstHalf} 天与后 {secondHalf} 天日均下降 ≥ {config.DeclineThresholdPercent:0.#}%、互动率 < {config.LowInteractionRatePercent:0.#}%，"
            + $"或近 {config.StaleDays} 天没有日记录。全部按日记录实时计算，不预估。";
    }
}
