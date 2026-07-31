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
}
