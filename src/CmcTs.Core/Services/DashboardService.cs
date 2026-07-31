using CmcTs.Core.Data;
using CmcTs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CmcTs.Core.Services;

public class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<CmcTsDbContext> _dbFactory;
    private readonly IPersonnelStatsService _personnelStatsService;

    public DashboardService(IDbContextFactory<CmcTsDbContext> dbFactory, IPersonnelStatsService personnelStatsService)
    {
        _dbFactory = dbFactory;
        _personnelStatsService = personnelStatsService;
    }

    public async Task<DashboardSummary> GetSummaryAsync(DateTime asOf, int? scopeToUserId = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var fyStartYear = FiscalYear.GetStartYear(asOf);
        var fyStart = new DateTime(fyStartYear, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var fyEnd = fyStart.AddYears(1);

        var summary = new DashboardSummary
        {
            FiscalYearLabel = FiscalYear.GetLabel(asOf),
            CurrentQuarter = FiscalYear.GetQuarter(asOf),
        };

        var fyStartDateOnly = DateOnly.FromDateTime(fyStart);
        var fyEndDateOnly = DateOnly.FromDateTime(fyEnd);

        var quarterly = new decimal[4];

        if (scopeToUserId is int userId)
        {
            // Nhân sự thường: manday chỉ tính báo cáo của chính người đó; doanh thu lấy từ số ước
            // tính theo tỷ trọng manday đã tính sẵn ở PersonnelStatsService (dùng chung logic quy
            // đổi với trang Thống kê nhân sự, không tính lại 2 nơi 2 kiểu).
            summary.MandayThisFiscalYear = await db.WorkReports
                .Where(r => r.ReportedByUserId == userId && r.ReportDate >= fyStartDateOnly && r.ReportDate < fyEndDateOnly)
                .SumAsync(r => (decimal?)r.MandayReported, ct) ?? 0m;

            var personalStats = await _personnelStatsService.GetForUserAsync(userId, ct);
            foreach (var s in personalStats.Where(s => s.FiscalYearStartYear == fyStartYear))
            {
                quarterly[s.Quarter - 1] += s.Revenue;
            }
        }
        else
        {
            summary.MandayThisFiscalYear = await db.WorkReports
                .Where(r => r.ReportDate >= fyStartDateOnly && r.ReportDate < fyEndDateOnly)
                .SumAsync(r => (decimal?)r.MandayReported, ct) ?? 0m;

            var completedProjects = await db.Projects
                .Where(p => p.Status == ProjectStatus.Completed && p.CompletedAt != null)
                .Select(p => new { p.CompletedAt, p.RevenueAmount })
                .ToListAsync(ct);

            foreach (var p in completedProjects)
            {
                var completedAt = p.CompletedAt!.Value;
                if (completedAt < fyStart || completedAt >= fyEnd)
                {
                    continue; // hoàn thành ở năm tài chính khác, không tính vào NTC hiện tại
                }

                var quarterIndex = FiscalYear.GetQuarter(completedAt) - 1;
                quarterly[quarterIndex] += p.RevenueAmount ?? 0m;
            }
        }

        summary.QuarterlyRevenue = Enumerable.Range(1, 4)
            .Select(q => new QuarterRevenue(q, quarterly[q - 1]))
            .ToList();
        summary.RevenueThisFiscalYear = quarterly.Sum();
        summary.RevenueCurrentQuarter = quarterly[summary.CurrentQuarter - 1];

        var projectsQuery = db.Projects
            .Include(p => p.ProjectLead)
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .AsQueryable();

        // Trưởng dự án luôn nằm trong Members nên không cần điều kiện riêng — cùng quy tắc phạm
        // vi xem dự án dùng ở trang Dự án và ITaskPermissionService.CanViewProjectAsync.
        if (scopeToUserId is int scopedUserId)
        {
            projectsQuery = projectsQuery.Where(p => p.Members.Any(m => m.UserId == scopedUserId));
        }

        var allProjects = await projectsQuery.ToListAsync(ct);

        summary.StatusCounts = new ProjectStatusCounts(
            allProjects.Count(p => p.Status == ProjectStatus.Draft),
            allProjects.Count(p => p.Status == ProjectStatus.OnTrack),
            allProjects.Count(p => p.Status == ProjectStatus.AtRisk),
            allProjects.Count(p => p.Status == ProjectStatus.Delayed),
            allProjects.Count(p => p.Status == ProjectStatus.Completed));

        summary.Projects = allProjects
            .Select(BuildProjectRow)
            .OrderByDescending(r => r.ProjectId)
            .ToList();

        return summary;
    }

    // Tiến độ/manday của cả dự án = tổng hợp từ các task Level 1 (đã tự roll-up từ Level 2/3 bởi
    // TaskProgressService mỗi lần có báo cáo công việc) — không tính lại từ leaf để tránh trùng lặp
    // logic rollup.
    private static DashboardProjectRow BuildProjectRow(Project p)
    {
        var level1Tasks = p.Tasks.Where(t => t.Level == TaskLevel.Level1).ToList();
        var totalPlan = level1Tasks.Sum(t => t.MandayPlan);
        var totalActual = level1Tasks.Sum(t => t.MandayActual);

        var progress = totalPlan > 0
            ? (int)Math.Round(level1Tasks.Sum(t => t.Progress * t.MandayPlan) / totalPlan)
            : level1Tasks.Count > 0
                ? (int)Math.Round(level1Tasks.Average(t => (decimal)t.Progress))
                : 0;

        return new DashboardProjectRow(
            p.Id, p.Name, p.CaseCode, p.FiscalYear, p.BusinessUnit,
            p.ProjectLead?.DisplayName, p.Status, progress, totalActual, totalPlan);
    }
}
