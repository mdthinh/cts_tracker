using CmcTs.Core.Entities;

namespace CmcTs.Core.Services;

public interface IUserProvisioningService
{
    // Gọi mỗi lần đăng nhập thành công: tạo mới User nếu chưa có, hoặc đồng bộ lại
    // DisplayName/Email/Department mới nhất từ AD. Không tự hạ quyền Admin đã gán trong DB.
    Task<User> ProvisionOnLoginAsync(AdUserInfo adUser, CancellationToken ct = default);

    // Dùng khi Admin chọn 1 user từ kết quả tìm AD (thêm thành viên dự án, gán quyền...)
    // trước khi người đó từng tự đăng nhập lần nào — tạo 1 bản ghi tối thiểu (role Viewer)
    // để có Id tham chiếu được, không cập nhật gì nếu user đã tồn tại.
    Task<User> GetOrCreateStubAsync(AdUserInfo adUser, CancellationToken ct = default);
}
