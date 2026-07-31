namespace CmcTs.Core.Services;

public interface ITaskPermissionService
{
    // true nếu user là Admin, hoặc Trưởng dự án của dự án chứa task, hoặc là AssigneeUserId của
    // chính task đó hay 1 tổ tiên của nó trong cây (được gán vào nhánh cha thì coi như phụ trách
    // luôn các task con bên trong nhánh).
    Task<bool> CanEditTaskAsync(int taskId, int userId, bool isGlobalAdmin, CancellationToken ct = default);

    // true nếu user là Admin hoặc Trưởng dự án của chính dự án đó (sửa được toàn bộ thông tin dự án).
    Task<bool> CanEditProjectAsync(int projectId, int userId, bool isGlobalAdmin, CancellationToken ct = default);

    // Trả về tập Id các task trong dự án mà user được phép sửa (gán phụ trách / báo cáo công việc) —
    // dùng ở các trang hiển thị cả cây/danh sách task, tính 1 lần thay vì gọi CanEditTaskAsync riêng
    // cho từng task. Admin/Trưởng dự án -> toàn bộ task; còn lại -> chính task được gán + mọi task
    // con của nó (gán vào nhánh cha thì coi như phụ trách luôn nhánh con bên trong).
    Task<HashSet<int>> GetEditableTaskIdsAsync(int projectId, int userId, bool isGlobalAdmin, CancellationToken ct = default);

    // true nếu user là Admin, hoặc là Members của dự án (Trưởng dự án luôn nằm trong Members nên
    // không cần điều kiện riêng) — dùng để CHẶN XEM (không chỉ sửa) trang chi tiết/cây công việc/
    // báo cáo/tài liệu của dự án mà user không tham gia, kể cả khi gõ thẳng URL.
    Task<bool> CanViewProjectAsync(int projectId, int userId, bool isGlobalAdmin, CancellationToken ct = default);
}
