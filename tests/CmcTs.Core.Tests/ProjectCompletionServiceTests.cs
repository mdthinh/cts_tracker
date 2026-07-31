using CmcTs.Core.Entities;
using CmcTs.Core.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CmcTs.Core.Tests;

public class ProjectCompletionServiceTests
{
    [Fact]
    public async Task CompleteProjectAsync_SetsStatusCompletedAtAndByUser_AndWritesAuditLog()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        int projectId, adminUserId;

        using (var db = factory.CreateDbContext())
        {
            var admin = new User { SamAccountName = "admin", DisplayName = "Admin", CreatedAt = DateTime.UtcNow };
            db.Users.Add(admin);
            db.SaveChanges();
            adminUserId = admin.Id;

            var project = new Project
            {
                Name = "Test", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.ENT,
                Status = ProjectStatus.OnTrack, RevenueAmount = 786_480_000m,
                CreatedByUserId = admin.Id, CreatedAt = DateTime.UtcNow,
            };
            db.Projects.Add(project);
            db.SaveChanges();
            projectId = project.Id;
        }

        var service = new ProjectCompletionService(factory);
        await service.CompleteProjectAsync(projectId, adminUserId);

        using var verifyDb = factory.CreateDbContext();
        var project2 = await verifyDb.Projects.SingleAsync(p => p.Id == projectId);
        Assert.Equal(ProjectStatus.Completed, project2.Status);
        Assert.NotNull(project2.CompletedAt);
        Assert.Equal(adminUserId, project2.CompletedByUserId);
        Assert.Equal(786_480_000m, project2.RevenueAmount); // không đổi doanh thu đã chốt lúc import

        var audit = await verifyDb.AuditLogs.SingleAsync(a => a.EntityType == nameof(Project) && a.EntityId == projectId);
        Assert.Equal(nameof(Project.Status), audit.FieldName);
        Assert.Equal(nameof(ProjectStatus.OnTrack), audit.OldValue);
        Assert.Equal(nameof(ProjectStatus.Completed), audit.NewValue);
        Assert.Equal(adminUserId, audit.ChangedByUserId);
    }

    [Fact]
    public async Task CompleteProjectAsync_Throws_WhenAlreadyCompleted()
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
                Status = ProjectStatus.Completed, CompletedAt = DateTime.UtcNow, CompletedByUserId = user.Id,
                CreatedByUserId = user.Id, CreatedAt = DateTime.UtcNow,
            };
            db.Projects.Add(project);
            db.SaveChanges();
            projectId = project.Id;
        }

        var service = new ProjectCompletionService(factory);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteProjectAsync(projectId, userId));
    }
}
