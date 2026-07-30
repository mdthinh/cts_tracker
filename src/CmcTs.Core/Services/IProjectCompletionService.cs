namespace CmcTs.Core.Services;

public interface IProjectCompletionService
{
    // Đánh dấu dự án Hoàn thành: chốt CompletedAt/CompletedByUserId, ghi AuditLog. Doanh thu
    // (Project.RevenueAmount, đã chốt lúc import Dự toán) từ thời điểm này được tính vào quý tài
    // chính chứa CompletedAt — quý cụ thể suy ra bằng FiscalYear.GetQuarter(CompletedAt) khi cần
    // hiển thị (Dashboard), không lưu thêm cột riêng. Ném InvalidOperationException nếu dự án đã
    // Hoàn thành từ trước.
    Task CompleteProjectAsync(int projectId, int completedByUserId, CancellationToken ct = default);
}
