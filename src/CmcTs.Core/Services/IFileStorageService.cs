namespace CmcTs.Core.Services;

public interface IFileStorageService
{
    // Lưu file vào {UploadsRootPath}/{relativeFolder}/{timestamp}_{fileName}, trả về đường dẫn
    // tuyệt đối đã lưu (để ghi vào cột FilePath của EstimateImport/ProjectDocument).
    Task<string> SaveAsync(string relativeFolder, string fileName, Stream content, CancellationToken ct = default);
}
