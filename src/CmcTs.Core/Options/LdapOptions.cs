namespace CmcTs.Core.Options;

// Đọc từ appsettings + User Secrets ("Ldap" section). ServiceAccountPassword KHÔNG lưu trong
// appsettings.json commit vào git — dùng `dotnet user-secrets set "Ldap:ServiceAccountPassword" "..."`
// khi dev, hoặc biến môi trường Ldap__ServiceAccountPassword khi chạy trên VM.
public class LdapOptions
{
    public const string SectionName = "Ldap";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 389;
    public string BaseDn { get; set; } = string.Empty;

    // Tài khoản chỉ có quyền đọc, dùng để bind + tìm kiếm user (không phải để đăng nhập người dùng cuối).
    public string ServiceAccountUsername { get; set; } = string.Empty;
    public string ServiceAccountPassword { get; set; } = string.Empty;

    // Danh sách sAMAccountName sẽ tự động được nâng lên Admin khi đăng nhập lần đầu (bootstrap).
    // Chỉ nâng quyền, không bao giờ tự hạ quyền Admin đã gán trong DB.
    public string[] InitialAdminSamAccountNames { get; set; } = Array.Empty<string>();
}
