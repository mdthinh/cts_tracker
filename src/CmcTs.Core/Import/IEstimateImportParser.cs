namespace CmcTs.Core.Import;

public interface IEstimateImportParser
{
    // fileStream: nội dung file .xls hoặc .xlsx "Dự toán". Không ghi DB — chỉ parse thuần.
    ParsedEstimateResult Parse(Stream fileStream);
}
