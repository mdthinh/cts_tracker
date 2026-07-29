namespace CmcTs.Core.Services;

public interface IReferenceDataSeedService
{
    // Seed danh mục "Công nghệ liên quan" mặc định lúc khởi động, chỉ thêm những tên chưa có
    // (Admin có thể bổ sung/đổi tên thêm sau qua trang quản lý danh mục).
    Task EnsureSeededAsync(CancellationToken ct = default);
}
