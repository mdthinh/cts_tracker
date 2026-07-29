namespace CmcTs.Core;

// Năm tài chính CMC TS: 1/4 -> 31/3 năm sau. Vd ngày 29/7/2026 thuộc NTC "2026-2027", quý 2 (T7-T9).
public static class FiscalYear
{
    public static int GetStartYear(DateTime date) => date.Month >= 4 ? date.Year : date.Year - 1;

    public static string GetLabel(DateTime date)
    {
        var start = GetStartYear(date);
        return $"{start}-{start + 1}";
    }

    public static int GetQuarter(DateTime date)
    {
        var fiscalMonthIndex = ((date.Month - 4 + 12) % 12) + 1; // 1..12, tháng 4 = 1
        return (fiscalMonthIndex - 1) / 3 + 1; // 1..4
    }
}
