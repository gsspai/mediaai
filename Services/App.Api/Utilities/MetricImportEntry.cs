namespace App.Api.Utilities
{
    /// <summary>
    /// CSV 导入中「已通过校验、等待落库」的一行。
    /// 放在 Utilities 而不是 Managers：`App.Api.Managers` 命名空间有分析器约束，
    /// 只允许放实现 IManagerService 的 Manager（RIDE0001 / RIDE0008）。
    /// </summary>
    public class MetricImportEntry
    {
        public Guid AccountId { get; init; }

        public string Platform { get; init; } = string.Empty;

        public DateOnly Date { get; init; }

        public long Views { get; init; }

        public long Likes { get; init; }

        public long Comments { get; init; }

        public long Shares { get; init; }

        public int NewFollowers { get; init; }
    }
}
