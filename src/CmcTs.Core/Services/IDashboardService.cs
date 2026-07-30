namespace CmcTs.Core.Services;

public interface IDashboardService
{
    // asOf truyền vào ngoài (thay vì tự lấy DateTime.Now bên trong) để tính được năm/quý tài chính
    // hiện tại một cách xác định và test được.
    Task<DashboardSummary> GetSummaryAsync(DateTime asOf, CancellationToken ct = default);
}
