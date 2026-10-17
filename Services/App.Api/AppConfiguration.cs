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
}