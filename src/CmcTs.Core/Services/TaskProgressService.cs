using CmcTs.Core.Data;
using CmcTs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CmcTs.Core.Services;

public class TaskProgressService : ITaskProgressService
{
    private readonly IDbContextFactory<CmcTsDbContext> _dbFactory;

    public TaskProgressService(IDbContextFactory<CmcTsDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task ReportWorkAsync(
        int taskId,
        int reportedByUserId,
        DateOnly reportDate,
        int progressPercent,
        decimal mandayReported,
        string? note,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var task = await db.Tasks.SingleAsync(t => t.Id == taskId, ct);

        // Load hết task của dự án vào cùng context để EF tự fixup ParentTask/Children — cần để
        // đi ngược lên tổ tiên tính lại rollup mà không phải query nhiều lượt.
        await db.Tasks.Where(t => t.ProjectId == task.ProjectId).LoadAsync(ct);

        db.WorkReports.Add(new WorkReport
        {
            TaskId = taskId,
            ReportedByUserId = reportedByUserId,
            ReportDate = reportDate,
            ProgressPercent = Math.Clamp(progressPercent, 0, 100),
            MandayReported = mandayReported,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt = DateTime.UtcNow,
        });

        task.Progress = Math.Clamp(progressPercent, 0, 100);
        task.MandayActual += mandayReported;

        var ancestor = task.ParentTask;
        while (ancestor is not null)
        {
            RecalculateFromChildren(ancestor);
            ancestor = ancestor.ParentTask;
        }

        await db.SaveChangesAsync(ct);
    }

    private static void RecalculateFromChildren(TaskItem node)
    {
        if (node.Children.Count == 0)
        {
            return;
        }

        node.MandayActual = node.Children.Sum(c => c.MandayActual);

        var totalPlan = node.Children.Sum(c => c.MandayPlan);
        node.Progress = totalPlan > 0
            ? (int)Math.Round(node.Children.Sum(c => c.Progress * c.MandayPlan) / totalPlan)
            : (int)Math.Round(node.Children.Average(c => (decimal)c.Progress));
    }
}
