namespace ThreatLens.Domain;

/// A raw service-to-service credential stored in the database for local
/// bootstrap of internal components (e.g. the Dashboard). Never exposed to
/// an end user; kept plaintext so a service can read it on startup without
/// out-of-band secret delivery. Rotate by deleting the row and restarting.
public class ServiceCredential
{
    public string Name { get; set; } = string.Empty;
    public string RawKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
