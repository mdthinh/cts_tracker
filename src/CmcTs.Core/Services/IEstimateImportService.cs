using CmcTs.Core.Entities;
using CmcTs.Core.Import;

namespace CmcTs.Core.Services;

public interface IEstimateImportService
{
    // Ghi kết quả đã parse (và có thể đã được Admin sửa tay ở màn Preview) vào DB: tạo
    // EstimateImport + EstimateCostSummary + toàn bộ cây Task, đồng thời chốt Project.RevenueAmount
    // từ dòng "Chi phí Manday". Chỉ cho phép 1 lần import cho mỗi dự án ở giai đoạn này — nếu dự án
    // đã có Task, ném InvalidOperationException (import lại/versioning sẽ bổ sung sau).
    Task<EstimateImport> CommitAsync(
        int projectId,
        string fileName,
        string filePath,
        int uploadedByUserId,
        ParsedEstimateResult parsed,
        CancellationToken ct = default);
}
