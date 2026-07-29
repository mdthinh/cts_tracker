namespace CmcTs.Core.Entities;

// 1 lần upload file "Dự toán" (.xls/.xlsx) cho 1 dự án. Có thể có nhiều bản (v1.0, v2.0...)
// nhưng chỉ 1 bản IsActive=true tại một thời điểm — bản đó quyết định Task tree + Revenue hiện tại.
public class EstimateImport
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;

    public int UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;
    public DateTime UploadedAt { get; set; }

    public ImportParseStatus Status { get; set; } = ImportParseStatus.Pending;
    public bool IsActive { get; set; }
    public string? ParseErrorMessage { get; set; }

    public ICollection<EstimateCostSummary> CostSummaries { get; set; } = new List<EstimateCostSummary>();
}
