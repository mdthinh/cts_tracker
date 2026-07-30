using CmcTs.Core.Data;
using CmcTs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CmcTs.Core.Services;

public class TaskPermissionService : ITaskPermissionService
{
    private readonly IDbContextFactory<CmcTsDbContext> _dbFactory;

    public TaskPermissionService(IDbContextFactory<CmcTsDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<bool> CanEditTaskAsync(int taskId, int userId, bool isGlobalAdmin, CancellationToken ct = default)
    {
        if (isGlobalAdmin)
        {
            return true;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var task = await db.Tasks.FindAsync(new object?[] { taskId }, ct);
        if (task is null)
        {
            return false;
        }

        var project = await db.Projects.FindAsync(new object?[] { task.ProjectId }, ct);
        if (project is null || IsLockedForNonAdmin(project))
        {
            return false;
        }

        if (project.ProjectLeadUserId == userId)
        {
            return true;
        }

        // Load hết task của dự án 1 lần để đi ngược lên tổ tiên trong bộ nhớ (cây thường nhỏ,
        // rẻ hơn nhiều so với query đệ quy nhiều lượt).
        var allTasks = await db.Tasks.Where(t => t.ProjectId == task.ProjectId).ToListAsync(ct);
        var byId = allTasks.ToDictionary(t => t.Id);

        var current = byId.GetValueOrDefault(taskId);
        while (current is not null)
        {
            if (current.AssigneeUserId == userId)
            {
                return true;
            }
            current = current.ParentTaskId is int parentId ? byId.GetValueOrDefault(parentId) : null;
        }

        return false;
    }

    public async Task<bool> CanEditProjectAsync(int projectId, int userId, bool isGlobalAdmin, CancellationToken ct = default)
    {
        if (isGlobalAdmin)
        {
            return true;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var project = await db.Projects.FindAsync(new object?[] { projectId }, ct);
        if (project is null || IsLockedForNonAdmin(project))
        {
            return false;
        }

        return project.ProjectLeadUserId == userId;
    }

    public async Task<HashSet<int>> GetEditableTaskIdsAsync(int projectId, int userId, bool isGlobalAdmin, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var allTasks = await db.Tasks.Where(t => t.ProjectId == projectId).ToListAsync(ct);

        if (isGlobalAdmin)
        {
            return allTasks.Select(t => t.Id).ToHashSet();
        }

        var project = await db.Projects.FindAsync(new object?[] { projectId }, ct);
        if (project is null || IsLockedForNonAdmin(project))
        {
            return new HashSet<int>();
        }

        if (project.ProjectLeadUserId == userId)
        {
            return allTasks.Select(t => t.Id).ToHashSet();
        }

        var editable = new HashSet<int>();
        void AddWithDescendants(TaskItem node)
        {
            editable.Add(node.Id);
            foreach (var child in node.Children)
            {
                AddWithDescendants(child);
            }
        }

        // userId ở đây luôn là số thật (tham số int không nullable) nên phép so sánh với
        // AssigneeUserId (int?) không bao giờ dính lỗi "null == null" như khi so 2 giá trị int?
        // với nhau — đây chính xác là bug thật đã gặp ở bản trước khi 2 trang Razor tự viết tay
        // logic này với biến CurrentUserId kiểu int?.
        foreach (var task in allTasks.Where(t => t.AssigneeUserId == userId))
        {
            AddWithDescendants(task);
        }

        return editable;
    }

    // Dự án đã "Hoàn thành" thì khóa sửa với tất cả trừ Admin (đảm bảo doanh thu đã chốt không bị
    // đổi ngầm sau khi ghi nhận vào quý tài chính).
    private static bool IsLockedForNonAdmin(Project project) => project.Status == ProjectStatus.Completed;
}
