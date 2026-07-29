using CmcTs.Core.Data;
using CmcTs.Core.Entities;
using CmcTs.Core.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CmcTs.Core.Services;

public class UserProvisioningService : IUserProvisioningService
{
    private readonly CmcTsDbContext _db;
    private readonly LdapOptions _options;

    public UserProvisioningService(CmcTsDbContext db, IOptions<LdapOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<User> ProvisionOnLoginAsync(AdUserInfo adUser, CancellationToken ct = default)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.SamAccountName == adUser.SamAccountName, ct);
        var isBootstrapAdmin = _options.InitialAdminSamAccountNames
            .Any(s => string.Equals(s, adUser.SamAccountName, StringComparison.OrdinalIgnoreCase));

        if (user is null)
        {
            user = new User
            {
                SamAccountName = adUser.SamAccountName,
                DisplayName = adUser.DisplayName,
                Email = adUser.Email,
                Department = adUser.Department,
                GlobalRole = isBootstrapAdmin ? GlobalRole.Admin : GlobalRole.Viewer,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastSyncedAt = DateTime.UtcNow,
            };
            _db.Users.Add(user);
        }
        else
        {
            user.DisplayName = adUser.DisplayName;
            user.Email = adUser.Email;
            user.Department = adUser.Department;
            user.LastSyncedAt = DateTime.UtcNow;

            // Chỉ nâng lên Admin nếu nằm trong danh sách bootstrap và hiện đang là Viewer —
            // không tự động hạ quyền 1 Admin đã được gán tay trong DB.
            if (isBootstrapAdmin && user.GlobalRole != GlobalRole.Admin)
            {
                user.GlobalRole = GlobalRole.Admin;
            }
        }

        await _db.SaveChangesAsync(ct);
        return user;
    }
}
