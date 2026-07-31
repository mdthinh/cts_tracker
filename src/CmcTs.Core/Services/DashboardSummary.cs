using CmcTs.Core.Entities;

namespace CmcTs.Core.Services;

public record ProjectStatusCounts(int Draft, int OnTrack, int AtRisk, int Delayed, int Completed)
{
    // Tổng hợp 3 trạng thái "đang chạy" (khác Draft/Completed) — dùng cho ô KPI "Dự án đang triển
    // khai" trên dashboard, không cần tách riêng theo từng trạng thái con ở đó.
    public int InProgress => OnTrack + AtRisk + Delayed;
}

public record QuarterRevenue(int Quarter, decimal Amount);

public record DashboardProjectRow(
    int ProjectId,
    string Name,
    string? CaseCode,
    string FiscalYear,
    BusinessUnit BusinessUnit,
    string? ProjectLeadName,
    ProjectStatus Status,
    int Progress,
    decimal MandayActual,
    decimal MandayPlan);

public class DashboardSummary
{
    public string FiscalYearLabel { get; set; } = string.Empty;
    public int CurrentQuarter { get; set; }

    // Tổng manday đã báo cáo (WorkReport.MandayReported) trong năm tài chính hiện tại, mọi dự án.
    public decimal MandayThisFiscalYear { get; set; }

    // Doanh thu = tổng Project.RevenueAmount của các dự án Completed có CompletedAt rơi vào quý/năm
    // tài chính tương ứng — chỉ tính khi đã bấm "Hoàn thành", không ước tính từ dự án đang chạy.
    public decimal RevenueCurrentQuarter { get; set; }
    public decimal RevenueThisFiscalYear { get; set; }
    public List<QuarterRevenue> QuarterlyRevenue { get; set; } = new();

    public ProjectStatusCounts StatusCounts { get; set; } = new(0, 0, 0, 0, 0);
    public List<DashboardProjectRow> Projects { get; set; } = new();
}
