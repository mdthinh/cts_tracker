using CmcTs.Core.Entities;

namespace CmcTs.Core.Services;

// Chuyển trạng thái giữa các trạng thái CHƯA hoàn thành (Draft/OnTrack/AtRisk/Delayed) — có thể
// đổi qua lại nhiều lần, không khóa gì thêm. Chuyển sang/ra khỏi Completed đi qua
// IProjectCompletionService (Completed gắn với việc chốt doanh thu, cần luồng riêng có xác nhận).
public interface IProjectStatusService
{
    Task ChangeStatusAsync(int projectId, ProjectStatus newStatus, int changedByUserId, CancellationToken ct = default);
}
