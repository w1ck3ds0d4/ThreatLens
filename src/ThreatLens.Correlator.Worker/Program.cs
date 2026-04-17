using ThreatLens.Correlator.Worker;
using ThreatLens.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<ThreatLensDbContext>("threatlens");
builder.AddRedisClient("redis");

builder.Services.AddHostedService<CorrelatorWorker>();

var host = builder.Build();
host.Run();
