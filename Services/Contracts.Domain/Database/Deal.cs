namespace Contracts.Domain.Database;

/// <summary>
/// 商务合作单。字段与《实施计划 · 3.6 Deal》对齐。
/// Stage 用字符串存而不是枚举，避免新增阶段时动序列化契约，也便于前端直接映射文案。
/// </summary>
public class Deal : BaseEntity
{
    /// <summary>
    /// 客户 / 品牌名。
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// 合作事项名，界面主标题；留空时前端回落显示 ClientName。
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 对接人，可为空。
    /// </summary>
    public string Contact { get; set; } = string.Empty;

    /// <summary>
    /// 报价金额，单位见 Currency。0 表示尚未报价。
    /// </summary>
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "CNY";

    /// <summary>
    /// 阶段：lead / quoting / pricing-review / confirmed / delivering / settled / lost。
    /// </summary>
    public string Stage { get; set; } = DealStage.Lead;

    /// <summary>
    /// 商务负责人展示名，例如「林乔」。
    /// </summary>
    public string OwnerName { get; set; } = string.Empty;

    /// <summary>
    /// 商务负责人账号（邮箱），用于归属与审计。
    /// </summary>
    public string OwnerAuthId { get; set; } = string.Empty;

    /// <summary>
    /// 当前阶段的下一步动作，直接显示在列表里。
    /// </summary>
    public string NextStep { get; set; } = string.Empty;

    /// <summary>
    /// 预计签约 / 交付日期。
    /// </summary>
    public DateOnly? ExpectedCloseAt { get; set; }

    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// 创建人邮箱；开发态示例数据为 seed:development。
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Deal.Stage 的取值与流转规则。流转只允许顺着往下走一格，或从任意阶段进入 lost。
/// 飞书审批回调接入后，推进由回调触发；在此之前由商务手工推进。
/// </summary>
public static class DealStage
{
    public const string Lead = "lead";
    public const string Quoting = "quoting";
    public const string PricingReview = "pricing-review";
    public const string Confirmed = "confirmed";
    public const string Delivering = "delivering";
    public const string Settled = "settled";
    public const string Lost = "lost";

    /// <summary>
    /// 正常推进顺序，不包含 lost。
    /// </summary>
    public static readonly string[] Pipeline =
    {
        Lead, Quoting, PricingReview, Confirmed, Delivering, Settled
    };

    public static readonly string[] All =
    {
        Lead, Quoting, PricingReview, Confirmed, Delivering, Settled, Lost
    };

    public static bool IsValid(string? stage) =>
        !string.IsNullOrWhiteSpace(stage) && Array.IndexOf(All, stage.Trim()) >= 0;

    /// <summary>
    /// 是否允许从 from 推进到 to。返回 null 表示允许，否则是中文原因。
    /// </summary>
    public static string? ValidateTransition(string from, string to)
    {
        if (!IsValid(to))
        {
            return $"不支持的阶段「{to}」。";
        }

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            return "阶段没有变化。";
        }

        if (string.Equals(from, Settled, StringComparison.Ordinal))
        {
            return "已结算的商务单不能再改阶段。";
        }

        if (string.Equals(to, Lost, StringComparison.Ordinal))
        {
            return null;
        }

        if (string.Equals(from, Lost, StringComparison.Ordinal))
        {
            return "已丢失的商务单要先改回推进阶段才能继续。";
        }

        var fromIndex = Array.IndexOf(Pipeline, from);
        var toIndex = Array.IndexOf(Pipeline, to);
        if (fromIndex < 0 || toIndex < 0)
        {
            return $"不支持的阶段流转：{from} → {to}。";
        }

        if (toIndex != fromIndex + 1)
        {
            return $"只能推进到下一阶段「{Pipeline[fromIndex + 1]}」，不能从 {from} 直接跳到 {to}。";
        }

        return null;
    }

    /// <summary>
    /// 阶段推进后默认的下一步动作，避免列表里出现空白的「下一步」。
    /// </summary>
    public static string DefaultNextStep(string stage) => stage switch
    {
        Lead => "确认需求范围与预算",
        Quoting => "整理报价区间待财务复核",
        PricingReview => "等待报价复核结论",
        Confirmed => "排期并拆解交付内容",
        Delivering => "跟踪制作与发布进度",
        Settled => "归档并结算",
        _ => "确认是否放弃本次合作"
    };
}
