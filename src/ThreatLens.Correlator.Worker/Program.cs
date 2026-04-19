using Microsoft.EntityFrameworkCore;
using ThreatLens.Correlator.Worker;
using ThreatLens.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<ThreatLensDbContext>("threatlens");
builder.AddRedisClient("redis");

builder.Services.AddHostedService<CorrelatorWorker>();

var host = builder.Build();

// Apply pending migrations before the worker starts polling so a cold
// start doesn't fire ProcessBatch against missing tables while waiting
// for some other service to run migrations.
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ThreatLensDbContext>();
    await db.Database.MigrateAsync();
}

host.Run();
