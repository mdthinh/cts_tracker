using CmcTs.Core.Entities;
using CmcTs.Core.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CmcTs.Core.Tests;

public class ProjectDocumentServiceTests
{
    [Fact]
    public async Task AddAsync_SavesDocument_LinkedToProjectAndUploader()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        int projectId, userId;

        using (var db = factory.CreateDbContext())
        {
            var user = new User { SamAccountName = "u", DisplayName = "U", CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
            db.SaveChanges();
            userId = user.Id;

            var project = new Project
            {
                Name = "Test", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.ENT,
                CreatedByUserId = user.Id, CreatedAt = DateTime.UtcNow,
            };
            db.Projects.Add(project);
            db.SaveChanges();
            projectId = project.Id;
        }

        var service = new ProjectDocumentService(factory);
        var document = await service.AddAsync(projectId, "Nghiệm thu.pdf", "/uploads/projects/1/documents/x.pdf", userId);

        Assert.NotEqual(0, document.Id);

        using var verifyDb = factory.CreateDbContext();
        var saved = await verifyDb.ProjectDocuments.SingleAsync(d => d.Id == document.Id);
        Assert.Equal(projectId, saved.ProjectId);
        Assert.Equal("Nghiệm thu.pdf", saved.FileName);
        Assert.Equal(userId, saved.UploadedByUserId);
    }

    [Fact]
    public async Task AddAsync_AllowsMultipleDocumentsPerProject()
    {
        var factory = new TestDbContextFactory(Guid.NewGuid().ToString());
        int projectId, userId;

        using (var db = factory.CreateDbContext())
        {
            var user = new User { SamAccountName = "u", DisplayName = "U", CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
            db.SaveChanges();
            userId = user.Id;

            var project = new Project
            {
                Name = "Test", FiscalYear = "2026-2027", BusinessUnit = BusinessUnit.ENT,
                CreatedByUserId = user.Id, CreatedAt = DateTime.UtcNow,
            };
            db.Projects.Add(project);
            db.SaveChanges();
            projectId = project.Id;
        }

        var service = new ProjectDocumentService(factory);
        await service.AddAsync(projectId, "Biên bản nghiệm thu.pdf", "/a.pdf", userId);
        await service.AddAsync(projectId, "Hồ sơ hoàn công.docx", "/b.docx", userId);

        using var verifyDb = factory.CreateDbContext();
        var count = await verifyDb.ProjectDocuments.CountAsync(d => d.ProjectId == projectId);
        Assert.Equal(2, count);
    }
}
