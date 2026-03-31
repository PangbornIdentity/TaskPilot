using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TaskPilot.Pages.Auth;

[AllowAnonymous]
public class RegisterModel(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager) : PageModel
{
    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = new IdentityUser { UserName = Email, Email = Email };
        var result = await userManager.CreateAsync(user, Password);

        if (result.Succeeded)
        {
            await signInManager.SignInAsync(user, isPersistent: true);
            return Redirect("/");
        }

        ErrorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
        return Page();
    }
}
