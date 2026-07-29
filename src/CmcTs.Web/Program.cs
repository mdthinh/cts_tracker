using CmcTs.Core.Data;
using CmcTs.Core.Options;
using CmcTs.Core.Services;
using CmcTs.Web.Components;
using CmcTs.Web.Endpoints;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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
