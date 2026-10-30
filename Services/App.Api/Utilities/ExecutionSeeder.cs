using App.Api.Accessors;
using Contracts.Domain.Database;
using Serilog;

namespace App.Api.Utilities
{
    /// <summary>
    /// 商务单 / 内容计划 / 待办的开发态示例数据。仅在 Development 且库里没有任何 Deal 时写入一次，
    /// 全部标记 CreatedBy = seed:development，界面会显示「示例」并单独说明来源。
    /// 要重置：删掉本地数据目录下的 Deal.json / ContentPlan.json / TodoItem.json。
    /// </summary>
    public class ExecutionSeeder
    {
        private const string SeedActor = SampleData.SeedPrefix + "development";

        private readonly IDatabaseAccessor _databaseAccessor;

        public ExecutionSeeder(IDatabaseAccessor databaseAccessor)
        {
            _databaseAccessor = databaseAccessor;
        }

        public async Task SeedAsync()
        {
            var existing = (await _databaseAccessor.GetAllDocuments<Deal>(0, 1)).ToList();
            if (existing.Count > 0)
            {
                return;
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var deals = await CreateDeals(today);
            var plans = CreatePlans(today, deals);
            var todos = CreateTodos(today);

            await _databaseAccessor.UpsertDocuments(plans);
            await _databaseAccessor.UpsertDocuments(todos);

            Log.Information("Execution seed wrote {Deals} deals, {Plans} plans, {Todos} todos",
                deals.Count, plans.Count, todos.Count);
        }

        private async Task<List<Deal>> CreateDeals(DateOnly today)
        {
            var deals = new List<Deal>
            {
                new()
                {
                    ClientName = "青岚科技",
                    Title = "青岚科技春季合作",
                    Contact = "王珂",
                    Amount = 48000,
                    Stage = DealStage.PricingReview,
                    OwnerName = "林乔",
                    OwnerAuthId = "linqiao@example.com",
                    NextStep = "财务确认报价区间",
                    ExpectedCloseAt = today.AddDays(7),
                    Notes = "含 3 条短视频 + 1 篇公众号长文，报价已提交待复核。",
                    CreatedBy = SeedActor,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
                    UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
                },
                new()
                {
                    ClientName = "云屿品牌",
                    Title = "云屿品牌发布专项",
                    Contact = "陈亦",
                    Amount = 120000,
                    Stage = DealStage.Delivering,
                    OwnerName = "周岚",
                    OwnerAuthId = "zhoulan@example.com",
                    NextStep = "确认排期与交付边界",
                    ExpectedCloseAt = today.AddDays(14),
                    Notes = "发布会当周集中投放，素材由客户提供。",
                    CreatedBy = SeedActor,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-12),
                    UpdatedAt = DateTimeOffset.UtcNow.AddDays(-2)
                },
                new()
                {
                    ClientName = "洛川汽车",
                    Title = "洛川汽车区域投放",
                    Contact = "刘畅",
                    Amount = 86000,
                    Stage = DealStage.Quoting,
                    OwnerName = "许舟",
                    OwnerAuthId = "xuzhou@example.com",
                    NextStep = "补齐短视频封面与发布时间后提交复核",
                    ExpectedCloseAt = today.AddDays(9),
                    Notes = "区域门店到店转化考核，需要留出到店链接。",
                    CreatedBy = SeedActor,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-3),
                    UpdatedAt = DateTimeOffset.UtcNow.AddDays(-3)
                },
                new()
                {
                    ClientName = "晨星食品",
                    Title = "晨星食品年度框架",
                    Contact = "赵敏",
                    Amount = 260000,
                    Stage = DealStage.Settled,
                    OwnerName = "林乔",
                    OwnerAuthId = "linqiao@example.com",
                    NextStep = "归档并结算",
                    ExpectedCloseAt = today.AddDays(-20),
                    Notes = "年框 12 期，本轮已结算。",
                    CreatedBy = SeedActor,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-60),
                    UpdatedAt = DateTimeOffset.UtcNow.AddDays(-20)
                }
            };

            await _databaseAccessor.UpsertDocuments(deals);
            return deals;
        }

        private static List<ContentPlan> CreatePlans(DateOnly today, IReadOnlyList<Deal> deals)
        {
            return new List<ContentPlan>
            {
                new()
                {
                    Title = "今日热点窗口",
                    Platforms = new List<string> { "douyin", "wechat-official" },
                    PlannedAt = today,
                    Status = ContentPlanStatus.Scheduled,
                    Direction = "抖音故事化、公众号深度拆解",
                    Score = 8.7,
                    AssigneeName = "许舟",
                    DealId = deals[1].Id,
                    CreatedBy = SeedActor,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    UpdatedAt = DateTimeOffset.UtcNow.AddHours(-6)
                },
                new()
                {
                    Title = "周度科技观察",
                    Platforms = new List<string> { "weibo", "toutiao" },
                    PlannedAt = today.AddDays(2),
                    Status = ContentPlanStatus.Scoring,
                    Direction = "微博快评、头条延展报道",
                    Score = 7.9,
                    AssigneeName = "周岚",
                    CreatedBy = SeedActor,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                    UpdatedAt = DateTimeOffset.UtcNow.AddHours(-10)
                },
                new()
                {
                    Title = "品牌案例拆解",
                    Platforms = new List<string> { "wechat-official", "xiaohongshu" },
                    PlannedAt = today.AddDays(4),
                    Status = ContentPlanStatus.Draft,
                    Direction = "公众号长文、小红书图文复盘",
                    Score = 7.5,
                    AssigneeName = "林乔",
                    DealId = deals[0].Id,
                    CreatedBy = SeedActor,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                    UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
                },
                new()
                {
                    Title = "竞品观察复盘",
                    Platforms = new List<string> { "xiaohongshu" },
                    PlannedAt = today.AddDays(3),
                    Status = ContentPlanStatus.Draft,
                    Direction = string.Empty,
                    AssigneeName = "林乔",
                    CreatedBy = SeedActor,
                    CreatedAt = DateTimeOffset.UtcNow.AddHours(-8),
                    UpdatedAt = DateTimeOffset.UtcNow.AddHours(-8)
                }
            };
        }

        private static List<TodoItem> CreateTodos(DateOnly today)
        {
            return new List<TodoItem>
            {
                new()
                {
                    Title = "抖音账号授权即将失效，需要重新授权",
                    OwnerRole = "operations",
                    Priority = TodoPriority.High,
                    DueDate = today,
                    Href = "/connections",
                    CreatedBy = SeedActor,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
                },
                new()
                {
                    Title = "周报数据核对：确认本周新增粉丝口径",
                    OwnerRole = "analyst",
                    Priority = TodoPriority.Normal,
                    DueDate = today.AddDays(2),
                    Href = "/",
                    CreatedBy = SeedActor,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
                }
            };
        }
    }
}
