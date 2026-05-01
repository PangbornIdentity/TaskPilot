using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.ApiKeys;
using TaskPilot.Models.Tags;

namespace TaskPilot.Pages.Settings;

public class SettingsIndexModel(
    IApiKeyService apiKeyService,
    ITagService tagService,
    UserManager<IdentityUser> userManager,
    IWebHostEnvironment env) : PageModel
{
    public List<ApiKeyResponse> ApiKeys { get; private set; } = [];
    public CreateApiKeyResponse? NewKey { get; private set; }
    public string? PasswordError { get; private set; }
    public List<TagResponse> Tags { get; private set; } = [];
    public bool IsDevelopment { get; private set; }
    public Guid? EditingTagId { get; private set; }
    public string? TagEditError { get; private set; }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string ModifiedBy => $"user:{User.Identity?.Name}";

    public async Task OnGetAsync(Guid? editTagId = null)
    {
        IsDevelopment = env.IsDevelopment();
        ApiKeys = (await apiKeyService.GetAllKeysAsync(UserId)).ToList();
        Tags = (await tagService.GetAllTagsAsync(UserId)).ToList();
        EditingTagId = editTagId.HasValue && Tags.Any(t => t.Id == editTagId.Value)
            ? editTagId
            : null;
    }

    public async Task<IActionResult> OnPostGenerateAsync(string keyName)
    {
        if (string.IsNullOrWhiteSpace(keyName)) return RedirectToPage();

        var created = await apiKeyService.GenerateKeyAsync(
            new CreateApiKeyRequest(keyName.Trim()), UserId, ModifiedBy);

        ApiKeys = (await apiKeyService.GetAllKeysAsync(UserId)).ToList();
        Tags = (await tagService.GetAllTagsAsync(UserId)).ToList();
        NewKey = created;
        TempData["Toast"] = "API key generated — copy it now, it won't be shown again.";
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
        Tags = (await tagService.GetAllTagsAsync(UserId)).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateTagAsync(string tagName, string tagColor)
    {
        if (string.IsNullOrWhiteSpace(tagName)) return RedirectToPage();
        if (string.IsNullOrWhiteSpace(tagColor)) tagColor = "#64748B";

        try
        {
            await tagService.CreateTagAsync(new CreateTagRequest(tagName.Trim(), tagColor), UserId, ModifiedBy);
            TempData["Toast"] = $"Tag \"{tagName.Trim()}\" created.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteTagAsync(Guid tagId)
    {
        await tagService.DeleteTagAsync(tagId, UserId);
        TempData["Toast"] = "Tag deleted.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateTagAsync(Guid tagId, string tagName, string tagColor)
    {
        if (string.IsNullOrWhiteSpace(tagColor)) tagColor = "#64748B";

        if (string.IsNullOrWhiteSpace(tagName))
        {
            return await RenderPageWithEditErrorAsync(tagId, "Tag name is required.");
        }

        try
        {
            var updated = await tagService.UpdateTagAsync(tagId,
                new UpdateTagRequest(tagName.Trim(), tagColor), UserId, ModifiedBy);

            if (updated is null)
            {
                TempData["Error"] = "Tag not found.";
                return RedirectToPage();
            }

            TempData["Toast"] = "Tag updated.";
            return RedirectToPage();
        }
        catch (InvalidOperationException ex)
        {
            return await RenderPageWithEditErrorAsync(tagId, ex.Message);
        }
    }

    // Validation/conflict on tag edit re-renders the page directly instead of doing
    // a PRG redirect with TempData. Keeps the edit row open with the failing input
    // and inline error visible in a single response, no cross-request state.
    private async Task<IActionResult> RenderPageWithEditErrorAsync(Guid tagId, string error)
    {
        IsDevelopment = env.IsDevelopment();
        ApiKeys = (await apiKeyService.GetAllKeysAsync(UserId)).ToList();
        Tags = (await tagService.GetAllTagsAsync(UserId)).ToList();
        EditingTagId = Tags.Any(t => t.Id == tagId) ? tagId : null;
        TagEditError = error;
        return Page();
    }
}
