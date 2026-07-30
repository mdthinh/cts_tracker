using CmcTs.Core.Data;
using CmcTs.Core.Entities;
using CmcTs.Core.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CmcTs.Core.Tests;

public class TaskPermissionServiceTests
{
    private static (IDbContextFactory<CmcTsDbContext> factory, int projectId, int leadUserId,
        int branchAssigneeUserId, int otherUserId, int level1Id, int level2Id, int leafId) Seed()
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = new TestDbContextFactory(dbName);

        using var db = factory.CreateDbContext();

        var lead = new User { SamAccountName = "lead", DisplayName = "Lead", CreatedAt = DateTime.UtcNow };
        var branchAssignee = new User { SamAccountName = "branch", DisplayName = "Branch Assignee", CreatedAt = DateTime.UtcNow };
        var other = new User { SamAccountName = "other", DisplayName = "Other", CreatedAt = DateTime.UtcNow };
        db.Users.AddRange(lead, branchAssignee, other);
        db.SaveChanges();

        var project = new Project
        {
            Name = "Test", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.ENT,
            ProjectLeadUserId = lead.Id, CreatedByUserId = lead.Id, CreatedAt = DateTime.UtcNow,
        };
        db.Projects.Add(project);
        db.SaveChanges();

        var level1 = new TaskItem { ProjectId = project.Id, Level = TaskLevel.Level1, Code = "1", Name = "L1", SourceRow = 1 };
        db.Tasks.Add(level1);
        db.SaveChanges();

        // Level2 được gán cho branchAssignee — user này phải sửa được cả leaf con dù không gán trực tiếp.
        var level2 = new TaskItem { ProjectId = project.Id, ParentTaskId = level1.Id, Level = TaskLevel.Level2, Code = "1.1", Name = "L2", SourceRow = 2, AssigneeUserId = branchAssignee.Id };
        db.Tasks.Add(level2);
        db.SaveChanges();

        var leaf = new TaskItem { ProjectId = project.Id, ParentTaskId = level2.Id, Level = TaskLevel.Level3, Name = "Leaf", SourceRow = 3 };
        db.Tasks.Add(leaf);
        db.SaveChanges();

        return (factory, project.Id, lead.Id, branchAssignee.Id, other.Id, level1.Id, level2.Id, leaf.Id);
    }

    [Fact]
    public async Task GlobalAdmin_CanAlwaysEdit()
    {
        var (factory, _, _, _, otherUserId, level1Id, _, _) = Seed();
        var service = new TaskPermissionService(factory);

        Assert.True(await service.CanEditTaskAsync(level1Id, otherUserId, isGlobalAdmin: true));
    }

    [Fact]
    public async Task ProjectLead_CanEditAnyTaskInProject()
    {
        var (factory, _, leadUserId, _, _, level1Id, _, leafId) = Seed();
        var service = new TaskPermissionService(factory);

        Assert.True(await service.CanEditTaskAsync(level1Id, leadUserId, isGlobalAdmin: false));
        Assert.True(await service.CanEditTaskAsync(leafId, leadUserId, isGlobalAdmin: false));
    }

    [Fact]
    public async Task BranchAssignee_CanEditOwnTaskAndDescendants_ButNotSiblingBranch()
    {
        var (factory, _, _, branchAssigneeUserId, _, level1Id, level2Id, leafId) = Seed();
        var service = new TaskPermissionService(factory);

        Assert.True(await service.CanEditTaskAsync(level2Id, branchAssigneeUserId, isGlobalAdmin: false));
        Assert.True(await service.CanEditTaskAsync(leafId, branchAssigneeUserId, isGlobalAdmin: false)); // con của nhánh được gán
        Assert.False(await service.CanEditTaskAsync(level1Id, branchAssigneeUserId, isGlobalAdmin: false)); // cha của nhánh được gán, không phải nhánh của mình
    }

    [Fact]
    public async Task UnrelatedUser_CannotEdit()
    {
        var (factory, _, _, _, otherUserId, level1Id, level2Id, leafId) = Seed();
        var service = new TaskPermissionService(factory);

        Assert.False(await service.CanEditTaskAsync(level1Id, otherUserId, isGlobalAdmin: false));
        Assert.False(await service.CanEditTaskAsync(level2Id, otherUserId, isGlobalAdmin: false));
        Assert.False(await service.CanEditTaskAsync(leafId, otherUserId, isGlobalAdmin: false));
    }

    [Fact]
    public async Task GetEditableTaskIds_BranchAssignee_ReturnsOwnNodePlusDescendantsOnly()
    {
        var (factory, projectId, _, branchAssigneeUserId, _, level1Id, level2Id, leafId) = Seed();
        var service = new TaskPermissionService(factory);

        var ids = await service.GetEditableTaskIdsAsync(projectId, branchAssigneeUserId, isGlobalAdmin: false);

        Assert.Equal(new HashSet<int> { level2Id, leafId }, ids);
        Assert.DoesNotContain(level1Id, ids);
    }

    [Fact]
    public async Task GetEditableTaskIds_ProjectLead_ReturnsEverything()
    {
        var (factory, projectId, leadUserId, _, _, level1Id, level2Id, leafId) = Seed();
        var service = new TaskPermissionService(factory);

        var ids = await service.GetEditableTaskIdsAsync(projectId, leadUserId, isGlobalAdmin: false);

        Assert.Equal(new HashSet<int> { level1Id, level2Id, leafId }, ids);
    }

    // Regression test cho bug thật đã gặp: khi so sánh trực tiếp 2 giá trị int? (thay vì int? với
    // int như service này làm), "AssigneeUserId == CurrentUserId" với cả 2 đều null bị C# coi là
    // true, khiến user không liên quan gì được cấp quyền sửa MỌI task chưa gán ai. Ở đây userId là
    // int không nullable nên không thể dính lỗi này — test xác nhận user không liên quan luôn nhận
    // được tập rỗng dù toàn bộ task trong dự án đều chưa gán cho ai.
    [Fact]
    public async Task GetEditableTaskIds_UnrelatedUser_ReturnsEmptySet_EvenWhenAllTasksUnassigned()
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = new TestDbContextFactory(dbName);
        int projectId, otherUserId;

        using (var db = factory.CreateDbContext())
        {
            var lead = new User { SamAccountName = "lead", DisplayName = "Lead", CreatedAt = DateTime.UtcNow };
            var other = new User { SamAccountName = "other", DisplayName = "Other", CreatedAt = DateTime.UtcNow };
            db.Users.AddRange(lead, other);
            db.SaveChanges();
            otherUserId = other.Id;

            var project = new Project { Name = "Test", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.ENT, ProjectLeadUserId = lead.Id, CreatedByUserId = lead.Id, CreatedAt = DateTime.UtcNow };
            db.Projects.Add(project);
            db.SaveChanges();
            projectId = project.Id;

            // Không gán AssigneeUserId cho task nào cả — mô phỏng đúng tình huống bug thật gặp phải.
            db.Tasks.AddRange(
                new TaskItem { ProjectId = projectId, Level = TaskLevel.Level1, Code = "1", Name = "L1", SourceRow = 1 },
                new TaskItem { ProjectId = projectId, Level = TaskLevel.Level2, Code = "1.1", Name = "L2", SourceRow = 2 });
            db.SaveChanges();
        }

        var service = new TaskPermissionService(factory);
        var ids = await service.GetEditableTaskIdsAsync(projectId, otherUserId, isGlobalAdmin: false);

        Assert.Empty(ids);
    }
}
