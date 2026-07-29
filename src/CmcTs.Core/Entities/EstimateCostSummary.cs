namespace CmcTs.Core.Entities;

// 1 dòng tổng chi phí lấy từ sheet "Du toan": CP ăn ở/đi lại/bốc hàng, Chi phí Manday,
// Vật tư phụ nhân công, Vận chuyển. Chỉ dòng "Chi phí Manday" có IsRevenueSource=true.
public class EstimateCostSummary
{
    public int Id { get; set; }

    public int EstimateImportId { get; set; }
    public EstimateImport EstimateImport { get; set; } = null!;

    public string Category { get; set; } = null!;
    public decimal Amount { get; set; }
    public bool IsRevenueSource { get; set; }
}
