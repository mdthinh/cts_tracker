namespace CmcTs.Core.Services;

public interface ITaskPermissionService
{
    // true nếu user là Admin, hoặc Trưởng dự án của dự án chứa task, hoặc là AssigneeUserId của
    // chính task đó hay 1 tổ tiên của nó trong cây (được gán vào nhánh cha thì coi như phụ trách
    // luôn các task con bên trong nhánh).
    Task<bool> CanEditTaskAsync(int taskId, int userId, bool isGlobalAdmin, CancellationToken ct = default);

    // true nếu user là Admin hoặc Trưởng dự án của chính dự án đó (sửa được toàn bộ thông tin dự án).
    Task<bool> CanEditProjectAsync(int projectId, int userId, bool isGlobalAdmin, CancellationToken ct = default);
}
