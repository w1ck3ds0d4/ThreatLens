using Microsoft.EntityFrameworkCore;
using ThreatLens.Data;

namespace ThreatLens.Dashboard;

/// Holds the raw bearer key the Dashboard uses when calling the Query API.
/// Populated once at startup (in Program.cs) from the ServiceCredentials
/// table. Registered as a singleton so the outbound HttpClient can read it.
public class QueryApiCredential
{
    public string Key { get; set; } = string.Empty;
}

/// DelegatingHandler that attaches the service credential as a bearer token
/// on every outbound HttpClient request.
public class BearerKeyHandler : DelegatingHandler
{
    private readonly QueryApiCredential _credential;

    public BearerKeyHandler(QueryApiCredential credential)
    {
        _credential = credential;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", _credential.Key);
        }
        return base.SendAsync(request, cancellationToken);
    }
}

public static class DashboardCredential
{
    public const string CredentialName = "dashboard-internal";

    /// Apply pending migrations, then ensure the dashboard-internal service
    /// credential exists and return its raw key. Called at startup before
    /// any HTTP client uses it.
    public static async Task<string> InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ThreatLensDbContext>();
        await db.Database.MigrateAsync();
        return await ApiKeyAuth.EnsureServiceCredentialAsync(db, CredentialName, CancellationToken.None);
    }
}
