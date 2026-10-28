namespace App.Api;
public class AppConfiguration
{
    public string SupabaseId { get; set; } = string.Empty;
    public string SupabaseHost { get; set; } = string.Empty;
    public string SupabasePort { get; set; } = string.Empty;
    public string SupabaseDatabasePassword { get; set; } = string.Empty;
    public string SupabasePublishableKey { get; set; } = string.Empty;
    public string SupabaseSecretKey { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// Development-only JWT signing key used by <see cref="App.Api.Accessors.LocalAuthAccessor"/>.
    /// Leave empty to let the API generate and persist a random key under the local app data directory.
    /// Never commit a real value; provide it through configuration (AppConfiguration__LocalJwtSigningKey).
    /// </summary>
    public string LocalJwtSigningKey { get; set; } = string.Empty;

    /// <summary>
    /// 本地认证下的初始管理员邮箱（逗号或分号分隔）。
    /// 本地 JSON 认证没有飞书用户组可同步，命中名单的账号在登录 / 注册时自动补上 admin 角色并落库，
    /// 否则 /settings 这类只对管理员开放的页面没人能进。
    /// 生产环境角色来自飞书用户组映射，这里留空。
    /// </summary>
    public string AdminEmails { get; set; } = string.Empty;
}