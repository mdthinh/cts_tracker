using CmcTs.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace CmcTs.Core.Tests;

// IDbContextFactory<CmcTsDbContext> dùng EF InMemory provider, để test các service phụ thuộc
// factory (giống hệt cách chúng được inject trong app thật) mà không cần SQL Server.
public class TestDbContextFactory : IDbContextFactory<CmcTsDbContext>
{
    private readonly DbContextOptions<CmcTsDbContext> _options;

    public TestDbContextFactory(string dbName)
    {
        _options = new DbContextOptionsBuilder<CmcTsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
    }

    public CmcTsDbContext CreateDbContext() => new(_options);

    public Task<CmcTsDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());
}
