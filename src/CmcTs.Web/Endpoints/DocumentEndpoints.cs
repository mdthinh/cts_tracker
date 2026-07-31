using CmcTs.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace CmcTs.Web.Endpoints;

// Tải file vật lý về máy người dùng cần 1 HTTP response bình thường (giống lý do AuthEndpoints
// không đi qua Blazor component) — không gắn [AllowAnonymous] nên vẫn kế thừa FallbackPolicy yêu
// cầu đăng nhập; mọi thành viên đã đăng nhập được tải mọi tài liệu final, cùng mức công khai nội
// bộ như dashboard/cây công việc/lịch sử báo cáo.
public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        app.MapGet("/documents/{id:int}/download", async (int id, IDbContextFactory<CmcTsDbContext> dbFactory) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var document = await db.ProjectDocuments.FindAsync(id);
            if (document is null || !File.Exists(document.FilePath))
            {
                return Results.NotFound();
            }

            var stream = File.OpenRead(document.FilePath);
            return Results.File(stream, "application/octet-stream", document.FileName);
        });
    }
}
