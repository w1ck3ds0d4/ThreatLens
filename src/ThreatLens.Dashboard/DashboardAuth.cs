using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ThreatLens.Data;
using ThreatLens.Domain;

namespace ThreatLens.Dashboard;

public static class DashboardAuth
{
    public const string SchemeName = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string LoginPath = "/login";
    public const string LogoutPath = "/logout";
    public const string BootstrapAdminEmail = "admin@threatlens.local";

    public static void AddDashboardAuth(this IServiceCollection services)
    {
        services.AddAuthentication(SchemeName)
            .AddCookie(SchemeName, opt =>
            {
                opt.LoginPath = LoginPath;
                opt.LogoutPath = LogoutPath;
                opt.AccessDeniedPath = LoginPath;
                opt.ExpireTimeSpan = TimeSpan.FromHours(8);
                opt.SlidingExpiration = true;
                opt.Cookie.Name = "threatlens.auth";
                opt.Cookie.HttpOnly = true;
                opt.Cookie.SameSite = SameSiteMode.Strict;
                opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });
        services.AddAuthorization(opt =>
        {
            opt.FallbackPolicy = opt.DefaultPolicy;
        });
        services.AddCascadingAuthenticationState();
    }

    /// Seed the bootstrap admin if no users exist, logging the generated
    /// password once at WARNING level.
    public static async Task SeedBootstrapAdminAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ThreatLensDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        if (await db.Users.AnyAsync()) return;

        var password = Environment.GetEnvironmentVariable("THREATLENS_ADMIN_PASSWORD");
        var generated = string.IsNullOrEmpty(password) || password.Length < 12;
        if (generated) password = PasswordHasher.GenerateBootstrapPassword();

        db.Users.Add(new User
        {
            Email = BootstrapAdminEmail,
            PasswordHash = PasswordHasher.Hash(password!),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        if (generated)
        {
            logger.LogWarning("No dashboard users found; created bootstrap admin");
            logger.LogWarning("Email: {Email}", BootstrapAdminEmail);
            logger.LogWarning("Password (shown once): {Password}", password);
            logger.LogWarning("Record it now and rotate after first login");
        }
        else
        {
            logger.LogWarning("Created bootstrap admin {Email} from THREATLENS_ADMIN_PASSWORD", BootstrapAdminEmail);
        }
    }

    public static ClaimsPrincipal BuildPrincipal(User user)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
            ],
            SchemeName);
        return new ClaimsPrincipal(identity);
    }
}
