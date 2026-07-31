using CmcTs.Core.Entities;

namespace CmcTs.Core.Services;

public record PersonnelTaskContribution(int TaskId, string TaskName, decimal Manday);

// Truy vết: 1 người đóng góp bao nhiêu manday/doanh thu ở đúng dự án nào, task nào trong dự án đó —
// để không phải "tin suông" con số tổng hợp ở PersonnelQuarterStat. Manday theo task là số thật
// (tổng WorkReport.MandayReported); RevenueAttributed là số ước tính ở cấp DỰ ÁN (cùng công thức
// quy đổi theo tỷ trọng manday dùng ở GetAllAsync) — cố ý KHÔNG chia tiếp xuống từng task để tránh
// chồng thêm 1 lớp ước tính nữa lên trên 1 lớp ước tính đã có.
public record PersonnelProjectContribution(
    int ProjectId,
    string ProjectName,
    string? ProjectCaseCode,
    ProjectStatus ProjectStatus,
    decimal Manday,
    decimal RevenueAttributed,
    List<PersonnelTaskContribution> Tasks);
