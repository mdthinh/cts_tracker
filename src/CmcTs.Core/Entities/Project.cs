namespace CmcTs.Core.Entities;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    // Định dạng "2026-2027" — năm tài chính 1/4 -> 31/3 năm sau.
    public string FiscalYear { get; set; } = null!;
    public BusinessUnit BusinessUnit { get; set; }

    public int? ProjectLeadUserId { get; set; }
    public User? ProjectLead { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;
    public DateTime? CompletedAt { get; set; }
    public int? CompletedByUserId { get; set; }
    public User? CompletedByUser { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    // Chốt từ EstimateCostSummary (dòng "Chi phí Manday") của bản Dự toán đang active,
    // tại thời điểm dự án được đánh dấu Hoàn thành mới được cộng vào doanh thu quý.
    public decimal? RevenueAmount { get; set; }

    public ICollection<ProjectTechnology> ProjectTechnologies { get; set; } = new List<ProjectTechnology>();
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<EstimateImport> EstimateImports { get; set; } = new List<EstimateImport>();
    public ICollection<ProjectDocument> Documents { get; set; } = new List<ProjectDocument>();
}
