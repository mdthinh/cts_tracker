using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CmcTs.Core.Data;

// Chỉ dùng bởi công cụ `dotnet ef migrations add/update` lúc design-time (không chạy trong app thật).
// Connection string ở đây không cần trỏ tới DB có thật vì lệnh `migrations add` không mở kết nối.
public class CmcTsDbContextFactory : IDesignTimeDbContextFactory<CmcTsDbContext>
{
    public CmcTsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CmcTsDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=CmcTsTracker;Trusted_Connection=True;TrustServerCertificate=True;");
        return new CmcTsDbContext(optionsBuilder.Options);
    }
}
