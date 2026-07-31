using CmcTs.Core.Data;
using CmcTs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CmcTs.Core.Services;

public class ProjectDocumentService : IProjectDocumentService
{
    private readonly IDbContextFactory<CmcTsDbContext> _dbFactory;

    public ProjectDocumentService(IDbContextFactory<CmcTsDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<ProjectDocument> AddAsync(int projectId, string fileName, string filePath, int uploadedByUserId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var document = new ProjectDocument
        {
            ProjectId = projectId,
            FileName = fileName,
            FilePath = filePath,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = DateTime.UtcNow,
        };

        db.ProjectDocuments.Add(document);
        await db.SaveChangesAsync(ct);
        return document;
    }
}
