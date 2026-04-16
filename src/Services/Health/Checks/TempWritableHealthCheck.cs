using System.Diagnostics;
using TaskPilot.Models.Health;

namespace TaskPilot.Services.Health.Checks;

/// <summary>Verifies that the process can write to and delete a file in <c>Path.GetTempPath()</c>.</summary>
public sealed class TempWritableHealthCheck : IHealthCheckComponent
{
    public string Name => "temp-writable";
    public bool IsRequired => false;

    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var filePath = Path.Combine(Path.GetTempPath(), $"taskpilot_health_{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(filePath, [0x01], ct);
            File.Delete(filePath);
            sw.Stop();

            return new HealthCheckResult
            {
                Name = Name, Status = HealthStatuses.Healthy, Duration = sw.Elapsed,
                IsRequired = IsRequired, Message = "Temp directory is writable."
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new HealthCheckResult
            {
                Name = Name, Status = HealthStatuses.Unhealthy, Duration = sw.Elapsed,
                IsRequired = IsRequired,
                Message = $"{ex.GetType().Name}: {ex.Message}"
            };
        }
        finally
        {
            try { if (File.Exists(filePath)) File.Delete(filePath); } catch { /* best effort */ }
        }
    }
}
