namespace Contracts.Dto;

/// <summary>
/// 工作台汇总。所有数值都由 MetricDaily 按区间聚合得到，不做任何估算。
/// Kpi 的 Trace 字段用于回答「这个数字是怎么来的」，前端必须展示。
/// </summary>
public class DashboardSummaryDto
{
    /// <summary>
    /// 区间起始日，格式 yyyy-MM-dd。DTO 层不用 DateOnly，避免给前端引入额外的日期解析约定。
    /// </summary>
    public string RangeStart { get; set; } = string.Empty;

    /// <summary>
    /// 区间结束日，格式 yyyy-MM-dd。
    /// </summary>
    public string RangeEnd { get; set; } = string.Empty;

    /// <summary>
    /// 区间内没有任何一条日记录时为 false，前端据此显示「尚未录入数据」而不是 0。
    /// </summary>
    public bool HasData { get; set; }

    public int AccountCount { get; set; }

    public DashboardTotalsDto Totals { get; set; } = new();

    public List<DashboardKpiDto> Kpis { get; set; } = new();

    public List<DashboardPlatformTrendDto> PlatformTrends { get; set; } = new();

    public DashboardDataSourceDto DataSource { get; set; } = new();

    /// <summary>
    /// 爆款机会：区间内单日播放明显超过该账号自身日均的记录。按倍数倒序，最多 3 条。
    /// </summary>
    public List<DashboardSignalDto> Opportunities { get; set; } = new();

    /// <summary>
    /// 限流 / 掉速风险：掉速、互动偏低、近几天没数据。按严重度排序，最多 3 条。
    /// </summary>
    public List<DashboardSignalDto> Risks { get; set; } = new();

    /// <summary>
    /// 触发规则的一句话说明，前端直接展示，避免「这些结论是怎么来的」说不清。
    /// 文案由当前生效的阈值生成，阈值改动后这里会跟着变。
    /// </summary>
    public string SignalPolicy { get; set; } = string.Empty;

    /// <summary>
    /// 阈值来源：默认阈值，或者「阈值更新于 2026-10-20 16:40 · someone@example.com」。
    /// </summary>
    public string SignalPolicyTrace { get; set; } = string.Empty;
}

/// <summary>
/// 首页机会 / 风险区块的一行。Title 是账号展示名，Signal 是触发值，Trace 说明依据。
/// </summary>
public class DashboardSignalDto
{
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 账号展示名，例如「抖音 · 主号」。
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 平台标识，前端映射为中文名。
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// 触发值，例如「2.8x 日均」「-42%」「近 4 天无数据」。
    /// </summary>
    public string Signal { get; set; } = string.Empty;

    /// <summary>
    /// 具体数字，例如「单日 18.6 万 · 区间合计 42.1 万」。
    /// </summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// success / warning / destructive，与前端 StatusPill 色板对应。
    /// </summary>
    public string Tone { get; set; } = "info";

    /// <summary>
    /// 回溯说明：哪天、几条记录、来源是什么。
    /// </summary>
    public string Trace { get; set; } = string.Empty;
}

/// <summary>
/// 区间合计。
/// </summary>
public class DashboardTotalsDto
{
    public long Views { get; set; }

    /// <summary>
    /// 点赞 + 评论 + 转发。
    /// </summary>
    public long Interactions { get; set; }

    public int NewFollowers { get; set; }

    /// <summary>
    /// 互动率 = Interactions / Views * 100，保留一位小数。
    /// </summary>
    public double InteractionRate { get; set; }
}

/// <summary>
/// 一张指标卡。
/// </summary>
public class DashboardKpiDto
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 已格式化的显示值，例如「抖音」「168.2 万」。
    /// </summary>
    public string Display { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// 显示值是否为文字（影响字号），例如平台名。
    /// </summary>
    public bool IsText { get; set; }

    /// <summary>
    /// ready / pending。pending 表示依赖的数据尚未接入，前端显示「待接入」而不是占位数字。
    /// </summary>
    public string Status { get; set; } = "ready";

    /// <summary>
    /// 回溯说明：这个数字来自哪些账号、多少条日记录、什么来源。
    /// </summary>
    public string Trace { get; set; } = string.Empty;
}

/// <summary>
/// 单个平台在区间内的走势。
/// </summary>
public class DashboardPlatformTrendDto
{
    public string Platform { get; set; } = string.Empty;

    public List<DashboardTrendPointDto> Points { get; set; } = new();

    /// <summary>
    /// 前半区间与后半区间的播放变化，例如「+32%」「-47%」「暂无对比」。
    /// </summary>
    public string ChangeRate { get; set; } = string.Empty;

    /// <summary>
    /// success / info / destructive，与前端 StatusPill 的色板对应。
    /// </summary>
    public string Tone { get; set; } = "info";
}

/// <summary>
/// 走势上的一个点（按天）。
/// </summary>
public class DashboardTrendPointDto
{
    /// <summary>
    /// 横轴标签，形如「10-18」。
    /// </summary>
    public string Label { get; set; } = string.Empty;

    public long Views { get; set; }

    public long Comments { get; set; }

    public long Shares { get; set; }
}

/// <summary>
/// 数据来源构成，用于在界面上说明「这些数字从哪来」。
/// </summary>
public class DashboardDataSourceDto
{
    public int ManualCount { get; set; }

    /// <summary>
    /// CSV 批量导入的条数。与 ManualCount 分开，便于判断数据是人一条条录的还是整批灌进来的。
    /// </summary>
    public int CsvCount { get; set; }

    public int ApiCount { get; set; }

    public int SeedCount { get; set; }

    /// <summary>
    /// 可直接展示的一句话，例如「开发态示例数据 · 5 个账号 / 35 条日记录」。
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
