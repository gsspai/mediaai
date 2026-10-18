using Contracts.Dto;

namespace App.Api.Utilities
{
    /// <summary>
    /// 单个平台在统计区间内的聚合结果，仅由 DashboardManager 内部使用。
    /// 放在 Utilities 下是因为 App.Api.Managers 命名空间约定所有类型必须是 IManagerService。
    /// </summary>
    internal sealed class PlatformAggregate
    {
        public string Platform { get; set; } = string.Empty;

        public long Views { get; set; }

        public long FirstHalfViews { get; set; }

        public long SecondHalfViews { get; set; }

        public int AccountCount { get; set; }

        public int RecordCount { get; set; }

        public List<DashboardTrendPointDto> Points { get; set; } = new();
    }
}
