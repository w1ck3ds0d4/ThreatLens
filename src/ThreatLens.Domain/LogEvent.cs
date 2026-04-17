namespace ThreatLens.Domain;

public enum Severity
{
    Debug = 0,
    Info = 1,
    Warn = 2,
    Error = 3,
    Critical = 4,
}

public class LogEvent
{
    public long Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public Severity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Host { get; set; }
    public string? RawPayload { get; set; }
    public bool Correlated { get; set; }
    public string? MatchedRule { get; set; }
    public DateTimeOffset? CorrelatedAt { get; set; }
}

public class CorrelationRule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public Severity ElevateTo { get; set; }
    public bool Enabled { get; set; } = true;
}
