namespace CmcTs.Core.Entities;

// Tài liệu final (nghiệm thu, hoàn công...) đính kèm theo dự án.
public class ProjectDocument
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;

    public int UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;
    public DateTime UploadedAt { get; set; }
}
