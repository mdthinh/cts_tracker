using CmcTs.Core.Entities;
using CmcTs.Core.Services;
using Xunit;

namespace CmcTs.Core.Tests;

public class DashboardServiceTests
{
    // asOf cố định 29/7/2026 -> NTC 2026-2027 (1/4/2026-31/3/2027), quý hiện tại = Q2 (T7-T9).
    private static readonly DateTime AsOf = new(2026, 7, 29, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetSummaryAsync_BucketsCompletedProjectRevenueByFiscalQuarter()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());

        using (var db = factory.CreateDbContext())
        {
            var user = new User { SamAccountName = "u", DisplayName = "U", CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
            db.SaveChanges();

            // Q1 NTC hiện tại (T4-T6/2026): hoàn thành 15/5/2026, doanh thu 100tr.
            db.Projects.Add(new Project
            {
                Name = "P-Q1", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.ENT,
                Status = ProjectStatus.Completed, RevenueAmount = 100_000_000m,
                CompletedAt = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                CreatedByUserId = user.Id, CreatedAt = DateTime.UtcNow,
            });

            // Q2 NTC hiện tại (T7-T9/2026, chứa asOf): hoàn thành 20/7/2026, doanh thu 200tr.
            db.Projects.Add(new Project
            {
                Name = "P-Q2", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.GOV,
                Status = ProjectStatus.Completed, RevenueAmount = 200_000_000m,
                CompletedAt = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc),
                CreatedByUserId = user.Id, CreatedAt = DateTime.UtcNow,
            });

            // Hoàn thành nhưng thuộc NTC KHÁC (T2/2026, tức NTC 2025-2026) -> không được tính vào.
            db.Projects.Add(new Project
            {
                Name = "P-OldFY", FiscalYear = "2025-2026", BusinessUnit = BusinessUnit.SME,
                Status = ProjectStatus.Completed, RevenueAmount = 999_000_000m,
                CompletedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc),
                CreatedByUserId = user.Id, CreatedAt = DateTime.UtcNow,
            });

            // Đang triển khai, chưa hoàn thành -> không tính vào doanh thu dù có RevenueAmount.
            db.Projects.Add(new Project
            {
                Name = "P-InProgress", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.ITS,
                Status = ProjectStatus.InProgress, RevenueAmount = 500_000_000m,
                CreatedByUserId = user.Id, CreatedAt = DateTime.UtcNow,
            });

            db.SaveChanges();
        }

        var service = new DashboardService(factory);
        var summary = await service.GetSummaryAsync(AsOf);

        Assert.Equal(2, summary.CurrentQuarter);
        Assert.Equal("2026-2027", summary.FiscalYearLabel);

        Assert.Equal(100_000_000m, summary.QuarterlyRevenue.Single(q => q.Quarter == 1).Amount);
        Assert.Equal(200_000_000m, summary.QuarterlyRevenue.Single(q => q.Quarter == 2).Amount);
        Assert.Equal(0m, summary.QuarterlyRevenue.Single(q => q.Quarter == 3).Amount);
        Assert.Equal(0m, summary.QuarterlyRevenue.Single(q => q.Quarter == 4).Amount);

        Assert.Equal(300_000_000m, summary.RevenueThisFiscalYear); // 100tr + 200tr, không tính P-OldFY hay P-InProgress
        Assert.Equal(200_000_000m, summary.RevenueCurrentQuarter); // quý hiện tại = Q2

        Assert.Equal(4, summary.Projects.Count);
        Assert.Equal(new ProjectStatusCounts(0, 1, 3), summary.StatusCounts);
    }

    [Fact]
    public async Task GetSummaryAsync_SumsMandayReportedWithinCurrentFiscalYearOnly()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());

        using (var db = factory.CreateDbContext())
        {
            var user = new User { SamAccountName = "u", DisplayName = "U", CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
            db.SaveChanges();

            var project = new Project { Name = "P", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.ENT, CreatedByUserId = user.Id, CreatedAt = DateTime.UtcNow };
            db.Projects.Add(project);
            db.SaveChanges();

            var task = new TaskItem { ProjectId = project.Id, Level = TaskLevel.Level3, Name = "T", SourceRow = 1 };
            db.Tasks.Add(task);
            db.SaveChanges();

            // Trong NTC hiện tại (1/4/2026-31/3/2027) -> tính.
            db.WorkReports.Add(new WorkReport { TaskId = task.Id, ReportedByUserId = user.Id, ReportDate = new DateOnly(2026, 6, 1), ProgressPercent = 50, MandayReported = 3m, CreatedAt = DateTime.UtcNow });
            // Trước NTC hiện tại (NTC 2025-2026) -> không tính.
            db.WorkReports.Add(new WorkReport { TaskId = task.Id, ReportedByUserId = user.Id, ReportDate = new DateOnly(2026, 3, 1), ProgressPercent = 20, MandayReported = 10m, CreatedAt = DateTime.UtcNow });
            db.SaveChanges();
        }

        var service = new DashboardService(factory);
        var summary = await service.GetSummaryAsync(AsOf);

        Assert.Equal(3m, summary.MandayThisFiscalYear);
    }
}
