using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using ThreatLens.Data;
using ThreatLens.Domain;

namespace ThreatLens.Correlator.Worker;

public class CorrelatorWorker(
    ILogger<CorrelatorWorker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);
    private static readonly int BatchSize = 200;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatch(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Correlator batch failed");
            }
            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessBatch(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ThreatLensDbContext>();

        var rules = await db.CorrelationRules
            .Where(r => r.Enabled)
            .AsNoTracking()
            .ToListAsync(ct);

        if (rules.Count == 0) return;

        var pending = await db.LogEvents
            .Where(e => !e.Correlated)
            .OrderBy(e => e.Id)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        int matched = 0;
        foreach (var ev in pending)
        {
            foreach (var rule in rules)
            {
                bool hit;
                try
                {
                    hit = Regex.IsMatch(ev.Message, rule.Pattern, RegexOptions.IgnoreCase, MatchTimeout);
                }
                catch (RegexMatchTimeoutException)
                {
                    logger.LogWarning(
                        "Rule {RuleName} timed out against event {EventId}; skipping rule for this event",
                        rule.Name, ev.Id);
                    continue;
                }
                catch (ArgumentException ex)
                {
                    logger.LogWarning(ex, "Rule {RuleName} has an invalid pattern; skipping", rule.Name);
                    continue;
                }

                if (hit)
                {
                    ev.MatchedRule = rule.Name;
                    if (rule.ElevateTo > ev.Severity)
                    {
                        ev.Severity = rule.ElevateTo;
                    }
                    matched++;
                    break;
                }
            }
            ev.Correlated = true;
            ev.CorrelatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Correlated {Count} events, {Matched} matched a rule",
            pending.Count, matched);
    }
}
