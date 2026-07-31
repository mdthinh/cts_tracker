using CmcTs.Core.Entities;
using CmcTs.Core.Services;
using Xunit;

namespace CmcTs.Core.Tests;

public class PersonnelStatsServiceTests
{
    [Fact]
    public async Task GetAllAsync_SumsMandayByReporterAndFiscalQuarter_RegardlessOfProjectStatus()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        int userId;

        using (var db = factory.CreateDbContext())
        {
            var user = new User { SamAccountName = "u", DisplayName = "U", CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
            db.SaveChanges();
            userId = user.Id;

            var project = new Project { Name = "P", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.ENT, CreatedByUserId = user.Id, CreatedAt = DateTime.UtcNow };
            db.Projects.Add(project);
            db.SaveChanges();

            var task = new TaskItem { ProjectId = project.Id, Level = TaskLevel.Level3, Name = "T", SourceRow = 1 };
            db.Tasks.Add(task);
            db.SaveChanges();

            // Cùng NTC 2026-2027 nhưng khác quý: Q1 (T4-T6) và Q2 (T7-T9).
            db.WorkReports.Add(new WorkReport { TaskId = task.Id, ReportedByUserId = userId, ReportDate = new DateOnly(2026, 5, 1), ProgressPercent = 20, MandayReported = 3m, CreatedAt = DateTime.UtcNow });
            db.WorkReports.Add(new WorkReport { TaskId = task.Id, ReportedByUserId = userId, ReportDate = new DateOnly(2026, 8, 1), ProgressPercent = 50, MandayReported = 5m, CreatedAt = DateTime.UtcNow });
            db.SaveChanges();
        }

        var service = new PersonnelStatsService(factory);
        var stats = await service.GetAllAsync();

        var q1 = Assert.Single(stats, s => s.Quarter == 1 && s.FiscalYearStartYear == 2026);
        var q2 = Assert.Single(stats, s => s.Quarter == 2 && s.FiscalYearStartYear == 2026);
        Assert.Equal(3m, q1.Manday);
        Assert.Equal(5m, q2.Manday);
        Assert.Equal(0m, q1.Revenue); // dự án chưa Hoàn thành -> chưa quy đổi doanh thu
    }

    [Fact]
    public async Task GetAllAsync_AttributesRevenueByMandayShare_OnlyForCompletedProjects()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        int userA, userB;

        using (var db = factory.CreateDbContext())
        {
            var a = new User { SamAccountName = "a", DisplayName = "A", CreatedAt = DateTime.UtcNow };
            var b = new User { SamAccountName = "b", DisplayName = "B", CreatedAt = DateTime.UtcNow };
            db.Users.AddRange(a, b);
            db.SaveChanges();
            userA = a.Id;
            userB = b.Id;

            // Hoàn thành ngày 20/7/2026 -> NTC 2026-2027, Q2 (T7-T9). Doanh thu 100tr.
            var project = new Project
            {
                Name = "P", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.ENT,
                Status = ProjectStatus.Completed, RevenueAmount = 100_000_000m,
                CompletedAt = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
                CreatedByUserId = userA, CreatedAt = DateTime.UtcNow,
            };
            db.Projects.Add(project);
            db.SaveChanges();

            var task = new TaskItem { ProjectId = project.Id, Level = TaskLevel.Level3, Name = "T", SourceRow = 1 };
            db.Tasks.Add(task);
            db.SaveChanges();

            // A báo 3 manday, B báo 1 manday -> tổng 4, A chiếm 75%, B chiếm 25%.
            db.WorkReports.Add(new WorkReport { TaskId = task.Id, ReportedByUserId = userA, ReportDate = new DateOnly(2026, 6, 1), ProgressPercent = 50, MandayReported = 3m, CreatedAt = DateTime.UtcNow });
            db.WorkReports.Add(new WorkReport { TaskId = task.Id, ReportedByUserId = userB, ReportDate = new DateOnly(2026, 6, 15), ProgressPercent = 100, MandayReported = 1m, CreatedAt = DateTime.UtcNow });
            db.SaveChanges();
        }

        var service = new PersonnelStatsService(factory);
        var stats = await service.GetAllAsync();

        // Cả 2 báo cáo đều rơi vào Q1 NTC 2026-2027 (tháng 6) về mặt manday...
        var aQ1 = Assert.Single(stats, s => s.UserId == userA && s.Quarter == 1);
        var bQ1 = Assert.Single(stats, s => s.UserId == userB && s.Quarter == 1);
        Assert.Equal(3m, aQ1.Manday);
        Assert.Equal(1m, bQ1.Manday);

        // ...nhưng doanh thu quy đổi lại dồn vào Q2 (quý dự án Hoàn thành), theo đúng tỷ trọng manday.
        var aQ2 = Assert.Single(stats, s => s.UserId == userA && s.Quarter == 2);
        var bQ2 = Assert.Single(stats, s => s.UserId == userB && s.Quarter == 2);
        Assert.Equal(75_000_000m, aQ2.Revenue);
        Assert.Equal(25_000_000m, bQ2.Revenue);
        Assert.Equal(0m, aQ2.Manday); // dòng Q2 chỉ có doanh thu, không có manday báo cáo trong quý đó
    }

    [Fact]
    public async Task GetContributionsForUserAsync_BreaksDownByProjectAndTask_RevenueOnlyOnCompletedProject()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        int userA, userB;

        using (var db = factory.CreateDbContext())
        {
            var a = new User { SamAccountName = "a", DisplayName = "A", CreatedAt = DateTime.UtcNow };
            var b = new User { SamAccountName = "b", DisplayName = "B", CreatedAt = DateTime.UtcNow };
            db.Users.AddRange(a, b);
            db.SaveChanges();
            userA = a.Id;
            userB = b.Id;

            // Dự án 1: đã Hoàn thành, doanh thu 100tr. A báo 3 manday trên Task1 + 1 manday trên Task2 (tổng 4);
            // B báo 1 manday trên Task1 -> tổng cả dự án 5 manday, A chiếm 4/5 = 80%.
            var completedProject = new Project
            {
                Name = "Completed", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.ENT,
                Status = ProjectStatus.Completed, RevenueAmount = 100_000_000m,
                CompletedAt = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
                CreatedByUserId = userA, CreatedAt = DateTime.UtcNow,
            };
            db.Projects.Add(completedProject);
            db.SaveChanges();

            var task1 = new TaskItem { ProjectId = completedProject.Id, Level = TaskLevel.Level3, Name = "Task1", SourceRow = 1 };
            var task2 = new TaskItem { ProjectId = completedProject.Id, Level = TaskLevel.Level3, Name = "Task2", SourceRow = 2 };
            db.Tasks.AddRange(task1, task2);
            db.SaveChanges();

            db.WorkReports.Add(new WorkReport { TaskId = task1.Id, ReportedByUserId = userA, ReportDate = new DateOnly(2026, 6, 1), ProgressPercent = 50, MandayReported = 3m, CreatedAt = DateTime.UtcNow });
            db.WorkReports.Add(new WorkReport { TaskId = task2.Id, ReportedByUserId = userA, ReportDate = new DateOnly(2026, 6, 5), ProgressPercent = 50, MandayReported = 1m, CreatedAt = DateTime.UtcNow });
            db.WorkReports.Add(new WorkReport { TaskId = task1.Id, ReportedByUserId = userB, ReportDate = new DateOnly(2026, 6, 2), ProgressPercent = 20, MandayReported = 1m, CreatedAt = DateTime.UtcNow });

            // Dự án 2: chưa Hoàn thành — A cũng báo cáo ở đây nhưng không được tính doanh thu.
            var onTrackProject = new Project
            {
                Name = "OnTrack", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.GOV,
                RevenueAmount = 999_000_000m, CreatedByUserId = userA, CreatedAt = DateTime.UtcNow,
            };
            db.Projects.Add(onTrackProject);
            db.SaveChanges();

            var task3 = new TaskItem { ProjectId = onTrackProject.Id, Level = TaskLevel.Level3, Name = "Task3", SourceRow = 1 };
            db.Tasks.Add(task3);
            db.SaveChanges();
            db.WorkReports.Add(new WorkReport { TaskId = task3.Id, ReportedByUserId = userA, ReportDate = new DateOnly(2026, 6, 10), ProgressPercent = 30, MandayReported = 2m, CreatedAt = DateTime.UtcNow });

            db.SaveChanges();
        }

        var service = new PersonnelStatsService(factory);
        var contributions = await service.GetContributionsForUserAsync(userA);

        Assert.Equal(2, contributions.Count);

        var completed = contributions.Single(c => c.ProjectName == "Completed");
        Assert.Equal(4m, completed.Manday); // 3 + 1
        Assert.Equal(80_000_000m, completed.RevenueAttributed); // 100tr * 4/5
        Assert.Equal(2, completed.Tasks.Count);
        Assert.Equal(3m, completed.Tasks.Single(t => t.TaskName == "Task1").Manday);
        Assert.Equal(1m, completed.Tasks.Single(t => t.TaskName == "Task2").Manday);

        var onTrack = contributions.Single(c => c.ProjectName == "OnTrack");
        Assert.Equal(2m, onTrack.Manday);
        Assert.Equal(0m, onTrack.RevenueAttributed); // chưa Hoàn thành -> chưa quy đổi doanh thu
    }

    [Fact]
    public async Task GetForUserAsync_FiltersToOnlyThatUser()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        int userA, userB;

        using (var db = factory.CreateDbContext())
        {
            var a = new User { SamAccountName = "a", DisplayName = "A", CreatedAt = DateTime.UtcNow };
            var b = new User { SamAccountName = "b", DisplayName = "B", CreatedAt = DateTime.UtcNow };
            db.Users.AddRange(a, b);
            db.SaveChanges();
            userA = a.Id;
            userB = b.Id;

            var project = new Project { Name = "P", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.ENT, CreatedByUserId = userA, CreatedAt = DateTime.UtcNow };
            db.Projects.Add(project);
            db.SaveChanges();

            var task = new TaskItem { ProjectId = project.Id, Level = TaskLevel.Level3, Name = "T", SourceRow = 1 };
            db.Tasks.Add(task);
            db.SaveChanges();

            db.WorkReports.Add(new WorkReport { TaskId = task.Id, ReportedByUserId = userA, ReportDate = new DateOnly(2026, 5, 1), ProgressPercent = 20, MandayReported = 2m, CreatedAt = DateTime.UtcNow });
            db.WorkReports.Add(new WorkReport { TaskId = task.Id, ReportedByUserId = userB, ReportDate = new DateOnly(2026, 5, 1), ProgressPercent = 20, MandayReported = 7m, CreatedAt = DateTime.UtcNow });
            db.SaveChanges();
        }

        var service = new PersonnelStatsService(factory);
        var stats = await service.GetForUserAsync(userA);

        var row = Assert.Single(stats);
        Assert.Equal(userA, row.UserId);
        Assert.Equal(2m, row.Manday);
    }
}
