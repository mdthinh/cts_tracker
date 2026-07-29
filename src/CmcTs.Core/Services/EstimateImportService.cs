using CmcTs.Core.Data;
using CmcTs.Core.Entities;
using CmcTs.Core.Import;
using Microsoft.EntityFrameworkCore;

namespace CmcTs.Core.Services;

public class EstimateImportService : IEstimateImportService
{
    private readonly IDbContextFactory<CmcTsDbContext> _dbFactory;

    public EstimateImportService(IDbContextFactory<CmcTsDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<EstimateImport> CommitAsync(
        int projectId,
        string fileName,
        string filePath,
        int uploadedByUserId,
        ParsedEstimateResult parsed,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var hasExistingTasks = await db.Tasks.AnyAsync(t => t.ProjectId == projectId, ct);
        if (hasExistingTasks)
        {
            throw new InvalidOperationException(
                "Dự án đã có cây công việc từ 1 lần import trước — chưa hỗ trợ import lại đè lên (sẽ bổ sung sau).");
        }

        var import = new EstimateImport
        {
            ProjectId = projectId,
            FileName = fileName,
            FilePath = filePath,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = DateTime.UtcNow,
            Status = ImportParseStatus.Committed,
            IsActive = true,
        };

        foreach (var line in parsed.CostSummaries)
        {
            import.CostSummaries.Add(new EstimateCostSummary
            {
                Category = line.Category,
                Amount = line.Amount,
                IsRevenueSource = line.IsRevenueSource,
            });
        }

        db.EstimateImports.Add(import);

        foreach (var root in parsed.RootTasks)
        {
            AddTaskRecursive(db, projectId, root, parentTask: null);
        }

        var project = await db.Projects.SingleAsync(p => p.Id == projectId, ct);
        project.RevenueAmount = parsed.CostSummaries.Where(c => c.IsRevenueSource).Sum(c => c.Amount);

        await db.SaveChangesAsync(ct);
        return import;
    }

    private static void AddTaskRecursive(CmcTsDbContext db, int projectId, ParsedTaskNode node, TaskItem? parentTask)
    {
        var entity = new TaskItem
        {
            ProjectId = projectId,
            ParentTask = parentTask,
            Level = node.Level switch
            {
                1 => TaskLevel.Level1,
                2 => TaskLevel.Level2,
                _ => TaskLevel.Level3,
            },
            Code = node.Code,
            Name = node.Name,
            CostType = node.IsPackage ? TaskCostType.Package : TaskCostType.Manday,
            HeadCount = node.HeadCount,
            Days = node.Days,
            StaffGroup = node.StaffGroup,
            UnitPrice = node.UnitPrice,
            MandayPlan = node.MandayPlan,
            MandayActual = 0,
            CostPlan = node.CostPlan,
            Progress = 0,
            SourceRow = node.SourceRow,
            CreatedAt = DateTime.UtcNow,
        };
        db.Tasks.Add(entity);

        foreach (var child in node.Children)
        {
            AddTaskRecursive(db, projectId, child, entity);
        }
    }
}
