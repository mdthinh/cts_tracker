using CmcTs.Core.Import;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Xunit;

namespace CmcTs.Core.Tests;

// Dựng workbook mẫu trong bộ nhớ (không dùng file khách hàng thật) mô phỏng đúng cấu trúc đã quan
// sát được từ file mẫu thật: sheet "Du toan" có 4 dòng DỰ TOÁN màu khác nhau, sheet "MAN DAY" có
// Level1 (STT nguyên) -> Level2 (STT dạng x.y) -> leaf, và 1 trường hợp Level1 không có Level2 con.
public class EstimateImportParserTests
{
    [Fact]
    public void Parse_ReadsCostSummaryFromDuToanSheet()
    {
        using var stream = BuildSampleWorkbook();
        var result = new EstimateImportParser().Parse(stream);

        Assert.Equal(4, result.CostSummaries.Count);

        var manday = Assert.Single(result.CostSummaries, c => c.IsRevenueSource);
        Assert.Equal("Chi phí Manday - ngày công", manday.Category);
        Assert.Equal(786_480_000m, manday.Amount);

        Assert.Equal(11_000_000m, result.CostSummaries.Single(c => c.Category.Contains("ăn ở")).Amount);
        Assert.Equal(0m, result.CostSummaries.Single(c => c.Category.Contains("vật tư")).Amount);
        Assert.Equal(0m, result.CostSummaries.Single(c => c.Category.Contains("vận chuyển")).Amount);
    }

    [Fact]
    public void Parse_BuildsThreeLevelTreeWithFormulaEvaluationAndRollup()
    {
        using var stream = BuildSampleWorkbook();
        var result = new EstimateImportParser().Parse(stream);

        Assert.Equal(2, result.RootTasks.Count);

        var chuanBi = result.RootTasks.Single(t => t.Name == "CHUẨN BỊ");
        Assert.Equal(1, chuanBi.Level);
        Assert.Equal("1", chuanBi.Code);
        Assert.Equal(2, chuanBi.Children.Count);

        var quanTri = chuanBi.Children.Single(t => t.Name == "Quản trị");
        Assert.Equal(2, quanTri.Level);
        Assert.Equal("1.1", quanTri.Code);
        var quanTriDuAn = Assert.Single(quanTri.Children);
        Assert.Equal(3, quanTriDuAn.Level);
        Assert.Equal(1m, quanTriDuAn.HeadCount);
        Assert.Equal(2m, quanTriDuAn.Days);
        Assert.Equal(2m, quanTriDuAn.MandayPlan); // 1 người x 2 ngày
        Assert.Equal(2_000_000m, quanTriDuAn.CostPlan); // đọc từ công thức C*D*F, không phải giá trị tĩnh
        Assert.False(quanTriDuAn.IsPackage);

        var chuanBiCon = chuanBi.Children.Single(t => t.Name == "Chuẩn bị");
        var kiemTra = Assert.Single(chuanBiCon.Children);
        Assert.Equal(6m, kiemTra.MandayPlan); // 2 người x 3 ngày
        Assert.Equal(12_000_000m, kiemTra.CostPlan);

        // Rollup Level1/Level2 phải tự tính bằng tổng các con, không đọc số có sẵn trong sheet.
        Assert.Equal(8m, chuanBi.MandayPlan);
        Assert.Equal(14_000_000m, chuanBi.CostPlan);

        // Level1 "TRIỂN KHAI" không có Level2 con — leaf phải nằm thẳng dưới Level1.
        var trienKhai = result.RootTasks.Single(t => t.Name == "TRIỂN KHAI");
        Assert.Equal(2, trienKhai.Children.Count);

        var nghiemThu = trienKhai.Children.Single(t => t.Name == "Nghiệm thu");
        Assert.Equal(3, nghiemThu.Level);
        Assert.True(nghiemThu.IsPackage);
        Assert.Equal(0m, nghiemThu.MandayPlan);
        Assert.Equal(5_000_000m, nghiemThu.CostPlan);
        Assert.Contains(result.Warnings, w => w.Contains("Nghiệm thu") && w.Contains("trọn gói"));

        // Leaf trọn gói khác: cột "Dự toán" (G) để trống, số tiền thật nằm ở cột "Ghi chú" (H) —
        // gặp thật khi test với 1 file khách hàng khác, parser phải lấy H làm phương án dự phòng
        // thay vì để chi phí = 0.
        var hoTro = trienKhai.Children.Single(t => t.Name == "Hỗ trợ 24/7");
        Assert.True(hoTro.IsPackage);
        Assert.Equal(0m, hoTro.MandayPlan);
        Assert.Equal(8_000_000m, hoTro.CostPlan);
        Assert.Contains(result.Warnings, w => w.Contains("Hỗ trợ 24/7") && w.Contains("cột Ghi chú"));

        Assert.Equal(0m, trienKhai.MandayPlan);
        Assert.Equal(13_000_000m, trienKhai.CostPlan);

        // Dòng "Tổng" ở cuối sheet (tổng cộng cả bảng) không được lẫn vào làm leaf của mục cuối cùng —
        // bug thật đã phát hiện khi chạy thử với file mẫu thật trước khi có nhánh chặn này.
        Assert.DoesNotContain(result.RootTasks, t => t.Name == "Tổng");
    }

    private static Stream BuildSampleWorkbook()
    {
        var workbook = new XSSFWorkbook();

        var duToan = workbook.CreateSheet("Du toan");
        SetCell(duToan, 0, 0, "DỰ TOÁN");
        SetCell(duToan, 1, 1, "Hợp đồng:");
        SetCell(duToan, 1, 2, "Dự án: Test");
        SetCell(duToan, 5, 1, "Tổng dự toán");
        SetCell(duToan, 5, 2, 797_480_000d);

        SetCell(duToan, 7, 0, 1d);
        SetCell(duToan, 7, 1, "CP ăn ở, đi lại, bốc hàng");
        SetCell(duToan, 7, 3, "DỰ TOÁN");
        SetCell(duToan, 8, 3, 11_000_000d);

        SetCell(duToan, 10, 0, 2d);
        SetCell(duToan, 10, 1, "Chi phí Manday - ngày công");
        SetCell(duToan, 10, 3, "DỰ TOÁN");
        SetCell(duToan, 11, 3, 786_480_000d);

        SetCell(duToan, 13, 0, 3d);
        SetCell(duToan, 13, 1, "Chi phí vật tư phụ, nhân công");
        SetCell(duToan, 13, 3, "DỰ TOÁN");
        SetCell(duToan, 14, 3, 0d);

        SetCell(duToan, 16, 0, 4d);
        SetCell(duToan, 16, 1, "Chi phí vận chuyển");
        SetCell(duToan, 16, 3, "DỰ TOÁN");
        SetCell(duToan, 17, 3, 0d);

        var manDay = workbook.CreateSheet("MAN DAY");
        SetCell(manDay, 0, 0, "CHI PHÍ MANDAY");
        SetCell(manDay, 2, 0, "STT");
        SetCell(manDay, 2, 1, "Công việc");
        SetCell(manDay, 2, 2, "Số người");
        SetCell(manDay, 2, 3, "Số ngày");
        SetCell(manDay, 2, 4, "Nhóm nhân viên");
        SetCell(manDay, 2, 5, "Đơn giá");
        SetCell(manDay, 2, 6, "Dự toán");

        // Level 1 "CHUẨN BỊ" -> 2 Level 2 con, mỗi cái 1 leaf
        SetCell(manDay, 4, 0, 1d);
        SetCell(manDay, 4, 1, "CHUẨN BỊ");

        SetCell(manDay, 5, 0, "1.1");
        SetCell(manDay, 5, 1, "Quản trị");
        SetCell(manDay, 6, 1, "- Quản trị dự án");
        SetCell(manDay, 6, 2, 1d);
        SetCell(manDay, 6, 3, 2d);
        SetCell(manDay, 6, 4, "Engineer 1");
        SetCell(manDay, 6, 5, 1_000_000d);
        SetFormula(manDay, 6, 6, "C7*D7*F7"); // = 2,000,000 — verify đọc được công thức, không chỉ giá trị tĩnh

        SetCell(manDay, 8, 0, "1.2");
        SetCell(manDay, 8, 1, "Chuẩn bị");
        SetCell(manDay, 9, 1, "- Kiểm tra thiết bị");
        SetCell(manDay, 9, 2, 2d);
        SetCell(manDay, 9, 3, 3d);
        SetCell(manDay, 9, 4, "Engineer 2");
        SetCell(manDay, 9, 5, 2_000_000d);
        SetCell(manDay, 9, 6, 12_000_000d);

        // Level 1 "TRIỂN KHAI" không có Level 2 con — leaf đi thẳng, kiểu trọn gói (D="Gói")
        SetCell(manDay, 12, 0, 2d);
        SetCell(manDay, 12, 1, "TRIỂN KHAI");
        SetCell(manDay, 13, 1, "- Nghiệm thu");
        SetCell(manDay, 13, 2, 1d);
        SetCell(manDay, 13, 3, "Gói");
        SetCell(manDay, 13, 5, 5_000_000d);
        SetCell(manDay, 13, 6, 5_000_000d);

        // Leaf trọn gói khác: cột G (Dự toán) để trống, số tiền thật nằm ở cột H (Ghi chú).
        SetCell(manDay, 14, 1, "- Hỗ trợ 24/7");
        SetCell(manDay, 14, 3, "Gói");
        SetCell(manDay, 14, 7, 8_000_000d);

        // Dòng tổng cộng cuối sheet — cột A trống giống leaf nhưng KHÔNG phải là 1 task.
        SetCell(manDay, 16, 1, "Tổng");
        SetCell(manDay, 16, 6, 19_000_000d);

        var evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();
        evaluator.EvaluateAll();

        var stream = new MemoryStream();
        workbook.Write(stream, leaveOpen: true);
        stream.Position = 0;
        return stream;
    }

    private static void SetCell(ISheet sheet, int rowIndex, int colIndex, string value)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        row.CreateCell(colIndex).SetCellValue(value);
    }

    private static void SetCell(ISheet sheet, int rowIndex, int colIndex, double value)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        row.CreateCell(colIndex).SetCellValue(value);
    }

    private static void SetFormula(ISheet sheet, int rowIndex, int colIndex, string formula)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        row.CreateCell(colIndex).SetCellFormula(formula);
    }
}
