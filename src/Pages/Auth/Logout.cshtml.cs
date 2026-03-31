using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TaskPilot.Pages.Auth;

public class LogoutModel(SignInManager<IdentityUser> signInManager) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await signInManager.SignOutAsync();
        return Redirect("/auth/login");
    }

    public IActionResult OnGet() => Redirect("/");
}
