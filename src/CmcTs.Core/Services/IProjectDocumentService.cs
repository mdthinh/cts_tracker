using CmcTs.Core.Entities;

namespace CmcTs.Core.Services;

public interface IProjectDocumentService
{
    Task<ProjectDocument> AddAsync(int projectId, string fileName, string filePath, int uploadedByUserId, CancellationToken ct = default);
}
