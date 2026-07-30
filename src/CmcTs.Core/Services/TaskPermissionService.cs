using CmcTs.Core.Data;
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
        if (project?.ProjectLeadUserId == userId)
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
        return project?.ProjectLeadUserId == userId;
    }
}
