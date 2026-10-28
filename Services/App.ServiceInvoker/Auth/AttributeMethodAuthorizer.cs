using App.ServiceInvoker.Reflection;
using Microsoft.AspNetCore.Http;

namespace App.ServiceInvoker.Auth
{
    public class AttributeMethodAuthorizer : IMethodAuthorizer
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AttributeMethodAuthorizer(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Authorize(MethodMetadata methodMetadata)
        {
            var context = _httpContextAccessor.HttpContext;
            var user = context?.User;

            var requiresAuth = methodMetadata.RequireAuthenticated != null || methodMetadata.RequireRole != null;
            if (!requiresAuth)
            {
                return;
            }

            if (user?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException("Authentication required.");
            }

            var roleAttr = methodMetadata.RequireRole;
            if (roleAttr == null || roleAttr.Roles.Length == 0)
            {
                return;
            }

            var roles = RoleClaimsReader.GetRoles(user);
            if (!roleAttr.Roles.Any(r => roles.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                // 文案直接回传给前端展示：只写「Forbidden」时界面无法告诉用户该找谁开通。
                throw new ForbiddenAccessException($"当前账号无权执行该操作（需要角色：{string.Join(" / ", roleAttr.Roles)}）。");
            }
        }
    }
}
