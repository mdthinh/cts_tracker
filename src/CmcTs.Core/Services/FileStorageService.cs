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
        var dir = Path.Combine(_options.UploadsRootPath, relativeFolder);
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
