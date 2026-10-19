namespace Contracts.Dto;

/// <summary>
/// 平台账号的连接页视图。不包含任何 token 字段，密钥类信息不出后端。
/// </summary>
public class PlatformAccountDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// 平台标识，例如 douyin。中文名由前端映射，后端只存标识。
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    public string Handle { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// connected / manual / paused / error。
    /// </summary>
    public string Status { get; set; } = string.Empty;

    public int FollowerCount { get; set; }

    /// <summary>
    /// 归属用户。空字符串表示共享账号（当前开发阶段的历史账号都是共享的）。
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// 最近一条日记录的日期（yyyy-MM-dd）。没有记录时为 null，界面显示「尚未录入」。
    /// </summary>
    public string? LastMetricDate { get; set; }

    /// <summary>
    /// 最近 N 天累计录入条数，用于判断账号是否真的有数据。
    /// </summary>
    public int RecentMetricCount { get; set; }

    public string CreatedAt { get; set; } = string.Empty;

    public string UpdatedAt { get; set; } = string.Empty;
}

/// <summary>
/// 列表统一包一层 DTO：ServiceInvoker 的方法返回值必须是 DTO 类型，
/// 直接返回 List&lt;T&gt; 会被约定检查拒绝。
/// </summary>
public class PlatformAccountListDto
{
    public List<PlatformAccountDto> Items { get; set; } = new();
}

public class ListAccountsRequestDto
{
    /// <summary>
    /// 是否包含已停用（paused）账号，默认不包含。
    /// </summary>
    public bool IncludePaused { get; set; }
}

public class SaveAccountRequestDto
{
    /// <summary>
    /// 为空表示新增，非空表示编辑。
    /// </summary>
    public Guid? Id { get; set; }

    public string Platform { get; set; } = string.Empty;

    public string Handle { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int FollowerCount { get; set; }
}

/// <summary>
/// 保存结果。校验失败走 Success = false + Message，不抛异常，
/// 让前端能直接把原因显示在表单上。
/// </summary>
public class SaveAccountResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public PlatformAccountDto? Account { get; set; }
}

public class AccountIdRequestDto
{
    public Guid AccountId { get; set; }
}
