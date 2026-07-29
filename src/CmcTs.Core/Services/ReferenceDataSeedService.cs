using CmcTs.Core.Data;
using CmcTs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CmcTs.Core.Services;

public class ReferenceDataSeedService : IReferenceDataSeedService
{
    // Theo danh sách người dùng cung cấp lúc đặc tả yêu cầu; mở rộng được qua DB sau này.
    private static readonly string[] DefaultTechnologies =
    {
        "Server", "SAN Switch", "Storage", "K8s", "Microsoft", "Network", "Security",
    };

    private readonly IDbContextFactory<CmcTsDbContext> _dbFactory;

    public ReferenceDataSeedService(IDbContextFactory<CmcTsDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task EnsureSeededAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var existing = await db.Technologies.Select(t => t.Name).ToListAsync(ct);
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = DefaultTechnologies.Where(name => !existingSet.Contains(name));
        foreach (var name in missing)
        {
            db.Technologies.Add(new Technology { Name = name, IsActive = true });
        }

        await db.SaveChangesAsync(ct);
    }
}
