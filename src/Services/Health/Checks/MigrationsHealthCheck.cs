using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TaskPilot.Data;
using TaskPilot.Models.Health;

namespace TaskPilot.Services.Health.Checks;

/// <summary>Verifies that all EF Core migrations have been applied (0 pending).</summary>
public sealed class MigrationsHealthCheck(ApplicationDbContext dbContext) : IHealthCheckComponent
{
    public string Name => "migrations";
    public bool IsRequired => true;

    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var pending = (await dbContext.Database.GetPendingMigrationsAsync(ct)).ToList();
            sw.Stop();

            var count = pending.Count.ToString();
            if (pending.Count == 0)
            {
                return new HealthCheckResult
                {
                    Name = Name, Status = HealthStatuses.Healthy, Duration = sw.Elapsed,
                    IsRequired = IsRequired, Message = "All migrations applied.",
                    Data = new Dictionary<string, string> { ["pendingMigrations"] = count }
                };
            }

            return new HealthCheckResult
            {
                Name = Name, Status = HealthStatuses.Unhealthy, Duration = sw.Elapsed,
                IsRequired = IsRequired,
                Message = $"{count} pending migration(s): {string.Join(", ", pending)}",
                Data = new Dictionary<string, string> { ["pendingMigrations"] = count }
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new HealthCheckResult
            {
                Name = Name, Status = HealthStatuses.Unhealthy, Duration = sw.Elapsed,
                IsRequired = IsRequired,
                Message = $"{ex.GetType().Name}: {ex.Message}",
                Data = new Dictionary<string, string> { ["pendingMigrations"] = "unknown" }
            };
        }
    }
}
