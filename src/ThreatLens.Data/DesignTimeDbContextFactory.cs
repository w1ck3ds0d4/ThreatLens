using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ThreatLens.Data;

/// Used by `dotnet ef` at design time. Aspire provides the real connection
/// string at runtime via AddNpgsqlDbContext; this factory only needs enough
/// to let EF build the model and emit migration SQL.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ThreatLensDbContext>
{
    public ThreatLensDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ThreatLensDbContext>()
            .UseNpgsql("Host=localhost;Database=threatlens;Username=threatlens;Password=threatlens")
            .Options;
        return new ThreatLensDbContext(options);
    }
}
