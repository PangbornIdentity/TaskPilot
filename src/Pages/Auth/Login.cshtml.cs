using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TaskPilot.Pages.Auth;

[AllowAnonymous]
public class LoginModel(SignInManager<IdentityUser> signInManager) : PageModel
{
    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid) return Page();

        var result = await signInManager.PasswordSignInAsync(Email, Password, isPersistent: true, lockoutOnFailure: false);

        if (result.Succeeded)
            return LocalRedirect(returnUrl ?? "/");

        ErrorMessage = "Invalid email or password.";
        return Page();
    }
}
