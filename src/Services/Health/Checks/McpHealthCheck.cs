using System.Diagnostics;
using Microsoft.AspNetCore.Routing;
using TaskPilot.Models.Health;

namespace TaskPilot.Services.Health.Checks;

/// <summary>Verifies that the MCP endpoint (<c>/mcp</c>) is registered in the routing table.</summary>
public sealed class McpHealthCheck(EndpointDataSource endpointDataSource) : IHealthCheckComponent
{
    public string Name => "mcp";
    public bool IsRequired => false;

    public Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var hasMcp = endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Any(e => e.RoutePattern.RawText?.Contains("/mcp") == true
                   || e.RoutePattern.RawText == "mcp");
        sw.Stop();

        return Task.FromResult(new HealthCheckResult
        {
            Name = Name,
            Status = hasMcp ? HealthStatuses.Healthy : HealthStatuses.Unhealthy,
            Duration = sw.Elapsed,
            IsRequired = IsRequired,
            Message = hasMcp ? "MCP endpoint registered." : "MCP endpoint not found in routing table."
        });
    }
}
