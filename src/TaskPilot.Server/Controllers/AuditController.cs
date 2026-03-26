using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Server.Services.Interfaces;
using TaskPilot.Shared.DTOs.Audit;

namespace TaskPilot.Server.Controllers;

[Authorize]
public class AuditController(IAuditService auditService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditQueryParams queryParams, CancellationToken cancellationToken)
    {
        var result = await auditService.GetAuditLogsAsync(queryParams, UserId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await auditService.GetSummaryAsync(UserId, cancellationToken);
        return Ok(Envelope(summary));
    }
}
