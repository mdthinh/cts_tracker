namespace CmcTs.Web.Auth;

public static class AppClaimTypes
{
    // Tài khoản AD gốc (sAMAccountName) — dùng khi cần tra cứu/hiển thị lại, khác với NameIdentifier (User.Id nội bộ).
    public const string SamAccountName = "cmcts:sam";
}
