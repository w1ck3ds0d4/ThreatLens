using Microsoft.EntityFrameworkCore;
using ThreatLens.Data;
using ThreatLens.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<ThreatLensDbContext>("threatlens");
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/events", async (
    ThreatLensDbContext db,
    int limit,
    long? afterId,
    Severity? minSeverity,
    string? source,
    CancellationToken ct) =>
{
    var take = limit <= 0 ? 100 : Math.Clamp(limit, 1, 500);
    var q = db.LogEvents.AsNoTracking().OrderByDescending(e => e.Id).AsQueryable();
    if (afterId is not null) q = q.Where(e => e.Id < afterId);
    if (minSeverity is not null) q = q.Where(e => e.Severity >= minSeverity);
    if (!string.IsNullOrWhiteSpace(source)) q = q.Where(e => e.Source == source);
    var items = await q.Take(take).ToListAsync(ct);
    return Results.Ok(items);
});

app.MapGet("/events/{id:long}", async (long id, ThreatLensDbContext db, CancellationToken ct) =>
{
    var ev = await db.LogEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
    return ev is null ? Results.NotFound() : Results.Ok(ev);
});

app.MapGet("/stats", async (ThreatLensDbContext db, CancellationToken ct) =>
{
    var cutoff = DateTimeOffset.UtcNow.AddHours(-24);
    var bySeverity = await db.LogEvents
        .Where(e => e.Timestamp >= cutoff)
        .GroupBy(e => e.Severity)
        .Select(g => new { Severity = g.Key, Count = g.Count() })
        .ToListAsync(ct);

    var total = await db.LogEvents.CountAsync(ct);
    var pending = await db.LogEvents.CountAsync(e => !e.Correlated, ct);

    return Results.Ok(new { total, pending, bySeverity });
});

app.Run();
