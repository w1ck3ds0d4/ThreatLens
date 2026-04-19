using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ThreatLens.Data;

namespace Microsoft.Extensions.Hosting;

/// Endpoint filter that requires an Authorization: Bearer token which
/// matches a non-revoked row in the ApiKeys table. Attach to any
/// minimal-API endpoint with .AddEndpointFilter(ApiKeyEndpointFilter.RequireKey).
public static class ApiKeyEndpointFilter
{
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

        http.Items["ApiKeyId"] = row.Id;
        return await next(ctx);
    }
}
