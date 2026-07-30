using CmcTs.Core.Data;
using CmcTs.Core.Entities;
using CmcTs.Core.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CmcTs.Core.Tests;

public class TaskProgressServiceTests
{
    [Fact]
    public async Task ReportWorkAsync_UpdatesLeafAndRollsUpWeightedProgressToAncestors()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        int projectId, level1Id, level2Id, leafAId, leafBId, reporterId;

        using (var db = factory.CreateDbContext())
        {
            var reporter = new User { SamAccountName = "u1", DisplayName = "U1", CreatedAt = DateTime.UtcNow };
            db.Users.Add(reporter);
            db.SaveChanges();
            reporterId = reporter.Id;

            var project = new Project { Name = "Test", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.ENT, CreatedByUserId = reporter.Id, CreatedAt = DateTime.UtcNow };
            db.Projects.Add(project);
            db.SaveChanges();
            projectId = project.Id;

            var level1 = new TaskItem { ProjectId = projectId, Level = TaskLevel.Level1, Code = "1", Name = "L1", SourceRow = 1 };
            db.Tasks.Add(level1);
            db.SaveChanges();
            level1Id = level1.Id;

            var level2 = new TaskItem { ProjectId = projectId, ParentTaskId = level1Id, Level = TaskLevel.Level2, Code = "1.1", Name = "L2", SourceRow = 2 };
            db.Tasks.Add(level2);
            db.SaveChanges();
            level2Id = level2.Id;

            // 2 leaf khối lượng khác nhau (3 manday và 1 manday) — rollup Progress phải theo trọng số,
            // không phải trung bình cộng đơn giản, để phân biệt được với lỗi tính sai.
            var leafA = new TaskItem { ProjectId = projectId, ParentTaskId = level2Id, Level = TaskLevel.Level3, Name = "A", SourceRow = 3, MandayPlan = 3m, Progress = 0 };
            var leafB = new TaskItem { ProjectId = projectId, ParentTaskId = level2Id, Level = TaskLevel.Level3, Name = "B", SourceRow = 4, MandayPlan = 1m, Progress = 0 };
            db.Tasks.AddRange(leafA, leafB);
            db.SaveChanges();
            leafAId = leafA.Id;
            leafBId = leafB.Id;
        }

        var service = new TaskProgressService(factory);

        // leafA (trọng số 3) hoàn thành 100%, leafB (trọng số 1) vẫn 0% -> kỳ vọng rollup = 75%
        // (3*100 + 1*0) / 4 = 75, KHÔNG phải (100+0)/2 = 50 như trung bình cộng thường.
        await service.ReportWorkAsync(leafAId, reporterId, DateOnly.FromDateTime(DateTime.UtcNow), 100, 3m, "done", CancellationToken.None);

        using (var verifyDb = factory.CreateDbContext())
        {
            var leafA = await verifyDb.Tasks.SingleAsync(t => t.Id == leafAId);
            Assert.Equal(100, leafA.Progress);
            Assert.Equal(3m, leafA.MandayActual);

            var level2 = await verifyDb.Tasks.SingleAsync(t => t.Id == level2Id);
            Assert.Equal(75, level2.Progress);
            Assert.Equal(3m, level2.MandayActual);

            var level1 = await verifyDb.Tasks.SingleAsync(t => t.Id == level1Id);
            Assert.Equal(75, level1.Progress);
            Assert.Equal(3m, level1.MandayActual);

            var report = await verifyDb.WorkReports.SingleAsync(r => r.TaskId == leafAId);
            Assert.Equal(reporterId, report.ReportedByUserId);
            Assert.Equal("done", report.Note);
        }
    }

    [Fact]
    public async Task ReportWorkAsync_FallsBackToSimpleAverage_WhenAllChildrenArePackagesWithZeroPlan()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        int level2Id, leafAId, reporterId;

        using (var db = factory.CreateDbContext())
        {
            var reporter = new User { SamAccountName = "u1", DisplayName = "U1", CreatedAt = DateTime.UtcNow };
            db.Users.Add(reporter);
            db.SaveChanges();
            reporterId = reporter.Id;

            var project = new Project { Name = "Test", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.ENT, CreatedByUserId = reporter.Id, CreatedAt = DateTime.UtcNow };
            db.Projects.Add(project);
            db.SaveChanges();

            var level2 = new TaskItem { ProjectId = project.Id, Level = TaskLevel.Level2, Code = "1.1", Name = "L2", SourceRow = 1 };
            db.Tasks.Add(level2);
            db.SaveChanges();
            level2Id = level2.Id;

            // 2 leaf "Gói" (MandayPlan = 0) — nếu không chặn chia-cho-0 sẽ throw hoặc ra NaN.
            var leafA = new TaskItem { ProjectId = project.Id, ParentTaskId = level2Id, Level = TaskLevel.Level3, Name = "A", SourceRow = 2, MandayPlan = 0m, CostType = TaskCostType.Package };
            var leafB = new TaskItem { ProjectId = project.Id, ParentTaskId = level2Id, Level = TaskLevel.Level3, Name = "B", SourceRow = 3, MandayPlan = 0m, CostType = TaskCostType.Package };
            db.Tasks.AddRange(leafA, leafB);
            db.SaveChanges();
            leafAId = leafA.Id;
        }

        var service = new TaskProgressService(factory);
        await service.ReportWorkAsync(leafAId, reporterId, DateOnly.FromDateTime(DateTime.UtcNow), 50, 0m, null, CancellationToken.None);

        using var verifyDb = factory.CreateDbContext();
        var level2Loaded = await verifyDb.Tasks.SingleAsync(t => t.Id == level2Id);
        Assert.Equal(25, level2Loaded.Progress); // (50 + 0) / 2 = 25, trung bình cộng khi tổng MandayPlan = 0
    }
}
