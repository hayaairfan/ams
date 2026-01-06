using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using AttendanceSystem.Business;

namespace AttendanceSystem.Web.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // 🔑 MUST match the cookie name in Login/Logout
            var token = context.Request.Cookies["auth_token"];

            if (!string.IsNullOrEmpty(token))
            {
                var result = JwtHelper.ValidateToken(token);
                if (result != null)
                {
                    var (email, role) = result.Value;

                    // ✅ CRITICAL: Set authenticated identity
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, email),
                        new Claim(ClaimTypes.Role, role)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, "jwt");
                    context.User = new ClaimsPrincipal(claimsIdentity);
                }
            }

            await _next(context);
        }
    }

    public static class JwtMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtMiddleware>();
        }
    }
}