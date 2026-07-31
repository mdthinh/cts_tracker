using CmcTs.Core.Data;
using CmcTs.Core.Services;
using CmcTs.Web.Auth;
using Microsoft.EntityFrameworkCore;

namespace CmcTs.Web.Endpoints;

// Tải file vật lý về máy người dùng cần 1 HTTP response bình thường (giống lý do AuthEndpoints
// không đi qua Blazor component) — không gắn [AllowAnonymous] nên vẫn kế thừa FallbackPolicy yêu
// cầu đăng nhập. Chỉ Admin hoặc thành viên của đúng dự án chứa tài liệu mới tải được — cùng luật
// CanViewProjectAsync áp dụng cho trang chi tiết dự án, tránh endpoint này thành lối tắt xem được
// tài liệu của dự án không tham gia dù trang chi tiết đã chặn.
public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        app.MapGet("/documents/{id:int}/download", async (
            int id,
            HttpContext http,
            IDbContextFactory<CmcTsDbContext> dbFactory,
            ITaskPermissionService permissionService) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var document = await db.ProjectDocuments.FindAsync(id);
            if (document is null || !File.Exists(document.FilePath))
            {
                return Results.NotFound();
            }

            var userId = http.User.GetUserId() ?? -1;
            var isAdmin = http.User.IsAdmin();
            if (!await permissionService.CanViewProjectAsync(document.ProjectId, userId, isAdmin))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var stream = File.OpenRead(document.FilePath);
            return Results.File(stream, "application/octet-stream", document.FileName);
        });
    }
}
