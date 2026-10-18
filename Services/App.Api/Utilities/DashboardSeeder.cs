using App.Api.Accessors;
using Contracts.Domain.Database;
using Serilog;

namespace App.Api.Utilities
{
    /// <summary>
    /// 开发态示例数据：仅在 Development 环境、且库里没有任何 MetricDaily 时写入一次。
    /// 每条记录都标记 Source = seed，界面上会明确标注「开发态示例数据」，
    /// 不会被当成真实业务数据。要重置：删除 LocalDatabaseAccessor 生成的
    /// PlatformAccount.json 与 MetricDaily.json。
    /// </summary>
    public class DashboardSeeder
    {
        private const int SeedDays = 7;
        private const string RecordedBy = "seed:development";

        private readonly IDatabaseAccessor _databaseAccessor;

        public DashboardSeeder(IDatabaseAccessor databaseAccessor)
        {
            _databaseAccessor = databaseAccessor;
        }

        public async Task SeedAsync()
        {
            var existing = (await _databaseAccessor.GetAllDocuments<MetricDaily>(0, 1)).ToList();
            if (existing.Count > 0)
            {
                return;
            }

            var accounts = await CreateAccounts();
            var end = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = end.AddDays(-(SeedDays - 1));

            var total = 0;
            foreach (var (account, startViews, dailyGrowth) in accounts)
            {
                for (var offset = 0; offset < SeedDays; offset++)
                {
                    var statDate = start.AddDays(offset);
                    var views = (long)Math.Round(startViews * Math.Pow(dailyGrowth, offset));

                    await _databaseAccessor.InsertDocument(new MetricDaily
                    {
                        AccountId = account.Id,
                        Platform = account.Platform,
                        StatDate = statDate,
                        Views = views,
                        Likes = (long)Math.Round(views * 0.082),
                        Comments = (long)Math.Round(views * 0.013),
                        Shares = (long)Math.Round(views * 0.006),
                        NewFollowers = (int)Math.Round(views * 0.0016),
                        Source = MetricSource.Seed,
                        RecordedBy = RecordedBy,
                        RecordedAt = DateTimeOffset.UtcNow.AddDays(-(SeedDays - 1 - offset))
                    });

                    total++;
                }
            }

            Log.Information("Dashboard seed wrote {Count} metric rows across {Accounts} accounts ({Start} to {End})",
                total, accounts.Count, start, end);
        }

        private async Task<List<(PlatformAccount Account, double StartViews, double DailyGrowth)>> CreateAccounts()
        {
            // 起点播放量与日增长率，仅用于让示例数据呈现「有涨有跌」的形状。
            var definitions = new (string Platform, string Handle, string DisplayName, string Status, int Followers, double StartViews, double DailyGrowth)[]
            {
                ("douyin", "@shuishui", "抖音 · 主号", PlatformAccountStatus.Connected, 128400, 96000, 1.12),
                ("xiaohongshu", "@shuishui", "小红书 · 主号", PlatformAccountStatus.Manual, 46300, 42000, 1.15),
                ("wechat-official", "shuishui", "公众号 · 主号", PlatformAccountStatus.Manual, 21800, 38000, 1.01),
                ("weibo", "@shuishui", "微博 · 主号", PlatformAccountStatus.Manual, 15600, 52000, 0.90),
                ("wechat-video", "shuishui", "视频号 · 主号", PlatformAccountStatus.Manual, 9400, 21000, 1.03)
            };

            var accounts = new List<(PlatformAccount, double, double)>();
            foreach (var definition in definitions)
            {
                var account = new PlatformAccount
                {
                    Platform = definition.Platform,
                    Handle = definition.Handle,
                    DisplayName = definition.DisplayName,
                    Status = definition.Status,
                    FollowerCount = definition.Followers,
                    OwnerId = string.Empty
                };

                await _databaseAccessor.InsertDocument(account);
                accounts.Add((account, definition.StartViews, definition.DailyGrowth));
            }

            return accounts;
        }
    }
}
