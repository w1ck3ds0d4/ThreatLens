namespace ThreatLens.Domain;

public class User
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public bool Active { get; set; } = true;
}
