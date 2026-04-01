using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.TaskTypes;

namespace TaskPilot.Controllers;

[Authorize]
[Route("api/v1/task-types")]
public class TaskTypeController(ITaskTypeService taskTypeService) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetTaskTypes(CancellationToken cancellationToken)
    {
        var types = await taskTypeService.GetAllActiveAsync(cancellationToken);
        return Ok(Envelope(types));
    }
}
