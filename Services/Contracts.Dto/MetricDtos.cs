namespace Contracts.Dto;

/// <summary>
/// 一条日指标记录。RecordedBy / RecordedAt / Source 三者共同回答
/// 「谁、什么时候、以什么方式录的这个数字」。
/// </summary>
public class MetricEntryDto
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// 账号展示名，便于列表直接阅读，不必二次查账号表。
    /// </summary>
    public string AccountName { get; set; } = string.Empty;

    public string StatDate { get; set; } = string.Empty;

    public long Views { get; set; }

    public long Likes { get; set; }

    public long Comments { get; set; }

    public long Shares { get; set; }

    public int NewFollowers { get; set; }

    /// <summary>
    /// api / manual / seed。
    /// </summary>
    public string Source { get; set; } = string.Empty;

    public string RecordedBy { get; set; } = string.Empty;

    public string RecordedAt { get; set; } = string.Empty;
}

/// <summary>
/// 列表包装：ServiceInvoker 的方法返回值必须是 DTO 类型，不能直接返回 List&lt;T&gt;。
/// </summary>
public class MetricEntryListDto
{
    public List<MetricEntryDto> Items { get; set; } = new();
}

public class IngestManualMetricsRequestDto
{
    public Guid AccountId { get; set; }

    /// <summary>
    /// 统计日期，格式 yyyy-MM-dd。不使用 DateOnly，避免前端额外的序列化约定。
    /// </summary>
    public string StatDate { get; set; } = string.Empty;

    public long Views { get; set; }

    public long Likes { get; set; }

    public long Comments { get; set; }

    public long Shares { get; set; }

    public int NewFollowers { get; set; }
}

public class MetricIngestResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 成功后返回落库后的记录；覆盖已有记录时 Updated = true。
    /// </summary>
    public bool Updated { get; set; }

    public MetricEntryDto? Entry { get; set; }
}

public class ListRecentMetricsRequestDto
{
    /// <summary>
    /// 只看某个账号；为空表示所有可见账号。
    /// </summary>
    public Guid? AccountId { get; set; }

    public int Take { get; set; } = 20;
}
