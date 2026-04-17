var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var threatLensDb = postgres.AddDatabase("threatlens");

var redis = builder.AddRedis("redis").WithRedisInsight();

builder.AddProject<Projects.ThreatLens_Ingest_Api>("ingest-api")
    .WithReference(threatLensDb)
    .WithReference(redis)
    .WaitFor(threatLensDb)
    .WaitFor(redis);

builder.AddProject<Projects.ThreatLens_Correlator_Worker>("correlator")
    .WithReference(threatLensDb)
    .WithReference(redis)
    .WaitFor(threatLensDb)
    .WaitFor(redis);

builder.AddProject<Projects.ThreatLens_Query_Api>("query-api")
    .WithReference(threatLensDb)
    .WaitFor(threatLensDb);

builder.AddProject<Projects.ThreatLens_Dashboard>("dashboard")
    .WithReference(redis)
    .WaitFor(redis);

builder.Build().Run();
