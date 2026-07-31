namespace CmcTs.Core.Services;

public interface IDashboardService
{
    // asOf truyền vào ngoài (thay vì tự lấy DateTime.Now bên trong) để tính được năm/quý tài chính
    // hiện tại một cách xác định và test được.
    // scopeToUserId: null -> toàn công ty (Admin). Có giá trị -> chỉ manday/doanh thu/dự án của
    // đúng người đó (nhân sự thường chỉ thấy phần của mình, không thấy toàn công ty).
    Task<DashboardSummary> GetSummaryAsync(DateTime asOf, int? scopeToUserId = null, CancellationToken ct = default);
}
