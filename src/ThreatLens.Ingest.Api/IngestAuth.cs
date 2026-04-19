using Microsoft.EntityFrameworkCore;
using ThreatLens.Data;
using ThreatLens.Domain;

namespace ThreatLens.Ingest.Api;

public static class IngestAuth
{
    public const string InitialKeyName = "initial-bootstrap";

    /// Applies pending migrations and, if no keys exist, issues one and logs
    /// it once. Run at startup before the app accepts requests.
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ThreatLensDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        await db.Database.MigrateAsync();

        var hasAny = await db.ApiKeys.AnyAsync();
        if (hasAny) return;

        var raw = ApiKeyAuth.GenerateKey();
        var row = new ApiKey
        {
            Name = InitialKeyName,
            KeyHash = ApiKeyAuth.HashKey(raw),
            KeyPrefix = ApiKeyAuth.PrefixFor(raw),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.ApiKeys.Add(row);
        await db.SaveChangesAsync();

        logger.LogWarning("No ingest keys found; issued initial bootstrap key");
        logger.LogWarning("Initial ingest key (shown once): {Key}", raw);
        logger.LogWarning("Record it now - it cannot be retrieved later");
    }

    /// Endpoint filter that enforces a bearer token against the ApiKeys table.
    public static async ValueTask<object?> RequireKey(
        EndpointFilterInvocationContext ctx,
        EndpointFilterDelegate next)
    {
        var http = ctx.HttpContext;
        var header = http.Request.Headers.Authorization.ToString();
        const string scheme = "Bearer ";
        if (string.IsNullOrEmpty(header) || !header.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Unauthorized();
        }

        var presented = header[scheme.Length..].Trim();
        if (presented.Length == 0) return Results.Unauthorized();

        var db = http.RequestServices.GetRequiredService<ThreatLensDbContext>();
        var row = await ApiKeyAuth.ValidateAsync(db, presented, http.RequestAborted);
        if (row is null) return Results.Unauthorized();

        http.Items["IngestKeyId"] = row.Id;
        return await next(ctx);
    }
}
