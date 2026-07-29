namespace CmcTs.Core.Entities;

// Đặt tên TaskItem (không phải "Task") để tránh trùng với System.Threading.Tasks.Task.
// Cây 3 cấp lấy từ sheet MAN DAY: Level1 (nền vàng) -> Level2 (nền cam/chữ đỏ) -> Level3 (leaf, không tô nền).
// Một số nhóm Level1 không có Level2 con mà đi thẳng xuống leaf (vd "Dịch vụ bảo hành sau triển khai") —
// ParentTaskId cho phép leaf trỏ thẳng lên Level1 trong trường hợp đó.
public class TaskItem
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int? ParentTaskId { get; set; }
    public TaskItem? ParentTask { get; set; }
    public ICollection<TaskItem> Children { get; set; } = new List<TaskItem>();

    public TaskLevel Level { get; set; }

    // "1", "1.1"... theo cột STT trong Excel. Leaf không có Code.
    public string? Code { get; set; }
    public string Name { get; set; } = null!;

    public TaskCostType CostType { get; set; } = TaskCostType.Manday;

    // Chỉ có giá trị ở leaf, giữ lại để trace/hiển thị lại đúng như Excel gốc.
    public decimal? HeadCount { get; set; } // Số người
    public decimal? Days { get; set; } // Số ngày (null nếu CostType = Package)
    public string? StaffGroup { get; set; } // Nhóm nhân viên
    public decimal? UnitPrice { get; set; } // Đơn giá

    // Leaf: MandayPlan = HeadCount * Days (Package: null/0). Level1/Level2: SUM(children.MandayPlan).
    // Luôn tự tính trong ứng dụng, không tin số có sẵn trong cột "Ghi chú" của Excel (không đáng tin cậy).
    public decimal MandayPlan { get; set; }
    public decimal MandayActual { get; set; } // roll-up từ WorkReport
    public decimal CostPlan { get; set; } // leaf: cột "Dự toán"; Level1/2: SUM(children.CostPlan)

    public int? AssigneeUserId { get; set; }
    public User? AssigneeUser { get; set; }

    // 0-100. Leaf: nhập trực tiếp qua WorkReport. Level1/2: bình quân trọng số theo MandayPlan của con.
    public int Progress { get; set; }
    public string? Note { get; set; }

    // Số dòng trong sheet MAN DAY gốc, phục vụ audit/trace khi cần đối chiếu lại Excel.
    public int SourceRow { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<WorkReport> WorkReports { get; set; } = new List<WorkReport>();
}
