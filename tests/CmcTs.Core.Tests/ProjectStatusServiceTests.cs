using CmcTs.Core.Entities;
using CmcTs.Core.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CmcTs.Core.Tests;

public class ProjectStatusServiceTests
{
    private static (TestDbContextFactory factory, int projectId, int userId) Seed(ProjectStatus initialStatus)
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        int projectId, userId;

        using (var db = factory.CreateDbContext())
        {
            var user = new User { SamAccountName = "u", DisplayName = "U", CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
            db.SaveChanges();
            userId = user.Id;

            var project = new Project
            {
                Name = "Test", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.ENT,
                Status = initialStatus, CreatedByUserId = user.Id, CreatedAt = DateTime.UtcNow,
            };
            db.Projects.Add(project);
            db.SaveChanges();
            projectId = project.Id;
        }

        return (factory, projectId, userId);
    }

    [Fact]
    public async Task ChangeStatusAsync_UpdatesStatus_AndWritesAuditLog()
    {
        var (factory, projectId, userId) = Seed(ProjectStatus.OnTrack);

        var service = new ProjectStatusService(factory);
        await service.ChangeStatusAsync(projectId, ProjectStatus.AtRisk, userId);

        using var verifyDb = factory.CreateDbContext();
        var project = await verifyDb.Projects.SingleAsync(p => p.Id == projectId);
        Assert.Equal(ProjectStatus.AtRisk, project.Status);

        var audit = await verifyDb.AuditLogs.SingleAsync(a => a.EntityType == nameof(Project) && a.EntityId == projectId);
        Assert.Equal(nameof(Project.Status), audit.FieldName);
        Assert.Equal(nameof(ProjectStatus.OnTrack), audit.OldValue);
        Assert.Equal(nameof(ProjectStatus.AtRisk), audit.NewValue);
        Assert.Equal(userId, audit.ChangedByUserId);
    }

    [Fact]
    public async Task ChangeStatusAsync_Throws_WhenTargetIsCompleted()
    {
        var (factory, projectId, userId) = Seed(ProjectStatus.OnTrack);

        var service = new ProjectStatusService(factory);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChangeStatusAsync(projectId, ProjectStatus.Completed, userId));
    }

    [Fact]
    public async Task ChangeStatusAsync_AllowsMovingAwayFromCompleted_ReopeningTheProject()
    {
        // Bản thân service không tự chặn việc "mở lại" dự án Completed — quyền này được chặn ở
        // tầng gọi vào (CanEditProjectAsync chỉ trả true cho Admin khi dự án đã Completed), không
        // phải ở đây. Test này xác nhận service cho phép transition này khi được gọi tới.
        var (factory, projectId, userId) = Seed(ProjectStatus.Completed);

        var service = new ProjectStatusService(factory);
        await service.ChangeStatusAsync(projectId, ProjectStatus.OnTrack, userId);

        using var verifyDb = factory.CreateDbContext();
        var project = await verifyDb.Projects.SingleAsync(p => p.Id == projectId);
        Assert.Equal(ProjectStatus.OnTrack, project.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_NoOp_WhenStatusUnchanged()
    {
        var (factory, projectId, userId) = Seed(ProjectStatus.Delayed);

        var service = new ProjectStatusService(factory);
        await service.ChangeStatusAsync(projectId, ProjectStatus.Delayed, userId);

        using var verifyDb = factory.CreateDbContext();
        Assert.Empty(await verifyDb.AuditLogs.ToListAsync());
    }
}
