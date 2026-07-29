using CmcTs.Core.Data;
using CmcTs.Core.Entities;
using CmcTs.Core.Options;
using CmcTs.Core.Services;
using CmcTs.Web.Components;
using CmcTs.Web.Endpoints;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<CmcTsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CmcTsDb")));

builder.Services.Configure<LdapOptions>(builder.Configuration.GetSection(LdapOptions.SectionName));
builder.Services.AddScoped<ILdapService, LdapService>();
builder.Services.AddScoped<IUserProvisioningService, UserProvisioningService>();

builder.Services.Configure<LocalAdminOptions>(builder.Configuration.GetSection(LocalAdminOptions.SectionName));
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ILocalAccountService, LocalAccountService>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization(options =>
{
    // Mặc định mọi trang đều yêu cầu đăng nhập, trừ trang gắn [AllowAnonymous] (vd /login).
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        await scope.ServiceProvider.GetRequiredService<ILocalAccountService>().EnsureSeededAsync();
    }
    catch (Exception ex)
    {
        // Không chặn khởi động app vì lỗi này (thường do DB chưa migrate) — log rõ để ops xử lý,
        // thay vì crash-loop toàn bộ tiến trình khi mới deploy lần đầu.
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>()
            .LogError(ex, "Seed tài khoản admin local thất bại — kiểm tra đã chạy `dotnet ef database update` chưa.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapAuthEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
