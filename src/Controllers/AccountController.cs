using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Models.Common;

namespace TaskPilot.Controllers;

public class AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager) : BaseApiController
{
    public record LoginRequest(string Email, string Password);
    public record RegisterRequest(string Email, string Password);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = new IdentityUser { UserName = request.Email, Email = request.Email };
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var details = result.Errors.Select(e => new FieldError(e.Code, e.Description)).ToList();
            return BadRequest(ValidationError("Registration failed.", details));
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return Ok(Envelope(new { user.Id, user.Email }));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await signInManager.PasswordSignInAsync(request.Email, request.Password, isPersistent: true, lockoutOnFailure: false);

        if (!result.Succeeded)
            return Unauthorized(new ErrorResponse(new ApiError(Constants.ErrorCodes.Unauthorized, "Invalid credentials.")));

        var user = await userManager.FindByEmailAsync(request.Email);
        return Ok(Envelope(new { user!.Id, user.Email }));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(UserId);
        if (user is null) return NotFound(NotFoundError("User"));

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var details = result.Errors.Select(e => new FieldError(e.Code, e.Description)).ToList();
            return BadRequest(ValidationError("Password change failed.", details));
        }

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(UserId);
        if (user is null) return NotFound(NotFoundError("User"));
        return Ok(Envelope(new { user.Id, user.Email }));
    }
}
