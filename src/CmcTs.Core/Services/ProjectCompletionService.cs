using CmcTs.Core.Data;
using CmcTs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CmcTs.Core.Services;

public class ProjectCompletionService : IProjectCompletionService
{
    private readonly IDbContextFactory<CmcTsDbContext> _dbFactory;

    public ProjectCompletionService(IDbContextFactory<CmcTsDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task CompleteProjectAsync(int projectId, int completedByUserId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var project = await db.Projects.SingleAsync(p => p.Id == projectId, ct);

        if (project.Status == ProjectStatus.Completed)
        {
            throw new InvalidOperationException("Dự án đã được đánh dấu hoàn thành từ trước.");
        }

        var now = DateTime.UtcNow;

        db.AuditLogs.Add(new AuditLog
        {
            EntityType = nameof(Project),
            EntityId = projectId,
            FieldName = nameof(Project.Status),
            OldValue = project.Status.ToString(),
            NewValue = ProjectStatus.Completed.ToString(),
            ChangedByUserId = completedByUserId,
            ChangedAt = now,
        });

        project.Status = ProjectStatus.Completed;
        project.CompletedAt = now;
        project.CompletedByUserId = completedByUserId;

        await db.SaveChangesAsync(ct);
    }
}
