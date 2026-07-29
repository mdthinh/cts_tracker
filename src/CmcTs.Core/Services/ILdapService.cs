namespace CmcTs.Core.Services;

public interface ILdapService
{
    // Xác thực username/password với AD. Trả về thông tin user nếu đúng, null nếu sai mật khẩu/không tồn tại.
    Task<AdUserInfo?> AuthenticateAsync(string samAccountName, string password, CancellationToken ct = default);

    // Tìm user theo tên/username (dùng cho ô "tìm kiếm AD" khi thêm thành viên/gán phụ trách).
    Task<IReadOnlyList<AdUserInfo>> SearchUsersAsync(string searchTerm, int maxResults = 20, CancellationToken ct = default);
}
