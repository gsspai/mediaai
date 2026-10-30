using App.Api.Accessors;
using App.Api.Utilities;
using App.Api.Utilities.Roles;
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

        // 机会 / 风险阈值已改为可配（SignalThresholdConfig + 设置页）。
        // 这里只保留「每个板块最多显示几条」这种纯展示常量，判定阈值一律从配置读，
        // 文案由 SignalPolicyText 按同一份配置生成，保证界面写的和实际算的一致。
        private const int MaxSignals = 3;

        /// <summary>
        /// 执行列表每块最多显示几条。与机会 / 风险一样，这是纯展示常量，不是判定阈值。
        /// </summary>
        private const int MaxExecutionRows = 3;

        private readonly IDatabaseAccessor _databaseAccessor;
        private readonly UserContextService _userContextService;

        public DashboardManager(IDatabaseAccessor databaseAccessor, UserContextService userContextService)
        {
            _databaseAccessor = databaseAccessor;
            _userContextService = userContextService;
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
            var thresholds = await SignalThresholdStore.LoadAsync(_databaseAccessor);

            var summary = new DashboardSummaryDto
            {
                RangeStart = start.ToString("yyyy-MM-dd"),
                RangeEnd = end.ToString("yyyy-MM-dd"),
                AccountCount = accounts.Count,
                HasData = metrics.Count > 0,
                DataSource = BuildDataSource(metrics, accounts),
                SignalPolicy = SignalPolicyText.Describe(days, thresholds),
                SignalPolicyTrace = SignalThresholdStore.DescribeSource(thresholds),
                // 执行列表与指标区间无关：即使区间内没有日记录，商务单与待办照样是真实单据。
                Execution = await BuildExecutionAsync()
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

            var signals = BuildSignals(metrics, accounts, days, start, end, thresholds);
            summary.Opportunities = signals.Opportunities;
            summary.Risks = signals.Risks;

            return summary;
        }

        /// <summary>
        /// 执行列表三块 + 管理层概览。三块各自「按紧急度取前 3 条」，
        /// 但 Total / OpenCount 给的是全量条数，前端「查看全部」旁的计数不会跟着被截断。
        /// </summary>
        private async Task<DashboardExecutionDto> BuildExecutionAsync()
        {
            var deals = (await _databaseAccessor.GetAllDocuments<Deal>()).ToList();
            var plans = (await _databaseAccessor.GetAllDocuments<ContentPlan>()).ToList();
            var todos = (await _databaseAccessor.GetAllDocuments<TodoItem>()).ToList();

            var dealTitles = deals.ToDictionary(
                deal => deal.Id,
                deal => string.IsNullOrWhiteSpace(deal.Title) ? deal.ClientName : deal.Title);

            var activeDeals = deals
                .Where(deal => !string.Equals(deal.Stage, DealStage.Lost, StringComparison.OrdinalIgnoreCase) &&
                               !string.Equals(deal.Stage, DealStage.Settled, StringComparison.OrdinalIgnoreCase))
                .OrderBy(deal => ExecutionMapper.DealPriority(deal.Stage))
                .ThenByDescending(deal => deal.UpdatedAt)
                .ToList();

            var activePlans = plans
                .Where(plan => !string.Equals(plan.Status, ContentPlanStatus.Archived, StringComparison.OrdinalIgnoreCase))
                .OrderBy(plan => ExecutionMapper.PlanPriority(plan.Status))
                .ThenBy(plan => plan.PlannedAt ?? DateOnly.MaxValue)
                .ThenByDescending(plan => plan.UpdatedAt)
                .ToList();

            var openTodos = TodoBuilder.Build(todos, deals, plans, includeDone: false, take: 200);

            var execution = new DashboardExecutionDto
            {
                Deals = activeDeals
                    .Take(MaxExecutionRows)
                    .Select(ExecutionMapper.DealToDto)
                    .ToList(),
                Plans = activePlans
                    .Take(MaxExecutionRows)
                    .Select(plan => ExecutionMapper.PlanToDto(
                        plan,
                        plan.DealId.HasValue && dealTitles.TryGetValue(plan.DealId.Value, out var title) ? title : string.Empty))
                    .ToList(),
                Todos = openTodos.Take(MaxExecutionRows).ToList(),
                DealTotal = activeDeals.Count,
                PlanTotal = activePlans.Count,
                TodoOpenCount = openTodos.Count,
                SourceLabel = BuildExecutionSourceLabel(deals, plans, todos)
            };

            // 管理层面板只给管理员；其他角色拿到空列表，前端据此不渲染。
            var roles = _userContextService.UserRoles ?? new List<string>();
            if (roles.Contains(Roles.Admin))
            {
                var inProgressDeals = deals.Count(deal => deal.Stage is DealStage.Quoting or DealStage.PricingReview
                    or DealStage.Confirmed or DealStage.Delivering);
                var pendingPlans = plans.Count(plan => plan.Status == ContentPlanStatus.Scheduled);

                execution.Management = new List<DashboardManagementDto>
                {
                    new()
                    {
                        Label = "进行中商务合作",
                        Value = inProgressDeals,
                        Unit = "个",
                        Trace = "报价中 / 待报价审核 / 已确认 / 待制作，不含已结算与已丢失"
                    },
                    new()
                    {
                        Label = "待制作内容计划",
                        Value = pendingPlans,
                        Unit = "份",
                        Trace = "状态为待制作 / 发布的内容计划，含今日排期"
                    },
                    new()
                    {
                        Label = "待处理待办",
                        Value = openTodos.Count,
                        Unit = "条",
                        Trace = "人工待办 + 由商务单阶段与内容计划推导出的自动待办"
                    }
                };
            }

            return execution;
        }

        private static string BuildExecutionSourceLabel(
            IReadOnlyList<Deal> deals,
            IReadOnlyList<ContentPlan> plans,
            IReadOnlyList<TodoItem> todos)
        {
            var manualTodos = todos.Count(todo => !string.Equals(todo.Status, TodoStatus.Done, StringComparison.OrdinalIgnoreCase));
            var hasSample = deals.Any(deal => SampleData.IsSample(deal.CreatedBy)) ||
                            plans.Any(plan => SampleData.IsSample(plan.CreatedBy)) ||
                            todos.Any(todo => SampleData.IsSample(todo.CreatedBy));

            var label = $"{deals.Count} 条商务单 / {plans.Count} 条内容计划 / {manualTodos} 条人工待办";
            return hasSample ? $"{label}（含开发态示例数据）" : label;
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
            var csv = metrics.Count(metric => metric.Source == MetricSource.Csv);
            var api = metrics.Count(metric => metric.Source == MetricSource.Api);
            var seed = metrics.Count(metric => metric.Source == MetricSource.Seed);

            var parts = new List<string>();
            if (seed > 0) parts.Add(manual + csv + api == 0 ? "开发态示例数据" : "含开发态示例数据");
            if (manual > 0) parts.Add($"手工录入 {manual} 条");
            if (csv > 0) parts.Add($"CSV 导入 {csv} 条");
            if (api > 0) parts.Add($"接口同步 {api} 条");

            var label = parts.Count == 0
                ? "区间内暂无数据"
                : $"{string.Join(" · ", parts)} · {accounts.Count} 个账号 / {metrics.Count} 条日记录";

            return new DashboardDataSourceDto
            {
                ManualCount = manual,
                CsvCount = csv,
                ApiCount = api,
                SeedCount = seed,
                Label = label
            };
        }

        /// <summary>
        /// 机会与风险都从 MetricDaily 现场算，每条都带触发值（Signal）与依据（Trace）。
        /// 阈值从 SignalThresholdConfig 读（设置页可改），没配置时用默认值。
        /// </summary>
        private static (List<DashboardSignalDto> Opportunities, List<DashboardSignalDto> Risks) BuildSignals(
            IReadOnlyList<MetricDaily> metrics,
            IReadOnlyList<PlatformAccount> accounts,
            int days,
            DateOnly start,
            DateOnly end,
            SignalThresholdConfig thresholds)
        {
            var nameById = accounts.ToDictionary(account => account.Id, account => account.DisplayName);
            var opportunities = new List<(DashboardSignalDto Signal, double Score)>();
            var risks = new List<(DashboardSignalDto Signal, int Severity, long Magnitude)>();

            var firstHalfDays = days / 2;
            var secondHalfDays = days - firstHalfDays;
            var mid = start.AddDays(firstHalfDays);

            foreach (var group in metrics.GroupBy(metric => metric.AccountId))
            {
                var records = group.ToList();
                var totalViews = records.Sum(record => record.Views);
                var interactions = records.Sum(record => record.Likes + record.Comments + record.Shares);
                var dayCount = records.Select(record => record.StatDate).Distinct().Count();
                var averageViews = dayCount > 0 ? (double)totalViews / dayCount : 0;
                var peak = records.OrderByDescending(record => record.Views).ThenByDescending(record => record.StatDate).First();
                var title = ResolveAccountName(peak, nameById);
                var source = ResolveSourceLabel(records);

                if (averageViews >= 1 && peak.Views >= thresholds.BreakoutMinViews && peak.Views >= averageViews * thresholds.BreakoutMultiplier)
                {
                    var ratio = peak.Views / averageViews;
                    opportunities.Add((
                        new DashboardSignalDto
                        {
                            Key = $"breakout-{peak.AccountId}-{peak.StatDate:yyyyMMdd}",
                            Title = title,
                            Platform = peak.Platform,
                            Signal = $"{ratio:0.#}x 日均",
                            Detail = $"单日 {FormatCompact(peak.Views)} 播放 · 区间日均 {FormatCompact((long)averageViews)}",
                            Tone = "success",
                            Trace = $"{peak.StatDate:MM-dd} · {records.Count} 条日记录 · {source}"
                        },
                        ratio));
                }

                var firstHalfViews = records.Where(record => record.StatDate < mid).Sum(record => record.Views);
                var secondHalfViews = records.Where(record => record.StatDate >= mid).Sum(record => record.Views);

                // 掉速优先于互动偏低：同一个账号两条都命中时，只报更严重的那条，避免一个账号占满整个面板。
                var rate = DailyChangeRate(firstHalfViews, secondHalfViews, firstHalfDays, secondHalfDays);
                if (rate.HasValue && rate.Value <= -thresholds.DeclineThresholdPercent && secondHalfViews > 0)
                {
                    var firstAverage = (double)firstHalfViews / firstHalfDays;
                    var secondAverage = (double)secondHalfViews / secondHalfDays;
                    risks.Add((
                        new DashboardSignalDto
                        {
                            Key = $"decline-{peak.AccountId}",
                            Title = title,
                            Platform = peak.Platform,
                            Signal = $"{rate.Value:0}%",
                            Detail = $"日均 {FormatCompact((long)firstAverage)} → {FormatCompact((long)secondAverage)}",
                            Tone = "destructive",
                            Trace = $"前 {firstHalfDays} 天与后 {secondHalfDays} 天日均对比 · {records.Count} 条日记录"
                        },
                        0,
                        (long)Math.Abs(rate.Value)));
                }
                else if (totalViews >= thresholds.BreakoutMinViews && interactions * 100d / totalViews < thresholds.LowInteractionRatePercent)
                {
                    var interactionRate = interactions * 100d / totalViews;
                    risks.Add((
                        new DashboardSignalDto
                        {
                            Key = $"low-interaction-{peak.AccountId}",
                            Title = title,
                            Platform = peak.Platform,
                            Signal = $"互动率 {interactionRate:0.##}%",
                            Detail = $"区间播放 {FormatCompact(totalViews)} · 互动 {FormatCompact(interactions)}",
                            Tone = "warning",
                            Trace = $"低于 {thresholds.LowInteractionRatePercent:0.#}% 阈值 · {records.Count} 条日记录 · {source}"
                        },
                        1,
                        (long)(thresholds.LowInteractionRatePercent - interactionRate)));
                }
            }

            // 停止更新的账号：已停用的不提醒，区间内新建的账号也不提醒，避免刚建号就报「缺数据」。
            var staleStart = end.AddDays(-(thresholds.StaleDays - 1));
            foreach (var account in accounts.Where(account => account.Status != PlatformAccountStatus.Paused))
            {
                if (DateOnly.FromDateTime(account.CreatedAt.UtcDateTime) > start)
                {
                    continue;
                }

                var latest = metrics
                    .Where(metric => metric.AccountId == account.Id)
                    .Select(metric => (DateOnly?)metric.StatDate)
                    .Max();

                if (latest.HasValue && latest.Value >= staleStart)
                {
                    continue;
                }

                var gapDays = latest.HasValue ? end.DayNumber - latest.Value.DayNumber : days;

                risks.Add((
                    new DashboardSignalDto
                    {
                        Key = $"stale-{account.Id}",
                        Title = account.DisplayName,
                        Platform = account.Platform,
                        Signal = $"近 {gapDays} 天无数据",
                        Detail = latest.HasValue
                            ? $"最后一次录入 {latest.Value:MM-dd} · 需要补录"
                            : $"区间 {start:MM-dd} 至 {end:MM-dd} 内没有任何日记录",
                        Tone = "warning",
                        Trace = $"账号 {account.Handle} · 状态 {account.Status}"
                    },
                    2,
                    gapDays));
            }

            return (
                opportunities
                    .OrderByDescending(item => item.Score)
                    .Take(MaxSignals)
                    .Select(item => item.Signal)
                    .ToList(),
                risks
                    .OrderBy(item => item.Severity)
                    .ThenByDescending(item => item.Magnitude)
                    .Take(MaxSignals)
                    .Select(item => item.Signal)
                    .ToList());
        }

        private static string ResolveAccountName(MetricDaily metric, IReadOnlyDictionary<Guid, string> nameById) =>
            nameById.TryGetValue(metric.AccountId, out var name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : $"{metric.Platform} 账号";

        /// <summary>
        /// 一个账号的多条记录可能来源混杂（手工补录 + 接口同步），取最强的那一种，避免文案含糊。
        /// </summary>
        private static string ResolveSourceLabel(IReadOnlyList<MetricDaily> records)
        {
            if (records.Any(record => record.Source == MetricSource.Manual)) return "手工录入";
            if (records.Any(record => record.Source == MetricSource.Api)) return "接口同步";
            return "开发态示例";
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
