using Microsoft.EntityFrameworkCore;
using ThreatLens.Data;
using ThreatLens.Domain;

namespace ThreatLens.Ingest.Api;

public static class IngestAuth
{
    public const string InitialKeyName = "initial-bootstrap";

    /// Applies pending migrations and, if no external ingest key has been
    /// issued, seeds one and logs it once.
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ThreatLensDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        await db.Database.MigrateAsync();

        var alreadySeeded = await db.ApiKeys.AnyAsync(k => k.Name == InitialKeyName);
        if (alreadySeeded) return;

        var raw = ApiKeyAuth.GenerateKey();
        db.ApiKeys.Add(new ApiKey
        {
            Name = InitialKeyName,
            KeyHash = ApiKeyAuth.HashKey(raw),
            KeyPrefix = ApiKeyAuth.PrefixFor(raw),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        logger.LogWarning("Seeded initial ingest key");
        logger.LogWarning("Initial ingest key (shown once): {Key}", raw);
        logger.LogWarning("Record it now - it cannot be retrieved later");
    }
}
