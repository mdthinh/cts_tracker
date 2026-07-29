using CmcTs.Core.Entities;

namespace CmcTs.Core.Services;

public interface ILocalAccountService
{
    // Gọi lúc khởi động app. Chỉ tạo tài khoản admin local nếu chưa tồn tại và có cấu hình
    // LocalAdmin:Username/Password — không làm gì nếu tài khoản đã có sẵn (không ghi đè mật khẩu).
    Task EnsureSeededAsync(CancellationToken ct = default);

    // Xác thực username/password với tài khoản local. Trả về null nếu không phải tài khoản local
    // (để AuthEndpoints thử tiếp qua AD) hoặc sai mật khẩu.
    Task<User?> ValidateAsync(string username, string password, CancellationToken ct = default);
}
