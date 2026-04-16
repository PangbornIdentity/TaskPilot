using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TaskPilot.Data;
using TaskPilot.Models.Health;

namespace TaskPilot.Services.Health.Checks;

/// <summary>Verifies that the database is reachable via <c>CanConnectAsync</c> within 2 seconds.</summary>
public sealed class DatabaseHealthCheck(ApplicationDbContext dbContext) : IHealthCheckComponent
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(2);

    public string Name => "database";
    public bool IsRequired => true;

    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(_timeout);

            var canConnect = await dbContext.Database.CanConnectAsync(timeoutCts.Token);
            sw.Stop();

            return canConnect
                ? Healthy(sw.Elapsed)
                : Unhealthy(sw.Elapsed, "Database returned false from CanConnectAsync.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            return Unhealthy(sw.Elapsed, "Database connectivity check timed out after 2 seconds.");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Unhealthy(sw.Elapsed, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private HealthCheckResult Healthy(TimeSpan duration) => new()
    {
        Name = Name, Status = HealthStatuses.Healthy, Duration = duration,
        IsRequired = IsRequired, Message = "Database is reachable."
    };

    private HealthCheckResult Unhealthy(TimeSpan duration, string message) => new()
    {
        Name = Name, Status = HealthStatuses.Unhealthy, Duration = duration,
        IsRequired = IsRequired, Message = message
    };
}
