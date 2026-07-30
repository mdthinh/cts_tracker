namespace CmcTs.Core.Services;

public interface ITaskProgressService
{
    // Ghi 1 báo cáo công việc cho task (Level 2 hoặc Level 3), cập nhật Progress/MandayActual của
    // chính task đó rồi tự tính lại rollup cho toàn bộ tổ tiên (Level 2/1 và không giới hạn số cấp)
    // — Progress rollup = bình quân trọng số theo MandayPlan của các con; MandayActual rollup = tổng.
    Task ReportWorkAsync(
        int taskId,
        int reportedByUserId,
        DateOnly reportDate,
        int progressPercent,
        decimal mandayReported,
        string? note,
        CancellationToken ct = default);
}
