namespace Contracts.Dto;

/// <summary>
/// 工作台汇总请求。Days 为统计区间长度，默认 7 天。
/// </summary>
public class GetDashboardSummaryRequestDto
{
    public int Days { get; set; } = 7;
}
