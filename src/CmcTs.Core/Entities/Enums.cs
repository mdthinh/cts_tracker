namespace CmcTs.Core.Entities;

public enum GlobalRole
{
    Viewer = 0,
    Admin = 1,
}

public enum BusinessUnit
{
    BFSI = 0,
    GOV = 1,
    ENT = 2,
    SME = 3,
    ITS = 4,
}

// Giá trị số cố định để không cần migrate dữ liệu cũ (lưu int trong DB) — OnTrack giữ nguyên giá
// trị 1 vốn là "InProgress" trước đây (đổi tên, không đổi số); AtRisk/Delayed là 2 trạng thái mới
// thêm, đặt số sau Completed để không xáo trộn thứ tự cũ.
public enum ProjectStatus
{
    Draft = 0,
    OnTrack = 1,
    Completed = 2,
    AtRisk = 3,
    Delayed = 4,
}

public enum TaskLevel
{
    Level1 = 1,
    Level2 = 2,
    Level3 = 3,
}

// Level 3 (leaf) tasks are either effort-based (Số người x Số ngày, có đơn giá/ngày)
// or a fixed package price (cột "Số ngày" ghi "Gói" thay vì số, vd dịch vụ bảo hành trọn gói).
public enum TaskCostType
{
    Manday = 0,
    Package = 1,
}

public enum ImportParseStatus
{
    Pending = 0,
    Parsed = 1,
    Committed = 2,
    Failed = 3,
}
