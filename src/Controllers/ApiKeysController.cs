using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.ApiKeys;

namespace TaskPilot.Controllers;

[Authorize]
public class ApiKeysController(IApiKeyService apiKeyService, IValidator<CreateApiKeyRequest> validator) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetKeys(CancellationToken cancellationToken)
    {
        var keys = await apiKeyService.GetAllKeysAsync(UserId, cancellationToken);
        return Ok(Envelope(keys));
    }

    [HttpPost]
    public async Task<IActionResult> GenerateKey([FromBody] CreateApiKeyRequest request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var details = validation.Errors.Select(e => new Models.Common.FieldError(e.PropertyName, e.ErrorMessage)).ToList();
            return BadRequest(ValidationError("Validation failed.", details));
        }

        var result = await apiKeyService.GenerateKeyAsync(request, UserId, ModifiedBy, cancellationToken);
        return Created(string.Empty, Envelope(result));
    }

    [HttpPatch("{id:guid}/rename")]
    public async Task<IActionResult> RenameKey(Guid id, [FromBody] RenameApiKeyRequest request, CancellationToken cancellationToken)
    {
        var updated = await apiKeyService.RenameKeyAsync(id, request, UserId, ModifiedBy, cancellationToken);
        if (!updated) return NotFound(NotFoundError("API key"));
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateKey(Guid id, CancellationToken cancellationToken)
    {
        var updated = await apiKeyService.SetActiveStateAsync(id, true, UserId, ModifiedBy, cancellationToken);
        if (!updated) return NotFound(NotFoundError("API key"));
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateKey(Guid id, CancellationToken cancellationToken)
    {
        var updated = await apiKeyService.SetActiveStateAsync(id, false, UserId, ModifiedBy, cancellationToken);
        if (!updated) return NotFound(NotFoundError("API key"));
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokeKey(Guid id, CancellationToken cancellationToken)
    {
        var revoked = await apiKeyService.RevokeKeyAsync(id, UserId, cancellationToken);
        if (!revoked) return NotFound(NotFoundError("API key"));
        return NoContent();
    }
}
