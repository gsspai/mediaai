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
    /// api / manual / csv / seed。
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

/// <summary>
/// CSV 导入请求。前端用 FileReader 读成文本直接提交，不做 base64——
/// 文本便于后端回显出错行，也省掉一次解码。
/// </summary>
public class ImportMetricsCsvRequestDto
{
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 完整 CSV 文本（含表头）。
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 一行导入错误。RowNumber 是文件物理行号，用于定位到具体哪一行。
/// </summary>
public class MetricImportErrorDto
{
    public int RowNumber { get; set; }

    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 导入结果。三种行数分开报，避免「成功 800 行」掩盖覆盖了谁的数据。
/// </summary>
public class ImportMetricsCsvResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 解析出的数据行数（不含表头与空行）。
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// 新建的日记录条数。
    /// </summary>
    public int CreatedRows { get; set; }

    /// <summary>
    /// 覆盖已有记录的条数。
    /// </summary>
    public int UpdatedRows { get; set; }

    /// <summary>
    /// 文件内重复（同账号同日出现多次，后出现的为准）被丢弃的行数。
    /// </summary>
    public int DuplicateRows { get; set; }

    public int FailedRows { get; set; }

    /// <summary>
    /// 失败明细，最多返回 20 条，超过时 ErrorsTruncated = true。
    /// </summary>
    public List<MetricImportErrorDto> Errors { get; set; } = new();

    public bool ErrorsTruncated { get; set; }

    /// <summary>
    /// 非致命提醒，例如被忽略的列名。
    /// </summary>
    public string Warning { get; set; } = string.Empty;
}
