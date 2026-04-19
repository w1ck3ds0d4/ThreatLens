using Microsoft.EntityFrameworkCore;
using ThreatLens.Data;
using ThreatLens.Domain;
using ThreatLens.Ingest.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<ThreatLensDbContext>("threatlens");
builder.AddRedisClient("redis");
builder.Services.AddOpenApi();

var app = builder.Build();

await IngestAuth.InitializeAsync(app);

app.MapDefaultEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/events", async (IngestRequest req, ThreatLensDbContext db, CancellationToken ct) =>
{
    var ev = new LogEvent
    {
        Timestamp = req.Timestamp ?? DateTimeOffset.UtcNow,
        Source = req.Source,
        Severity = req.Severity,
        Message = req.Message,
        Host = req.Host,
        RawPayload = req.RawPayload,
        Correlated = false,
    };
    db.LogEvents.Add(ev);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/events/{ev.Id}", new { ev.Id });
}).AddEndpointFilter(ApiKeyEndpointFilter.RequireKey);

app.MapPost("/events/batch", async (IngestRequest[] reqs, ThreatLensDbContext db, CancellationToken ct) =>
{
    var now = DateTimeOffset.UtcNow;
    var events = reqs.Select(r => new LogEvent
    {
        Timestamp = r.Timestamp ?? now,
        Source = r.Source,
        Severity = r.Severity,
        Message = r.Message,
        Host = r.Host,
        RawPayload = r.RawPayload,
        Correlated = false,
    }).ToArray();
    db.LogEvents.AddRange(events);
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { count = events.Length });
}).AddEndpointFilter(ApiKeyEndpointFilter.RequireKey);

app.Run();

public record IngestRequest(
    string Source,
    Severity Severity,
    string Message,
    string? Host = null,
    string? RawPayload = null,
    DateTimeOffset? Timestamp = null);
