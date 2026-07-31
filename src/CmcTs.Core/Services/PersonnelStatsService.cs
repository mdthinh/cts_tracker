using CmcTs.Core.Data;
using CmcTs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CmcTs.Core.Services;

public class PersonnelStatsService : IPersonnelStatsService
{
    private readonly IDbContextFactory<CmcTsDbContext> _dbFactory;

    public PersonnelStatsService(IDbContextFactory<CmcTsDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<PersonnelQuarterStat>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var reports = await db.WorkReports
            .Include(r => r.ReportedByUser)
            .Include(r => r.Task)
            .ToListAsync(ct);

        var completedProjects = await db.Projects
            .Where(p => p.Status == ProjectStatus.Completed && p.CompletedAt != null && p.RevenueAmount != null)
            .Include(p => p.Tasks)
            .ToListAsync(ct);

        var mandayBuckets = new Dictionary<(int UserId, int FyStart, int Quarter), decimal>();
        var displayNames = new Dictionary<int, string>();

        foreach (var r in reports)
        {
            displayNames[r.ReportedByUserId] = r.ReportedByUser.DisplayName;

            var reportDateTime = r.ReportDate.ToDateTime(TimeOnly.MinValue);
            var key = (r.ReportedByUserId, FiscalYear.GetStartYear(reportDateTime), FiscalYear.GetQuarter(reportDateTime));
            mandayBuckets[key] = mandayBuckets.GetValueOrDefault(key) + r.MandayReported;
        }

        var revenueBuckets = new Dictionary<(int UserId, int FyStart, int Quarter), decimal>();

        foreach (var project in completedProjects)
        {
            var taskIds = project.Tasks.Select(t => t.Id).ToHashSet();
            var projectReports = reports.Where(r => taskIds.Contains(r.TaskId)).ToList();
            var totalManday = projectReports.Sum(r => r.MandayReported);
            if (totalManday <= 0)
            {
                continue;
            }

            var completedAt = project.CompletedAt!.Value;
            var fyStart = FiscalYear.GetStartYear(completedAt);
            var quarter = FiscalYear.GetQuarter(completedAt);

            foreach (var group in projectReports.GroupBy(r => r.ReportedByUserId))
            {
                var userManday = group.Sum(r => r.MandayReported);
                var share = project.RevenueAmount!.Value * (userManday / totalManday);
                var key = (group.Key, fyStart, quarter);
                revenueBuckets[key] = revenueBuckets.GetValueOrDefault(key) + share;
            }
        }

        var allKeys = mandayBuckets.Keys.Union(revenueBuckets.Keys);

        return allKeys
            .Select(k => new PersonnelQuarterStat(
                k.UserId,
                displayNames.GetValueOrDefault(k.UserId, "?"),
                k.FyStart,
                k.Quarter,
                mandayBuckets.GetValueOrDefault(k),
                revenueBuckets.GetValueOrDefault(k)))
            .OrderBy(s => s.DisplayName)
            .ThenBy(s => s.FiscalYearStartYear)
            .ThenBy(s => s.Quarter)
            .ToList();
    }

    public async Task<List<PersonnelQuarterStat>> GetForUserAsync(int userId, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.Where(s => s.UserId == userId).ToList();
    }

    public async Task<List<PersonnelProjectContribution>> GetContributionsForUserAsync(int userId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var userReports = await db.WorkReports
            .Where(r => r.ReportedByUserId == userId)
            .Include(r => r.Task).ThenInclude(t => t.Project)
            .ToListAsync(ct);

        var result = new List<PersonnelProjectContribution>();

        foreach (var projectGroup in userReports.GroupBy(r => r.Task.Project))
        {
            var project = projectGroup.Key;

            var tasks = projectGroup
                .GroupBy(r => r.Task)
                .Select(g => new PersonnelTaskContribution(g.Key.Id, g.Key.Name, g.Sum(r => r.MandayReported)))
                .OrderByDescending(t => t.Manday)
                .ToList();

            var userManday = tasks.Sum(t => t.Manday);
            var revenue = 0m;

            if (project.Status == ProjectStatus.Completed && project.RevenueAmount is decimal amount)
            {
                // Tổng manday của TOÀN dự án (mọi người báo cáo, không chỉ userId) để tính đúng tỷ
                // trọng — userReports ở trên chỉ chứa báo cáo của riêng người này nên phải query
                // riêng, không suy ra được từ dữ liệu đã tải.
                var totalProjectManday = await db.WorkReports
                    .Where(r => r.Task.ProjectId == project.Id)
                    .SumAsync(r => (decimal?)r.MandayReported, ct) ?? 0m;

                if (totalProjectManday > 0)
                {
                    revenue = amount * (userManday / totalProjectManday);
                }
            }

            result.Add(new PersonnelProjectContribution(
                project.Id, project.Name, project.CaseCode, project.Status,
                userManday, revenue, tasks));
        }

        return result
            .OrderByDescending(p => p.RevenueAttributed)
            .ThenByDescending(p => p.Manday)
            .ToList();
    }
}
