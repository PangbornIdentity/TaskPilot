using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using TaskPilot.Constants;

namespace TaskPilot.Controllers;

/// <summary>
/// Implements RFC 7591 Dynamic Client Registration at POST /connect/register.
/// Restricted to public clients using authorization_code + PKCE, with redirect URIs
/// matching ChatGPT's connector callback pattern or localhost (for dev/test).
/// </summary>
[AllowAnonymous]
[ApiController]
public class DcrController(
    IOpenIddictApplicationManager applicationManager,
    ILogger<DcrController> logger) : ControllerBase
{
    // ChatGPT connector callback host pattern and localhost for dev
    private static readonly string[] AllowedRedirectHostPatterns =
    [
        "chatgpt.com",
        "chat.openai.com",
        "localhost",
        "127.0.0.1"
    ];

    [HttpPost("/connect/register")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> RegisterClientAsync(
        [FromBody] DcrRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { error = "invalid_client_metadata", error_description = "Request body is required." });

        // Only authorization_code grant is permitted
        var grantTypes = request.GrantTypes ?? ["authorization_code"];
        if (!grantTypes.Contains("authorization_code") || grantTypes.Count > 2 ||
            grantTypes.Any(g => g is not "authorization_code" and not "refresh_token"))
        {
            return BadRequest(new
            {
                error = "invalid_client_metadata",
                error_description = "Only authorization_code (and optionally refresh_token) grant types are permitted."
            });
        }

        // Only mcp scope
        var scope = request.Scope ?? AuthConstants.McpScope;
        if (!scope.Split(' ').All(s => s == AuthConstants.McpScope))
        {
            return BadRequest(new
            {
                error = "invalid_client_metadata",
                error_description = $"Only the '{AuthConstants.McpScope}' scope is permitted."
            });
        }

        // Validate redirect URIs
        var redirectUris = request.RedirectUris ?? [];
        if (redirectUris.Count == 0)
            return BadRequest(new { error = "invalid_client_metadata", error_description = "At least one redirect_uri is required." });

        foreach (var uri in redirectUris)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) ||
                !IsAllowedRedirectHost(parsed.Host))
            {
                logger.LogWarning("DCR rejected redirect_uri '{Uri}' — host not in allowed list.", uri);
                return BadRequest(new
                {
                    error = "invalid_redirect_uri",
                    error_description = $"Redirect URI '{uri}' is not permitted. Only ChatGPT connector and localhost hosts are allowed."
                });
            }
        }

        // Generate a client_id (public client — no secret stored)
        var clientId = Guid.NewGuid().ToString("N");
        var displayName = request.ClientName ?? "MCP Client";

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            DisplayName = displayName,
            ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                $"{OpenIddictConstants.Permissions.Prefixes.Scope}{AuthConstants.McpScope}",
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        };

        foreach (var uri in redirectUris)
            descriptor.RedirectUris.Add(new Uri(uri));

        await applicationManager.CreateAsync(descriptor, cancellationToken);
        logger.LogInformation("DCR registered new client '{ClientId}' ('{DisplayName}').", clientId, displayName);

        return StatusCode(StatusCodes.Status201Created, new
        {
            client_id = clientId,
            client_id_issued_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            client_name = displayName,
            redirect_uris = redirectUris,
            grant_types = grantTypes,
            response_types = new[] { "code" },
            scope = AuthConstants.McpScope,
            token_endpoint_auth_method = "none"
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool IsAllowedRedirectHost(string host) =>
        AllowedRedirectHostPatterns.Any(p => host.Equals(p, StringComparison.OrdinalIgnoreCase) ||
                                             host.EndsWith($".{p}", StringComparison.OrdinalIgnoreCase));
}

/// <summary>RFC 7591 registration request payload.</summary>
public sealed record DcrRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("redirect_uris")]
    public List<string>? RedirectUris { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("grant_types")]
    public List<string>? GrantTypes { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("client_name")]
    public string? ClientName { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("scope")]
    public string? Scope { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; init; }
}
