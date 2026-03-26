using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Server.Constants;
using TaskPilot.Shared.DTOs.Common;

namespace TaskPilot.Server.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found in claims.");

    protected string ModifiedBy
    {
        get
        {
            var authMethod = User.FindFirstValue(ClaimTypes.AuthenticationMethod);
            if (authMethod == AuthConstants.ApiKeyScheme)
            {
                var keyName = User.FindFirstValue(AuthConstants.ApiKeyClaimType) ?? "unknown";
                return $"api:{keyName}";
            }
            var userName = User.Identity?.Name ?? "unknown";
            return $"user:{userName}";
        }
    }

    protected static ApiResponse<T> Envelope<T>(T data, string? requestId = null) =>
        new(data, new ResponseMeta(DateTime.UtcNow, requestId ?? Guid.NewGuid().ToString()));

    protected static ErrorResponse NotFoundError(string resource) =>
        new(new ApiError(ErrorCodes.NotFound, $"{resource} not found."));

    protected static ErrorResponse ValidationError(string message, IReadOnlyList<FieldError>? details = null) =>
        new(new ApiError(ErrorCodes.ValidationError, message, details));

    protected static ErrorResponse ConflictError(string message) =>
        new(new ApiError(ErrorCodes.Conflict, message));
}
