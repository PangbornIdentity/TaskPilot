using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.Tags;

namespace TaskPilot.Controllers;

[Authorize]
public class TagsController(
    ITagService tagService,
    IValidator<CreateTagRequest> createValidator,
    IValidator<UpdateTagRequest> updateValidator) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetTags(CancellationToken cancellationToken)
    {
        var tags = await tagService.GetAllTagsAsync(UserId, cancellationToken);
        return Ok(Envelope(tags));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var details = validation.Errors.Select(e => new Models.Common.FieldError(e.PropertyName, e.ErrorMessage)).ToList();
            return BadRequest(ValidationError("Validation failed.", details));
        }

        try
        {
            var tag = await tagService.CreateTagAsync(request, UserId, ModifiedBy, cancellationToken);
            return Created(string.Empty, Envelope(tag));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ConflictError(ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTag(Guid id, [FromBody] UpdateTagRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var details = validation.Errors.Select(e => new Models.Common.FieldError(e.PropertyName, e.ErrorMessage)).ToList();
            return BadRequest(ValidationError("Validation failed.", details));
        }

        try
        {
            var tag = await tagService.UpdateTagAsync(id, request, UserId, ModifiedBy, cancellationToken);
            if (tag is null) return NotFound(NotFoundError("Tag"));
            return Ok(Envelope(tag));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ConflictError(ex.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTag(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await tagService.DeleteTagAsync(id, UserId, cancellationToken);
        if (!deleted) return NotFound(NotFoundError("Tag"));
        return NoContent();
    }
}
