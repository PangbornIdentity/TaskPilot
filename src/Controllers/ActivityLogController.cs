using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Models.Audit;
using TaskPilot.Services.Interfaces;

namespace TaskPilot.Controllers;

[Authorize]
[Route("api/v1/activity-logs")]
public class ActivityLogController(IActivityLogService activityLogService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetActivityLogs(
        [FromQuery] ActivityLogQueryParams queryParams,
        CancellationToken cancellationToken)
    {
        var result = await activityLogService.GetPagedAsync(queryParams, UserId, cancellationToken);
        return Ok(result);
    }
}
