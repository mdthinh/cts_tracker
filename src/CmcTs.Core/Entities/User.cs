namespace CmcTs.Core.Entities;

public class User
{
    public int Id { get; set; }

    // Tài khoản AD (sAMAccountName), dùng làm khóa nghiệp vụ khi đăng nhập / gán người phụ trách.
    // Với tài khoản local (IsLocalAccount=true) thì đây là username tự đặt, không tra được trên AD.
    public string SamAccountName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Department { get; set; }

    public GlobalRole GlobalRole { get; set; } = GlobalRole.Viewer;
    public bool IsActive { get; set; } = true;

    // Tài khoản local (không qua AD) — dùng cho admin "break-glass" khi AD chưa sẵn sàng/gặp sự cố.
    // Được seed 1 lần khi khởi động từ cấu hình LocalAdmin, không quản lý qua AD search.
    public bool IsLocalAccount { get; set; }
    public string? PasswordHash { get; set; }

    // Lần gần nhất thông tin (tên hiển thị, email, phòng ban) được đồng bộ lại từ AD.
    public DateTime? LastSyncedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
}
