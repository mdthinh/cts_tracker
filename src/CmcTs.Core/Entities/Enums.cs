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

public enum ProjectStatus
{
    Draft = 0,
    InProgress = 1,
    Completed = 2,
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
