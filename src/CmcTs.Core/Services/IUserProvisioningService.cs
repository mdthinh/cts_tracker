using CmcTs.Core.Entities;

namespace CmcTs.Core.Services;

public interface IUserProvisioningService
{
    // Gọi mỗi lần đăng nhập thành công: tạo mới User nếu chưa có, hoặc đồng bộ lại
    // DisplayName/Email/Department mới nhất từ AD. Không tự hạ quyền Admin đã gán trong DB.
    Task<User> ProvisionOnLoginAsync(AdUserInfo adUser, CancellationToken ct = default);
}
