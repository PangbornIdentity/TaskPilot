using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Diagnostics;
using TaskPilot.Models.Health;
using TaskPilot.Services.Health;

namespace TaskPilot.Controllers;

/// <summary>
/// Health and diagnostics endpoints. All anonymous, all excluded from audit log.
/// </summary>
[AllowAnonymous]
[Route("api/v1/health")]
[ApiController]
public sealed class HealthController(
    IHealthService healthService,
    AssetsService assetsService,
    IWebHostEnvironment env) : ControllerBase
{
    /// <summary>
    /// Version — process identity, git commit, and build timestamp.
    /// Always 200 if the process is responding.
    /// </summary>
    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        AddHealthHeaders();
        var response = new VersionResponse
        {
            Version = BuildInfo.Version,
            GitCommit = BuildInfo.GitCommit,
            GitCommitShort = BuildInfo.GitCommitShort,
            BuildTimestampUtc = BuildInfo.BuildTimestampUtc,
            Environment = env.EnvironmentName,
            MachineName = System.Environment.MachineName,
            Uptime = DateTime.UtcNow - ProcessUptime.StartTime
        };
        return Ok(Envelope(response));
    }

    /// <summary>
    /// Liveness — the process is alive. Always 200 if process responds.
    /// </summary>
    [HttpGet("live")]
    public IActionResult GetLiveness()
    {
        AddHealthHeaders();
        var response = new LivenessResponse
        {
            Status = "alive",
            TimestampUtc = DateTime.UtcNow
        };
        return Ok(Envelope(response));
    }

    /// <summary>
    /// Readiness — required checks only (database, migrations, config).
    /// Returns 200 healthy / 503 unhealthy.
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness(CancellationToken ct)
    {
        AddHealthHeaders();
        var health = await healthService.RunReadinessAsync(ct);
        AddVersionHeaders(health.Version);
        var statusCode = health.Status == HealthStatuses.Unhealthy
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK;
        return StatusCode(statusCode, Envelope(health));
    }

    /// <summary>
    /// Full check — all components with per-check durations.
    /// Returns 200 healthy/degraded / 503 unhealthy.
    /// </summary>
    [HttpGet("full")]
    public async Task<IActionResult> GetFull(CancellationToken ct)
    {
        AddHealthHeaders();
        var health = await healthService.RunFullAsync(ct);
        AddVersionHeaders(health.Version);
        var statusCode = health.Status == HealthStatuses.Unhealthy
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK;
        return StatusCode(statusCode, Envelope(health));
    }

    /// <summary>
    /// Static asset fingerprint manifest. Always 200.
    /// </summary>
    [HttpGet("assets")]
    public IActionResult GetAssets()
    {
        AddHealthHeaders();
        return Ok(Envelope(assetsService.GetManifest()));
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static Models.Common.ApiResponse<T> Envelope<T>(T data) =>
        new(data, new Models.Common.ResponseMeta(DateTime.UtcNow, Guid.NewGuid().ToString()));

    private void AddHealthHeaders()
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
        Response.Headers["X-TaskPilot-Version"] = BuildInfo.Version;
        Response.Headers["X-TaskPilot-Commit"] = BuildInfo.GitCommitShort;
    }

    private void AddVersionHeaders(VersionResponse version)
    {
        Response.Headers["X-TaskPilot-Version"] = version.Version;
        Response.Headers["X-TaskPilot-Commit"] = version.GitCommitShort;
    }
}
