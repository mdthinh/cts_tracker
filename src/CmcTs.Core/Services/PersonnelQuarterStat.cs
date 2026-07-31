namespace CmcTs.Core.Services;

// Manday: tổng WorkReport.MandayReported của user đó, dồn vào quý chứa ReportDate — độc lập với
// việc dự án đã Hoàn thành hay chưa (phản ánh đúng khối lượng công việc đã làm mỗi quý).
// Revenue: chỉ tính từ các dự án đã Hoàn thành, quy đổi theo tỷ trọng manday user đó đã báo cáo
// trên đúng dự án đó (so với tổng manday mọi người báo cáo trên dự án) nhân với Project.RevenueAmount,
// dồn vào quý chứa Project.CompletedAt — là số ước tính, không phải số chốt riêng từng người.
public record PersonnelQuarterStat(int UserId, string DisplayName, int FiscalYearStartYear, int Quarter, decimal Manday, decimal Revenue)
{
    public string FiscalYearLabel => $"{FiscalYearStartYear}-{FiscalYearStartYear + 1}";
}
