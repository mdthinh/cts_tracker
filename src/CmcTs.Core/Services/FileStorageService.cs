using CmcTs.Core.Options;
using Microsoft.Extensions.Options;

namespace CmcTs.Core.Services;

public class FileStorageService : IFileStorageService
{
    private readonly StorageOptions _options;

    public FileStorageService(IOptions<StorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> SaveAsync(string relativeFolder, string fileName, Stream content, CancellationToken ct = default)
    {
        // Đường dẫn tuyệt đối (vd "D:\...") dùng nguyên; đường dẫn tương đối (vd "App_Data/Uploads")
        // luôn quy về thư mục chứa file thực thi (AppContext.BaseDirectory), không phụ thuộc
        // working directory lúc chạy (dotnet run / IIS / click đúp exe mỗi cái 1 kiểu khác nhau).
        var root = Path.IsPathRooted(_options.UploadsRootPath)
            ? _options.UploadsRootPath
            : Path.Combine(AppContext.BaseDirectory, _options.UploadsRootPath);

        var dir = Path.Combine(root, relativeFolder);
        Directory.CreateDirectory(dir);

        var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(dir, safeFileName);

        await using var fileStream = File.Create(fullPath);
        if (content.CanSeek)
        {
            content.Position = 0;
        }
        await content.CopyToAsync(fileStream, ct);

        return fullPath;
    }
}
