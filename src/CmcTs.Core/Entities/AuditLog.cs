namespace CmcTs.Core.Entities;

// Ghi lại mọi thay đổi field trên các entity cho phép sửa (Project, TaskItem...) để truy vết ai sửa gì.
public class AuditLog
{
    public long Id { get; set; }

    public string EntityType { get; set; } = null!;
    public int EntityId { get; set; }
    public string FieldName { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public int ChangedByUserId { get; set; }
    public User ChangedByUser { get; set; } = null!;
    public DateTime ChangedAt { get; set; }
}
