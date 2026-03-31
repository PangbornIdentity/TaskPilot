using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.Tasks;

namespace TaskPilot.Controllers;

[Authorize]
public class TasksController(ITaskService taskService, IValidator<CreateTaskRequest> createValidator, IValidator<UpdateTaskRequest> updateValidator) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetTasks([FromQuery] TaskQueryParams queryParams, CancellationToken cancellationToken)
    {
        var result = await taskService.GetTasksAsync(queryParams, UserId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTask(Guid id, CancellationToken cancellationToken)
    {
        var task = await taskService.GetTaskByIdAsync(id, UserId, cancellationToken);
        if (task is null) return NotFound(NotFoundError("Task"));
        return Ok(Envelope(task));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromServices] IStatsService statsService, CancellationToken cancellationToken)
    {
        var stats = await statsService.GetTaskStatsAsync(UserId, cancellationToken);
        return Ok(Envelope(stats));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var details = validation.Errors.Select(e => new Models.Common.FieldError(e.PropertyName, e.ErrorMessage)).ToList();
            return BadRequest(ValidationError("Validation failed.", details));
        }

        var task = await taskService.CreateTaskAsync(request, UserId, ModifiedBy, cancellationToken);
        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, Envelope(task));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var details = validation.Errors.Select(e => new Models.Common.FieldError(e.PropertyName, e.ErrorMessage)).ToList();
            return BadRequest(ValidationError("Validation failed.", details));
        }

        var task = await taskService.UpdateTaskAsync(id, request, UserId, ModifiedBy, cancellationToken);
        if (task is null) return NotFound(NotFoundError("Task"));
        return Ok(Envelope(task));
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> PatchTask(Guid id, [FromBody] PatchTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await taskService.PatchTaskAsync(id, request, UserId, ModifiedBy, cancellationToken);
        if (task is null) return NotFound(NotFoundError("Task"));
        return Ok(Envelope(task));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> CompleteTask(Guid id, [FromBody] CompleteTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await taskService.CompleteTaskAsync(id, request, UserId, ModifiedBy, cancellationToken);
        if (task is null) return NotFound(NotFoundError("Task"));
        return Ok(Envelope(task));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await taskService.DeleteTaskAsync(id, UserId, ModifiedBy, cancellationToken);
        if (!deleted) return NotFound(NotFoundError("Task"));
        return NoContent();
    }
}
