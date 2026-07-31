namespace CmcTs.Core.Services;

public interface IPersonnelStatsService
{
    // Toàn bộ nhân sự có ít nhất 1 báo cáo công việc, mỗi dòng là 1 (user, năm tài chính, quý).
    Task<List<PersonnelQuarterStat>> GetAllAsync(CancellationToken ct = default);

    // Lọc lại từ GetAllAsync — chỉ đúng 1 người, dùng cho dashboard cá nhân của non-admin.
    Task<List<PersonnelQuarterStat>> GetForUserAsync(int userId, CancellationToken ct = default);
}
