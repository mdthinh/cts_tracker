using CmcTs.Core.Data;
using CmcTs.Core.Entities;
using CmcTs.Core.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CmcTs.Core.Services;

public class LocalAccountService : ILocalAccountService
{
    private readonly IDbContextFactory<CmcTsDbContext> _dbFactory;
    private readonly LocalAdminOptions _options;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<LocalAccountService> _logger;

    public LocalAccountService(
        IDbContextFactory<CmcTsDbContext> dbFactory,
        IOptions<LocalAdminOptions> options,
        IPasswordHasher<User> passwordHasher,
        ILogger<LocalAccountService> logger)
    {
        _dbFactory = dbFactory;
        _options = options.Value;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task EnsureSeededAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Username) || string.IsNullOrWhiteSpace(_options.Password))
        {
            _logger.LogInformation("Bỏ qua seed local admin: chưa cấu hình LocalAdmin:Username/Password.");
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var exists = await db.Users.AnyAsync(
            u => u.IsLocalAccount && u.SamAccountName == _options.Username, ct);
        if (exists)
        {
            return;
        }

        var user = new User
        {
            SamAccountName = _options.Username,
            DisplayName = _options.Username,
            IsLocalAccount = true,
            GlobalRole = GlobalRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, _options.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã tạo tài khoản admin local {Username}.", _options.Username);
    }

    public async Task<User?> ValidateAsync(string username, string password, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var user = await db.Users.SingleOrDefaultAsync(
            u => u.IsLocalAccount && u.SamAccountName == username, ct);
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded
            ? user
            : null;
    }
}
