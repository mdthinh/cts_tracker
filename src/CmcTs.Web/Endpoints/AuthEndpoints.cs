using System.Security.Claims;
using CmcTs.Core.Services;
using CmcTs.Web.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CmcTs.Web.Endpoints;

// Đăng nhập/đăng xuất cố ý KHÔNG đi qua Blazor Server component: ghi cookie xác thực cần 1 HTTP
// response bình thường, trong khi 1 component Interactive Server chạy trên kết nối SignalR đã mở
// nên không set cookie theo cách thông thường được. Đây là pattern chuẩn của Blazor Web App khi
// dùng cookie auth thủ công (không dùng ASP.NET Core Identity UI).
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async (
            HttpContext http,
            ILdapService ldap,
            IUserProvisioningService provisioning) =>
        {
            var form = await http.Request.ReadFormAsync();
            var username = form["username"].ToString().Trim();
            var password = form["password"].ToString();
            var returnUrl = SafeLocalUrl(form["returnUrl"].ToString());

            var adUser = await ldap.AuthenticateAsync(username, password, http.RequestAborted);
            if (adUser is null)
            {
                return Results.Redirect($"/login?error=invalid&returnUrl={Uri.EscapeDataString(returnUrl)}");
            }

            var user = await provisioning.ProvisionOnLoginAsync(adUser, http.RequestAborted);
            if (!user.IsActive)
            {
                return Results.Redirect("/login?error=disabled");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.DisplayName),
                new(ClaimTypes.Role, user.GlobalRole.ToString()),
                new(AppClaimTypes.SamAccountName, user.SamAccountName),
            };
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await http.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            return Results.Redirect(returnUrl);
        }).DisableAntiforgery().AllowAnonymous();

        app.MapPost("/auth/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        }).DisableAntiforgery().AllowAnonymous();
    }

    // Chỉ chấp nhận đường dẫn nội bộ (bắt đầu bằng "/", không phải "//" hay "/\") để tránh open redirect.
    private static string SafeLocalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith('/') || url.StartsWith("//") || url.StartsWith("/\\"))
        {
            return "/";
        }
        return url;
    }
}
