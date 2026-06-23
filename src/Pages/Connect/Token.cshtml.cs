using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace TaskPilot.Pages.Connect;

/// <summary>
/// Handles POST /connect/token (token exchange passthrough).
/// OpenIddict emits this endpoint via UseAspNetCore().EnableTokenEndpointPassthrough().
///
/// <see cref="IgnoreAntiforgeryToken"/> is required: this is a back-channel OAuth endpoint
/// called server-to-server by the OAuth client (e.g. ChatGPT) with no browser session and
/// no antiforgery token. Razor Pages validate antiforgery on POST by default, which would
/// otherwise reject every token exchange with a 400. (The /connect/authorize consent POST
/// keeps antiforgery on purpose — it is a real browser form.)
/// </summary>
[IgnoreAntiforgeryToken]
public class TokenModel(UserManager<IdentityUser> userManager) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict server request is not available.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            // Retrieve the claims principal saved in the authorization code / refresh token.
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var principal = result.Principal
                ?? throw new InvalidOperationException("The token claims could not be retrieved.");

            // Verify the user still exists and is not locked out.
            var userId = principal.GetClaim(OpenIddictConstants.Claims.Subject)
                ?? throw new InvalidOperationException("Subject claim missing.");

            var user = await userManager.FindByIdAsync(userId);
            if (user is null || await userManager.IsLockedOutAsync(user))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                    }));
            }

            // Re-add NameIdentifier so MCP tools can resolve UserId identically to the API-key path.
            var identity = (ClaimsIdentity)principal.Identity!;
            if (identity.FindFirst(ClaimTypes.NameIdentifier) is null)
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));

            principal.SetDestinations(GetDestinations);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new { error = "unsupported_grant_type" });
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
