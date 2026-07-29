namespace CmcTs.Core.Import;

// Kết quả parse tạm thời từ Excel, chưa lưu DB — dùng để hiển thị màn Preview cho Admin sửa
// tay trước khi xác nhận (parser dò theo cấu trúc cột A, không đảm bảo đúng 100% với mọi file).
public class ParsedTaskNode
{
    public int Level { get; set; } // 1, 2, 3 (leaf)
    public string? Code { get; set; } // "1", "1.1"... null với leaf
    public string Name { get; set; } = string.Empty;

    public decimal? HeadCount { get; set; } // Số người (leaf)
    public decimal? Days { get; set; } // Số ngày (leaf, null nếu IsPackage)
    public string? StaffGroup { get; set; } // Nhóm nhân viên (leaf)
    public decimal? UnitPrice { get; set; } // Đơn giá (leaf)

    public decimal MandayPlan { get; set; } // leaf: HeadCount * Days; level 1/2: SUM(children) tính khi build tree
    public decimal CostPlan { get; set; } // leaf: cột "Dự toán"; level 1/2: SUM(children)
    public bool IsPackage { get; set; } // true nếu cột "Số ngày" là chữ (vd "Gói") thay vì số

    public int SourceRow { get; set; } // số dòng trong sheet MAN DAY gốc, phục vụ audit/trace
    public List<ParsedTaskNode> Children { get; set; } = new();
}
