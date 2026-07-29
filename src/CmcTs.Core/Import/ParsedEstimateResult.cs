namespace CmcTs.Core.Import;

public class ParsedCostSummaryLine
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    // true cho đúng 1 dòng — "Chi phí Manday" — chính là Revenue của dự án theo quy ước đã chốt.
    public bool IsRevenueSource { get; set; }
}

public class ParsedEstimateResult
{
    public List<ParsedCostSummaryLine> CostSummaries { get; set; } = new();
    public List<ParsedTaskNode> RootTasks { get; set; } = new();

    // Các trường hợp parser không chắc chắn (dòng không xác định được mục cha, leaf trọn gói...)
    // — hiển thị cho Admin xem lại ở màn Preview, không chặn việc parse.
    public List<string> Warnings { get; set; } = new();
}
