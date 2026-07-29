namespace CmcTs.Core.Options;

// Tài khoản admin "break-glass" không phụ thuộc AD — hữu ích khi AD chưa sẵn sàng lúc mới deploy,
// hoặc khi LDAP server gặp sự cố. Chỉ tạo 1 lần nếu chưa tồn tại (không tự reset lại mật khẩu mỗi
// lần khởi động). Password KHÔNG lưu trong appsettings.json commit vào git — dùng
// `dotnet user-secrets set "LocalAdmin:Password" "..."` khi dev, hoặc biến môi trường
// LocalAdmin__Password khi chạy trên VM.
public class LocalAdminOptions
{
    public const string SectionName = "LocalAdmin";

    public string? Username { get; set; }
    public string? Password { get; set; }
}
