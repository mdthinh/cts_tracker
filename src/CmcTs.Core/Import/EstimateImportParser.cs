using NPOI.SS.UserModel;

namespace CmcTs.Core.Import;

// Đọc file "Dự toán" (.xls cũ hoặc .xlsx) bằng NPOI (WorkbookFactory tự nhận diện định dạng).
//
// Nhận diện cấp task DỰA VÀO ĐỊNH DẠNG CỘT A (STT), không dựa vào màu ô — vì màu tô có thể khác
// nhau giữa các file do người khác nhau lập (đã quan sát thấy điều này ngay trong sheet "Du toan"
// mẫu: 4 dòng DỰ TOÁN dùng 4 màu nền khác nhau). Quy tắc suy ra được từ cấu trúc dữ liệu, ổn định
// hơn nhiều so với dò màu:
//   - Cột A rỗng, cột B có nội dung  => Level 3 (leaf, dòng công việc cụ thể)
//   - Cột A dạng "N" (không có dấu chấm)     => Level 1
//   - Cột A dạng "N.M" (có dấu chấm)         => Level 2
// Cây được dựng bằng stack theo thứ tự dòng + level, nên tự xử lý được cả trường hợp 1 mục Level 1
// không có Level 2 con mà đi thẳng xuống leaf.
public class EstimateImportParser : IEstimateImportParser
{
    private const int ColStt = 0; // A
    private const int ColName = 1; // B
    private const int ColHeadCount = 2; // C - Số người
    private const int ColDays = 3; // D - Số ngày (hoặc chữ "Gói")
    private const int ColStaffGroup = 4; // E - Nhóm nhân viên
    private const int ColUnitPrice = 5; // F - Đơn giá
    private const int ColCost = 6; // G - Dự toán (thành tiền)

    // Trên sheet "Du toan": nhãn "DỰ TOÁN" và số tiền tương ứng (dòng ngay dưới) đều nằm ở cột D.
    private const int ColSummaryLabel = 3;
    private const int ColSummaryAmount = 3;

    public ParsedEstimateResult Parse(Stream fileStream)
    {
        using var workbook = WorkbookFactory.Create(fileStream);
        var evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();

        var result = new ParsedEstimateResult
        {
            CostSummaries = ParseCostSummary(workbook, evaluator),
        };

        var manDaySheet = FindSheet(workbook, "man day") ?? FindSheet(workbook, "manday");
        if (manDaySheet is null)
        {
            result.Warnings.Add("Không tìm thấy sheet \"MAN DAY\" trong file — không có dữ liệu công việc để tạo task.");
            return result;
        }

        result.RootTasks = ParseTaskTree(manDaySheet, evaluator, result.Warnings);
        return result;
    }

    private static ISheet? FindSheet(IWorkbook workbook, string nameContainsLower)
    {
        for (var i = 0; i < workbook.NumberOfSheets; i++)
        {
            var sheet = workbook.GetSheetAt(i);
            if (sheet.SheetName.ToLowerInvariant().Contains(nameContainsLower))
            {
                return sheet;
            }
        }
        return null;
    }

    private static List<ParsedCostSummaryLine> ParseCostSummary(IWorkbook workbook, IFormulaEvaluator evaluator)
    {
        var lines = new List<ParsedCostSummaryLine>();
        var sheet = FindSheet(workbook, "toan") ?? workbook.GetSheetAt(0);

        for (var r = sheet.FirstRowNum; r <= sheet.LastRowNum; r++)
        {
            var row = sheet.GetRow(r);
            if (row is null)
            {
                continue;
            }

            var label = GetStringValue(row.GetCell(ColSummaryLabel))?.Trim();
            if (!string.Equals(label, "DỰ TOÁN", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var category = GetStringValue(row.GetCell(1))?.Trim() ?? $"Mục dòng {r + 1}";
            var amountRow = sheet.GetRow(r + 1);
            var amount = amountRow is null ? 0m : GetNumericValue(amountRow.GetCell(ColSummaryAmount), evaluator) ?? 0m;

            lines.Add(new ParsedCostSummaryLine
            {
                Category = category,
                Amount = amount,
                IsRevenueSource = category.Contains("manday", StringComparison.OrdinalIgnoreCase),
            });
        }

        return lines;
    }

    private static List<ParsedTaskNode> ParseTaskTree(ISheet sheet, IFormulaEvaluator evaluator, List<string> warnings)
    {
        var flat = new List<ParsedTaskNode>();

        for (var r = sheet.FirstRowNum; r <= sheet.LastRowNum; r++)
        {
            var row = sheet.GetRow(r);
            if (row is null)
            {
                continue;
            }

            var rawName = GetStringValue(row.GetCell(ColName))?.Trim();
            if (string.IsNullOrWhiteSpace(rawName) || string.Equals(rawName, "Công việc", StringComparison.OrdinalIgnoreCase))
            {
                continue; // dòng trống/ngăn cách hoặc dòng tiêu đề bảng
            }

            var code = GetCodeString(row.GetCell(ColStt));

            // Dòng "Tổng"/"Tổng cộng" ở cuối bảng là số tổng cộng của cả sheet, không phải 1 task —
            // nếu không chặn ở đây, dòng này bị hiểu nhầm thành leaf cuối cùng, cộng dư đúng bằng
            // tổng cả sheet vào mục Level 1 cuối (đã xảy ra thật khi test với file mẫu thật).
            if (code is null && rawName.StartsWith("Tổng", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            var level = code is null ? 3 : code.Contains('.') ? 2 : 1;

            var node = new ParsedTaskNode
            {
                Level = level,
                Code = level == 3 ? null : code,
                Name = rawName.TrimStart('-', ' '),
                SourceRow = r + 1,
            };

            if (level == 3)
            {
                PopulateLeaf(node, row, evaluator, warnings);
            }

            flat.Add(node);
        }

        return TaskTreeBuilder.Build(flat, warnings);
    }

    private static void PopulateLeaf(ParsedTaskNode node, IRow row, IFormulaEvaluator evaluator, List<string> warnings)
    {
        var headCount = GetNumericValue(row.GetCell(ColHeadCount), evaluator);
        var daysCell = row.GetCell(ColDays);
        var daysNumeric = GetNumericValue(daysCell, evaluator);
        var daysText = GetStringValue(daysCell);

        node.HeadCount = headCount;
        node.StaffGroup = GetStringValue(row.GetCell(ColStaffGroup))?.Trim();
        node.UnitPrice = GetNumericValue(row.GetCell(ColUnitPrice), evaluator);
        node.CostPlan = GetNumericValue(row.GetCell(ColCost), evaluator) ?? 0m;

        if (daysNumeric is not null)
        {
            node.Days = daysNumeric;
            node.MandayPlan = (headCount ?? 0m) * daysNumeric.Value;
            node.IsPackage = false;
        }
        else
        {
            node.IsPackage = true;
            node.MandayPlan = 0m;
            if (!string.IsNullOrWhiteSpace(daysText))
            {
                warnings.Add($"Dòng {node.SourceRow}: \"{node.Name}\" là trọn gói (\"{daysText}\"), không tính manday, chỉ tính chi phí.");
            }
        }
    }

    private static string? GetCodeString(ICell? cell)
    {
        if (cell is null)
        {
            return null;
        }

        return cell.CellType switch
        {
            CellType.Numeric => FormatNumeric(cell.NumericCellValue),
            CellType.String => string.IsNullOrWhiteSpace(cell.StringCellValue) ? null : cell.StringCellValue.Trim(),
            _ => null,
        };
    }

    private static string? GetStringValue(ICell? cell)
    {
        if (cell is null)
        {
            return null;
        }

        return cell.CellType switch
        {
            CellType.String => cell.StringCellValue,
            CellType.Numeric => FormatNumeric(cell.NumericCellValue),
            _ => null,
        };
    }

    private static decimal? GetNumericValue(ICell? cell, IFormulaEvaluator evaluator)
    {
        if (cell is null)
        {
            return null;
        }

        if (cell.CellType == CellType.Numeric)
        {
            return (decimal)cell.NumericCellValue;
        }

        if (cell.CellType == CellType.Formula)
        {
            var evaluated = evaluator.Evaluate(cell);
            return evaluated.CellType == CellType.Numeric ? (decimal)evaluated.NumberValue : null;
        }

        return null;
    }

    private static string FormatNumeric(double value)
    {
        return value == Math.Floor(value) && !double.IsInfinity(value)
            ? ((long)value).ToString()
            : value.ToString("0.##");
    }
}
