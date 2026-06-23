using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using TaskPilot.Constants;

namespace TaskPilot.Pages.Connect;

/// <summary>
/// Handles GET /connect/authorize (show consent UI) and POST /connect/authorize (apply consent decision).
/// Uses cookie authentication: if the user is not logged in they are redirected to /auth/login first.
/// </summary>
[Authorize(AuthenticationSchemes = AuthConstants.CookieScheme)]
public class AuthorizeModel(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictScopeManager scopeManager,
    UserManager<IdentityUser> userManager) : PageModel
{
    public string ClientName { get; private set; } = string.Empty;

    /// <summary>
    /// All OAuth 2.1 query parameters received on the GET request.
    /// Round-tripped as hidden fields in the consent form so OpenIddict can
    /// read them from the POST body on consent submit.
    /// </summary>
    public IReadOnlyDictionary<string, string> OAuthParameters { get; private set; } =
        new Dictionary<string, string>();

    public async Task<IActionResult> OnGetAsync()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict server request is not available.");

        var application = await applicationManager.FindByClientIdAsync(request.ClientId!)
            ?? throw new InvalidOperationException("The application cannot be found.");

        ClientName = await applicationManager.GetDisplayNameAsync(application) ?? request.ClientId!;

        // Capture all OAuth query-string parameters so the view can round-trip
        // them as hidden fields in the POST body (OpenIddict reads from body on POST).
        OAuthParameters = HttpContext.Request.Query
            .Where(kvp => kvp.Value.Count > 0 && !string.IsNullOrEmpty(kvp.Value.ToString()))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

        // Check if the user has already consented (persistent authorization).
        var user = await userManager.GetUserAsync(User)
            ?? throw new InvalidOperationException("The user details cannot be retrieved.");

        var authorizations = await authorizationManager.FindAsync(
            subject: await userManager.GetUserIdAsync(user),
            client: await applicationManager.GetIdAsync(application) ?? string.Empty,
            status: OpenIddictConstants.Statuses.Valid,
            type: OpenIddictConstants.AuthorizationTypes.Permanent,
            scopes: request.GetScopes()).ToListAsync();

        if (authorizations.Count > 0)
        {
            // Already consented — auto-approve without showing the form.
            return await AcceptAsync(user, application, authorizations.Last(), request);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync([FromForm] string consent)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict server request is not available.");

        if (consent == "denied")
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.AccessDenied,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user denied access."
                }));
        }

        var user = await userManager.GetUserAsync(User)
            ?? throw new InvalidOperationException("The user details cannot be retrieved.");

        var application = await applicationManager.FindByClientIdAsync(request.ClientId!)
            ?? throw new InvalidOperationException("The application cannot be found.");

        // Create a permanent authorization so subsequent connects skip the consent screen.
        var authorization = await authorizationManager.CreateAsync(
            principal: await BuildPrincipalAsync(user, request),
            subject: await userManager.GetUserIdAsync(user),
            client: await applicationManager.GetIdAsync(application) ?? string.Empty,
            type: OpenIddictConstants.AuthorizationTypes.Permanent,
            scopes: request.GetScopes());

        return await AcceptAsync(user, application, authorization, request);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<IActionResult> AcceptAsync(
        IdentityUser user,
        object application,
        object authorization,
        OpenIddictRequest request)
    {
        var principal = await BuildPrincipalAsync(user, request);
        principal.SetAuthorizationId(await authorizationManager.GetIdAsync(authorization));
        principal.SetDestinations(GetDestinations);

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<ClaimsPrincipal> BuildPrincipalAsync(IdentityUser user, OpenIddictRequest request)
    {
        // Build identity with sub = IdentityUser.Id (data-isolation boundary).
        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        identity.SetClaim(OpenIddictConstants.Claims.Subject, await userManager.GetUserIdAsync(user));
        identity.SetClaim(OpenIddictConstants.Claims.Name, await userManager.GetUserNameAsync(user) ?? string.Empty);
        identity.SetClaim(ClaimTypes.NameIdentifier, await userManager.GetUserIdAsync(user));

        var scopes = request.GetScopes();
        identity.SetScopes(scopes);
        identity.SetResources(await scopeManager.ListResourcesAsync(scopes).ToListAsync());

        return new ClaimsPrincipal(identity);
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        return claim.Type switch
        {
            OpenIddictConstants.Claims.Subject or
            OpenIddictConstants.Claims.Name or
            ClaimTypes.NameIdentifier =>
                [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken],
            _ => [OpenIddictConstants.Destinations.AccessToken]
        };
    }
}
