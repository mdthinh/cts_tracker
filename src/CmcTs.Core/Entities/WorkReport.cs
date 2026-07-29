namespace CmcTs.Core.Entities;

// 1 lần 1 thành viên báo cáo khối lượng hoàn thành cho 1 task (Level 2 hoặc Level 3).
public class WorkReport
{
    public int Id { get; set; }

    public int TaskId { get; set; }
    public TaskItem Task { get; set; } = null!;

    public int ReportedByUserId { get; set; }
    public User ReportedByUser { get; set; } = null!;

    public DateOnly ReportDate { get; set; }
    public int ProgressPercent { get; set; }
    public decimal MandayReported { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
}
