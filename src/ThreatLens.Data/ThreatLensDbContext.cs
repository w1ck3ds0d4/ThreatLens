using Microsoft.EntityFrameworkCore;
using ThreatLens.Domain;

namespace ThreatLens.Data;

public class ThreatLensDbContext(DbContextOptions<ThreatLensDbContext> options) : DbContext(options)
{
    public DbSet<LogEvent> LogEvents => Set<LogEvent>();
    public DbSet<CorrelationRule> CorrelationRules => Set<CorrelationRule>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<ServiceCredential> ServiceCredentials => Set<ServiceCredential>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<LogEvent>(e =>
        {
            e.HasIndex(x => x.Timestamp);
            e.HasIndex(x => new { x.Correlated, x.Severity });
            e.Property(x => x.Source).HasMaxLength(128);
            e.Property(x => x.Host).HasMaxLength(256);
            e.Property(x => x.MatchedRule).HasMaxLength(128);
        });

        b.Entity<CorrelationRule>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Pattern).HasMaxLength(1024).IsRequired();
        });

        b.Entity<ApiKey>(e =>
        {
            e.HasIndex(x => x.KeyHash).IsUnique();
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.KeyPrefix);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.KeyHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.KeyPrefix).HasMaxLength(12).IsRequired();
        });

        b.Entity<ServiceCredential>(e =>
        {
            e.HasKey(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(128);
            e.Property(x => x.RawKey).HasMaxLength(128).IsRequired();
        });

        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
        });
    }
}
