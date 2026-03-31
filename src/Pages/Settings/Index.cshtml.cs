using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.ApiKeys;

namespace TaskPilot.Pages.Settings;

public class SettingsIndexModel(
    IApiKeyService apiKeyService,
    UserManager<IdentityUser> userManager) : PageModel
{
    public List<ApiKeyResponse> ApiKeys { get; private set; } = [];
    public CreateApiKeyResponse? NewKey { get; private set; }
    public string? PasswordError { get; private set; }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string ModifiedBy => $"user:{User.Identity?.Name}";

    public async Task OnGetAsync()
    {
        ApiKeys = (await apiKeyService.GetAllKeysAsync(UserId)).ToList();
    }

    public async Task<IActionResult> OnPostGenerateAsync(string keyName)
    {
        if (string.IsNullOrWhiteSpace(keyName)) return RedirectToPage();

        var created = await apiKeyService.GenerateKeyAsync(
            new CreateApiKeyRequest(keyName.Trim()), UserId, ModifiedBy);

        ApiKeys = (await apiKeyService.GetAllKeysAsync(UserId)).ToList();
        NewKey = created;
        TempData["Toast"] = "API key generated — copy it now, it won't be shown again.";
        // Don't redirect — stay on page to show the key
        return Page();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid keyId)
    {
        await apiKeyService.SetActiveStateAsync(keyId, false, UserId, ModifiedBy);
        TempData["Toast"] = "API key deactivated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid keyId)
    {
        await apiKeyService.SetActiveStateAsync(keyId, true, UserId, ModifiedBy);
        TempData["Toast"] = "API key activated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteKeyAsync(Guid keyId)
    {
        await apiKeyService.RevokeKeyAsync(keyId, UserId);
        TempData["Toast"] = "API key revoked.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync(string currentPassword, string newPassword)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return RedirectToPage();

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            TempData["Toast"] = "Password updated successfully.";
            return RedirectToPage();
        }

        PasswordError = string.Join(" ", result.Errors.Select(e => e.Description));
        ApiKeys = (await apiKeyService.GetAllKeysAsync(UserId)).ToList();
        return Page();
    }
}
