using CmcTs.Core.Data;
using CmcTs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CmcTs.Core.Services;

public class ProjectStatusService : IProjectStatusService
{
    private readonly IDbContextFactory<CmcTsDbContext> _dbFactory;

    public ProjectStatusService(IDbContextFactory<CmcTsDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task ChangeStatusAsync(int projectId, ProjectStatus newStatus, int changedByUserId, CancellationToken ct = default)
    {
        if (newStatus == ProjectStatus.Completed)
        {
            throw new InvalidOperationException("Dùng chức năng \"Đánh dấu hoàn thành\" để chuyển dự án sang trạng thái Hoàn thành.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var project = await db.Projects.SingleAsync(p => p.Id == projectId, ct);

        if (project.Status == newStatus)
        {
            return;
        }

        // Không tự kiểm tra quyền ở đây (giống ProjectCompletionService) — trang gọi vào đã xác
        // thực bằng ITaskPermissionService.CanEditProjectAsync trước, luật khóa dự án Hoàn thành
        // (chỉ Admin sửa được) đã nằm sẵn trong đó nên "mở lại" từ Completed tự động chỉ Admin làm
        // được, không cần thêm điều kiện riêng ở service này.
        db.AuditLogs.Add(new AuditLog
        {
            EntityType = nameof(Project),
            EntityId = projectId,
            FieldName = nameof(Project.Status),
            OldValue = project.Status.ToString(),
            NewValue = newStatus.ToString(),
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
        });

        project.Status = newStatus;
        await db.SaveChangesAsync(ct);
    }
}
