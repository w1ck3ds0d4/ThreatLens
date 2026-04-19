using Microsoft.AspNetCore.Authentication;
using ThreatLens.Dashboard;
using ThreatLens.Dashboard.Components;
using ThreatLens.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<ThreatLensDbContext>("threatlens");
builder.AddRedisClient("redis");

builder.Services.AddSingleton<QueryApiCredential>();
builder.Services.AddTransient<BearerKeyHandler>();
builder.Services.AddHttpClient("query-api", c => c.BaseAddress = new Uri("http+https://query-api"))
    .AddHttpMessageHandler<BearerKeyHandler>();
builder.Services.AddServiceDiscovery();

builder.Services.AddHttpContextAccessor();
builder.Services.AddDashboardAuth();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.Services.GetRequiredService<QueryApiCredential>().Key =
    await DashboardCredential.InitializeAsync(app);

await DashboardAuth.SeedBootstrapAdminAsync(app);

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(DashboardAuth.SchemeName);
    return Results.Redirect(DashboardAuth.LoginPath);
});

app.Run();
