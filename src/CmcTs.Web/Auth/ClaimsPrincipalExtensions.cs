using System.Security.Claims;

namespace CmcTs.Web.Auth;

public static class ClaimsPrincipalExtensions
{
    // User.Id nội bộ (bảng Users), không phải tài khoản AD.
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }

    public static string? GetSamAccountName(this ClaimsPrincipal principal)
        => principal.FindFirstValue(AppClaimTypes.SamAccountName);

    public static bool IsAdmin(this ClaimsPrincipal principal)
        => principal.IsInRole(nameof(CmcTs.Core.Entities.GlobalRole.Admin));
}
