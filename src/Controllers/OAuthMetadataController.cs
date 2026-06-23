using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.Constants;

namespace TaskPilot.Controllers;

/// <summary>
/// Emits RFC 9728 Protected Resource Metadata at GET /.well-known/oauth-protected-resource.
/// This endpoint is anonymous and lets MCP clients (e.g. ChatGPT) discover the authorization
/// server and supported scopes before initiating OAuth 2.1 + PKCE.
/// Note: the AS discovery document (RFC 8414) is emitted automatically by OpenIddict at
/// GET /.well-known/oauth-authorization-server.
/// </summary>
[AllowAnonymous]
[ApiController]
public class OAuthMetadataController(IConfiguration configuration) : ControllerBase
{
    [HttpGet("/.well-known/oauth-protected-resource")]
    public IActionResult GetProtectedResourceMetadata()
    {
        var baseUrl = GetBaseUrl();
        var resource = $"{baseUrl.TrimEnd('/')}/mcp";
        var issuer = baseUrl.TrimEnd('/');

        var metadata = new
        {
            resource,
            authorization_servers = new[] { issuer },
            scopes_supported = new[] { AuthConstants.McpScope },
            bearer_methods_supported = new[] { "header" }
        };

        return Ok(metadata);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string GetBaseUrl()
    {
        var configured = configuration[AuthConstants.OAuthBaseUrlConfigKey];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        // Derive from the current request when config is absent (dev convenience).
        var req = HttpContext.Request;
        return $"{req.Scheme}://{req.Host}";
    }
}
