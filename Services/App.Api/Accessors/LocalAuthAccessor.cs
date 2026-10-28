using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Contracts.Domain.Database;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using App.Api.Utilities;
using App.Api.Utilities.Roles;

namespace App.Api.Accessors
{
    public class LocalAuthAccessor : IAuthAccessor
    {
        private readonly string _userFilePath;
        private readonly ILogger<LocalAuthAccessor> _logger;
        private readonly string _signingKey;
        private const string LocalJwtIssuer = "ride-local";
        private const string LocalJwtAudience = "authenticated";
        private const int MinSigningKeyLength = 32;
        private const string SigningKeyFileName = "local-signing.key";
        private readonly string _mockRefreshToken = "local-dev-static-refresh-token";
        private readonly HashSet<string> _bootstrapAdminEmails;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        public LocalAuthAccessor(AppConfiguration config, ILogger<LocalAuthAccessor> logger)
        {
            var rootDir = LocalPathHelper.GetAppResourcePath(config.AppName);
            Directory.CreateDirectory(rootDir);
            _userFilePath = Path.Combine(rootDir, "Users.json");
            _logger = logger;
            _signingKey = ResolveSigningKey(config, rootDir, logger);
            _bootstrapAdminEmails = ParseAdminEmails(config.AdminEmails);
        }

        /// <summary>
        /// 解析 AppConfiguration:AdminEmails（逗号或分号分隔，大小写不敏感）。
        /// 本地认证没有飞书用户组同步，这是唯一能让某个账号拿到 admin 角色的入口。
        /// </summary>
        private static HashSet<string> ParseAdminEmails(string? raw) =>
            (raw ?? string.Empty)
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Resolves the development JWT signing key without ever shipping a key in source control:
        /// 1. explicit configuration (<c>AppConfiguration:LocalJwtSigningKey</c>) wins;
        /// 2. otherwise a random key is generated once and persisted under the local app data directory
        ///    with owner-only permissions, so tokens survive restarts on the same machine.
        /// </summary>
        private static string ResolveSigningKey(AppConfiguration config, string rootDir, ILogger logger)
        {
            var configured = config.LocalJwtSigningKey?.Trim();
            if (!string.IsNullOrEmpty(configured))
            {
                if (configured.Length < MinSigningKeyLength)
                {
                    throw new InvalidOperationException(
                        $"AppConfiguration:LocalJwtSigningKey must be at least {MinSigningKeyLength} characters long.");
                }
                return configured;
            }

            string keyFilePath = Path.Combine(rootDir, SigningKeyFileName);
            if (File.Exists(keyFilePath))
            {
                var existing = File.ReadAllText(keyFilePath).Trim();
                if (existing.Length >= MinSigningKeyLength)
                {
                    return existing;
                }
                logger.LogWarning("Local signing key at {Path} is invalid; regenerating a new one.", keyFilePath);
            }

            var generated = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            File.WriteAllText(keyFilePath, generated);
            RestrictToCurrentUser(keyFilePath);
            logger.LogInformation(
                "Generated a local development signing key at {Path}. Override it with AppConfiguration:LocalJwtSigningKey when needed.",
                keyFilePath);
            return generated;
        }

        private static void RestrictToCurrentUser(string path)
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            try
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch (Exception)
            {
                // Best effort only: some file systems ignore POSIX permissions.
            }
        }

        private async Task<List<User>> LoadUsersAsync()
        {
            if (!File.Exists(_userFilePath))
                return new List<User>();
            await using var stream = File.OpenRead(_userFilePath);
            return await JsonSerializer.DeserializeAsync<List<User>>(stream, JsonOptions) ?? new List<User>();
        }

        private async Task SaveUsersAsync(List<User> users)
        {
            await using var stream = File.Create(_userFilePath);
            await JsonSerializer.SerializeAsync(stream, users, JsonOptions);
        }

        private string GenerateJwt(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_signingKey);
            var appMetadata = new { roles = user.Roles };
            string appMetaJson = JsonSerializer.Serialize(appMetadata);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.AuthId ?? ""),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim("app_metadata", appMetaJson)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(12),
                Issuer = LocalJwtIssuer,
                Audience = LocalJwtAudience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public Task<(string, string)> GetInviteActionLink(User user, string redirectUrl)
        {
            var userId = string.IsNullOrWhiteSpace(user.AuthId) ? Guid.NewGuid().ToString() : user.AuthId;
            var link = $"file-invite://invite?userId={userId}&redirect={Uri.EscapeDataString(redirectUrl)}";
            return Task.FromResult((link, userId));
        }

        public async Task<bool> DeleteUser(string userId)
        {
            var users = await LoadUsersAsync();
            var beforeCount = users.Count;
            users.RemoveAll(u => u.AuthId == userId);
            await SaveUsersAsync(users);
            return users.Count < beforeCount;
        }

        public async Task<List<User>> GetUsersAsync()
        {
            return await LoadUsersAsync();
        }

        public async Task<User?> GetUserById(string userId)
        {
            return (await LoadUsersAsync()).FirstOrDefault(u => u.AuthId == userId);
        }

        public async Task<bool> UpdateEmail(string userId, string newEmail)
        {
            var users = await LoadUsersAsync();
            var user = users.FirstOrDefault(u => u.AuthId == userId);
            if (user == null) return false;
            user.Email = newEmail;
            await SaveUsersAsync(users);
            return true;
        }

        public async Task<bool> UpdatePassword(string userId, string newPassword)
        {
            // For local/mock auth: acknowledge the change but do not persist or log the password.
            _logger.LogInformation("Mock password change for user {UserId} (no-op in dev mode).", userId);
            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateUserName(string userId, string newName)
        {
            var users = await LoadUsersAsync();
            var user = users.FirstOrDefault(u => u.AuthId == userId);
            if (user == null) return false;
            user.Name = newName;
            await SaveUsersAsync(users);
            return true;
        }

        public async Task<bool> AddRolesToUserAsync(string userId, string[] rolesToAdd)
        {
            var users = await LoadUsersAsync();
            var user = users.FirstOrDefault(u => u.AuthId == userId);
            if (user == null) return false;
            user.Roles = user.Roles.Union(rolesToAdd).Distinct().ToArray();
            await SaveUsersAsync(users);
            return true;
        }

        public async Task<bool> RemoveRolesFromUserAsync(string userId, string[] rolesToRemove)
        {
            var users = await LoadUsersAsync();
            var user = users.FirstOrDefault(u => u.AuthId == userId);
            if (user == null) return false;
            user.Roles = user.Roles.Except(rolesToRemove).ToArray();
            await SaveUsersAsync(users);
            return true;
        }

        /// <summary>
        /// 命 AppConfiguration:AdminEmails 的账号在登录 / 注册时补上 admin，并立刻落库。
        /// 只在首次命中时写盘；角色已存在则直接跳过，避免每次登录都全量重写 Users.json。
        /// </summary>
        private async Task EnsureBootstrapAdminAsync(User user, List<User> users)
        {
            if (_bootstrapAdminEmails.Count == 0 || string.IsNullOrWhiteSpace(user.Email))
            {
                return;
            }

            if (!_bootstrapAdminEmails.Contains(user.Email))
            {
                return;
            }

            var roles = user.Roles?.ToList() ?? new List<string>();
            if (roles.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            roles.Add(Roles.Admin);
            user.Roles = roles.ToArray();
            await SaveUsersAsync(users);
            _logger.LogInformation("Granted bootstrap admin role to {Email} via AppConfiguration:AdminEmails.", user.Email);
        }

        public async Task<(User? user, string? jwt, string? refreshToken, string error)> SignUp(string email, string password, string? name)
        {
            var users = await LoadUsersAsync();
            var already = users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            if (already != null)
                return (null, null, null, "User already exists");
            var user = new User
            {
                AuthId = Guid.NewGuid().ToString(),
                Email = email,
                Name = string.IsNullOrEmpty(name) ? email : name,
                Roles = new[] { "user" }
            };
            users.Add(user);
            await SaveUsersAsync(users);
            await EnsureBootstrapAdminAsync(user, users);
            var jwt = GenerateJwt(user);
            return (user, jwt, _mockRefreshToken, string.Empty);
        }

        public async Task<(User? user, string? jwt, string? refreshToken, string error)> Login(string email, string password)
        {
            var users = await LoadUsersAsync();
            var user = users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            if (user == null)
                return (null, null, null, "Invalid email or user not found.");
            await EnsureBootstrapAdminAsync(user, users);
            string jwt = GenerateJwt(user);
            return (user, jwt, _mockRefreshToken, string.Empty);
        }

        // Password reset: mock/no-op
        public Task<bool> SendPasswordResetEmail(string email, string redirectUrl)
        {
            _logger.LogInformation($"Mock password reset email (not sent) to {email}, redirect: {redirectUrl}");
            return Task.FromResult(true);
        }

        public async Task<(User? user, string? jwt, string? refreshToken, string error)> RefreshToken(string accessToken, string refreshToken)
        {
            if (refreshToken != _mockRefreshToken)
            {
                return (null, null, null, "Invalid refresh token");
            }

            string? userId;
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = BuildLocalTokenValidationParameters(validateLifetime: false);

                ClaimsPrincipal principal = tokenHandler.ValidateToken(accessToken, validationParameters, out _);
                userId = principal.Claims.FirstOrDefault(c => c.Type is JwtRegisteredClaimNames.Sub
                        or ClaimTypes.NameIdentifier
                        or "sub"
                        or "uid")
                    ?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate/parse access token during refresh (dev auth).");
                return (null, null, null, "Invalid access token");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return (null, null, null, "Invalid access token");
            }

            var users = await LoadUsersAsync();
            var user = users.FirstOrDefault(u => u.AuthId == userId);
            if (user == null)
            {
                return (null, null, null, "User not found in dev");
            }
            var jwt = GenerateJwt(user);
            return (user, jwt, _mockRefreshToken, string.Empty);
        }

        public Task<TokenValidationParameters> GetAccessTokenValidationParameters()
        {
            return Task.FromResult(BuildLocalTokenValidationParameters(validateLifetime: true));
        }

        private TokenValidationParameters BuildLocalTokenValidationParameters(bool validateLifetime)
        {
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = LocalJwtIssuer,
                ValidateAudience = true,
                ValidAudience = LocalJwtAudience,
                ValidateLifetime = validateLifetime,
                ClockSkew = TimeSpan.FromMinutes(validateLifetime ? 2 : 1),
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey))
            };
        }
    }
}
