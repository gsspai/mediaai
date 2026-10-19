using App.Api.Accessors;
using App.Api.Utilities;
using App.ServiceInvoker.Attributes;
using App.ServiceInvoker.Interfaces;
using Contracts.Domain.Database;
using Contracts.Dto;

namespace App.Api.Managers
{
    /// <summary>
    /// 工作台汇总。只做聚合，不做估算：区间内没有日记录时返回 HasData = false，
    /// 前端显示「尚未录入数据」，而不是把 0 当成真实业务结论。
    /// </summary>
    public class DashboardManager : IManagerService
    {
        private const int DefaultDays = 7;
        private const int MinDays = 3;
        private const int MaxDays = 90;
        private const string KpiReady = "ready";
        private const string KpiPending = "pending";

        private readonly IDatabaseAccessor _databaseAccessor;

        public DashboardManager(IDatabaseAccessor databaseAccessor)
        {
            _databaseAccessor = databaseAccessor;
        }

        /// <summary>
        /// 返回区间内的 KPI 与平台走势，每个 KPI 附带 Trace 说明其来源。
        /// 环比按「日均播放」对比，避免后半月多一天导致的口径偏差。
        /// </summary>
        [RequireAuthenticated]
        public async Task<DashboardSummaryDto> GetSummary(GetDashboardSummaryRequestDto request)
        {
            var days = Math.Clamp(request?.Days ?? DefaultDays, MinDays, MaxDays);
            var end = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = end.AddDays(-(days - 1));

            var metrics = (await _databaseAccessor
                    .GetDocumentsByProperty<MetricDaily>(metric => metric.StatDate >= start && metric.StatDate <= end))
                .ToList();
            var accounts = (await _databaseAccessor.GetAllDocuments<PlatformAccount>()).ToList();

            var summary = new DashboardSummaryDto
            {
                RangeStart = start.ToString("yyyy-MM-dd"),
                RangeEnd = end.ToString("yyyy-MM-dd"),
                AccountCount = accounts.Count,
                HasData = metrics.Count > 0,
                DataSource = BuildDataSource(metrics, accounts)
            };

            if (!summary.HasData)
            {
                summary.Kpis = BuildPendingKpis();
                return summary;
            }

            var views = metrics.Sum(metric => metric.Views);
            var interactions = metrics.Sum(metric => metric.Likes + metric.Comments + metric.Shares);

            summary.Totals = new DashboardTotalsDto
            {
                Views = views,
                Interactions = interactions,
                NewFollowers = metrics.Sum(metric => metric.NewFollowers),
                InteractionRate = views > 0 ? Math.Round(interactions * 100d / views, 1) : 0
            };

            var firstHalfDays = days / 2;
            var secondHalfDays = days - firstHalfDays;
            var mid = start.AddDays(firstHalfDays);

            var platformStats = metrics
                .GroupBy(metric => metric.Platform)
                .Select(group => new PlatformAggregate
                {
                    Platform = group.Key,
                    Views = group.Sum(metric => metric.Views),
                    FirstHalfViews = group.Where(metric => metric.StatDate < mid).Sum(metric => metric.Views),
                    SecondHalfViews = group.Where(metric => metric.StatDate >= mid).Sum(metric => metric.Views),
                    AccountCount = group.Select(metric => metric.AccountId).Distinct().Count(),
                    RecordCount = group.Count(),
                    Points = group
                        .GroupBy(metric => metric.StatDate)
                        .OrderBy(date => date.Key)
                        .Select(date => new DashboardTrendPointDto
                        {
                            Label = date.Key.ToString("MM-dd"),
                            Views = date.Sum(metric => metric.Views),
                            Comments = date.Sum(metric => metric.Comments),
                            Shares = date.Sum(metric => metric.Shares)
                        })
                        .ToList()
                })
                .OrderByDescending(stat => stat.Views)
                .ToList();

            summary.Kpis = BuildKpis(summary.Totals, platformStats, metrics.Count, days, start, end);
            summary.PlatformTrends = platformStats
                .Take(3)
                .Select(stat =>
                {
                    var rate = DailyChangeRate(stat.FirstHalfViews, stat.SecondHalfViews, firstHalfDays, secondHalfDays);
                    return new DashboardPlatformTrendDto
                    {
                        Platform = stat.Platform,
                        Points = stat.Points,
                        ChangeRate = ToChangeRate(rate),
                        Tone = ToTone(rate)
                    };
                })
                .ToList();

            return summary;
        }

        private static List<DashboardKpiDto> BuildKpis(
            DashboardTotalsDto totals,
            IReadOnlyList<PlatformAggregate> platformStats,
            int recordCount,
            int days,
            DateOnly start,
            DateOnly end)
        {
            var firstHalfDays = days / 2;
            var secondHalfDays = days - firstHalfDays;

            var top = platformStats[0];
            var declining = platformStats
                .OrderBy(stat => DailyChangeRate(stat.FirstHalfViews, stat.SecondHalfViews, firstHalfDays, secondHalfDays) ?? double.MaxValue)
                .First();
            var decliningRate = DailyChangeRate(declining.FirstHalfViews, declining.SecondHalfViews, firstHalfDays, secondHalfDays);

            var rangeLabel = $"{start:MM-dd} 至 {end:MM-dd}";

            return new List<DashboardKpiDto>
            {
                new()
                {
                    Key = "top-platform",
                    Label = "最强平台",
                    Display = top.Platform,
                    Detail = $"近 {days} 天播放 {FormatCompact(top.Views)}",
                    IsText = true,
                    Status = KpiReady,
                    Trace = $"来自 {top.AccountCount} 个账号 / {top.RecordCount} 条日记录"
                },
                new()
                {
                    Key = "declining-platform",
                    Label = "下滑平台",
                    Display = decliningRate.HasValue ? declining.Platform : "—",
                    Detail = decliningRate.HasValue ? $"日均环比 {ToChangeRate(decliningRate)}" : "区间不足，无法对比",
                    IsText = true,
                    Status = decliningRate.HasValue ? KpiReady : KpiPending,
                    Trace = decliningRate.HasValue
                        ? $"前 {firstHalfDays} 天与后 {secondHalfDays} 天的日均播放对比"
                        : "至少需要两段可比的日记录"
                },
                new()
                {
                    Key = "total-views",
                    Label = "总播放",
                    Display = FormatCompact(totals.Views),
                    Detail = $"互动率 {totals.InteractionRate:0.#}%",
                    IsText = false,
                    Status = KpiReady,
                    Trace = $"{rangeLabel}，{recordCount} 条日记录合计"
                },
                new()
                {
                    Key = "new-followers",
                    Label = "新增粉丝",
                    Display = totals.NewFollowers.ToString("N0"),
                    Unit = "人",
                    Detail = "净增",
                    IsText = false,
                    Status = KpiReady,
                    Trace = $"{rangeLabel}，按日净增求和"
                }
            };
        }

        private static List<DashboardKpiDto> BuildPendingKpis()
        {
            var labels = new[] { ("top-platform", "最强平台"), ("declining-platform", "下滑平台"), ("total-views", "总播放"), ("new-followers", "新增粉丝") };
            return labels
                .Select(item => new DashboardKpiDto
                {
                    Key = item.Item1,
                    Label = item.Item2,
                    Display = "—",
                    Detail = "尚未录入数据",
                    IsText = true,
                    Status = KpiPending,
                    Trace = "区间内没有找到任何一条日记录"
                })
                .ToList();
        }

        private static DashboardDataSourceDto BuildDataSource(IReadOnlyList<MetricDaily> metrics, IReadOnlyList<PlatformAccount> accounts)
        {
            var manual = metrics.Count(metric => metric.Source == MetricSource.Manual);
            var api = metrics.Count(metric => metric.Source == MetricSource.Api);
            var seed = metrics.Count(metric => metric.Source == MetricSource.Seed);

            var parts = new List<string>();
            if (seed > 0) parts.Add(manual + api == 0 ? "开发态示例数据" : "含开发态示例数据");
            if (manual > 0) parts.Add($"手工录入 {manual} 条");
            if (api > 0) parts.Add($"接口同步 {api} 条");

            var label = parts.Count == 0
                ? "区间内暂无数据"
                : $"{string.Join(" · ", parts)} · {accounts.Count} 个账号 / {metrics.Count} 条日记录";

            return new DashboardDataSourceDto
            {
                ManualCount = manual,
                ApiCount = api,
                SeedCount = seed,
                Label = label
            };
        }

        /// <summary>
        /// 前后两段区间的日均播放变化率。前半段没有数据时返回 null，表示不可比。
        /// </summary>
        private static double? DailyChangeRate(long firstHalfViews, long secondHalfViews, int firstHalfDays, int secondHalfDays)
        {
            if (firstHalfViews <= 0 || firstHalfDays <= 0 || secondHalfDays <= 0)
            {
                return null;
            }

            var firstAverage = (double)firstHalfViews / firstHalfDays;
            var secondAverage = (double)secondHalfViews / secondHalfDays;
            return (secondAverage - firstAverage) * 100d / firstAverage;
        }

        private static string ToChangeRate(double? rate)
        {
            if (!rate.HasValue) return "暂无对比";
            return $"{(rate.Value > 0 ? "+" : string.Empty)}{rate.Value:0}%";
        }

        private static string ToTone(double? rate)
        {
            if (!rate.HasValue) return "info";
            if (rate.Value >= 5) return "success";
            if (rate.Value <= -5) return "destructive";
            return "info";
        }

        private static string FormatCompact(long value)
        {
            var abs = Math.Abs(value);
            if (abs >= 100_000_000) return $"{value / 100_000_000d:0.##} 亿";
            if (abs >= 10_000) return $"{value / 10_000d:0.#} 万";
            return value.ToString("N0");
        }
    }
}
