using OpenIddict.Validation.AspNetCore;

namespace TaskPilot.Constants;

public static class AuthConstants
{
    public const string ApiKeyScheme = "ApiKey";
    public const string ApiKeyHeader = "X-Api-Key";
    public const string CookieScheme = "Identity.Application";

    /// <summary>OpenIddict in-process Bearer validation scheme.</summary>
    public const string BearerScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;

    public const string ApiKeyClaimType = "api_key_name";
    public const string UserIdClaimType = "sub";

    // ── OAuth 2.1 / MCP ─────────────────────────────────────────────────────
    /// <summary>Single scope granted to OAuth clients connecting via /mcp.</summary>
    public const string McpScope = "mcp";

    /// <summary>
    /// Resource indicator (audience) for /mcp tokens.
    /// Tokens issued to OAuth clients carry this as their aud claim.
    /// </summary>
    public const string McpResourceIndicator = "https://taskpilot.azurewebsites.net/mcp";

    /// <summary>Config key: app public base URL (drives OAuth issuer + resource metadata).</summary>
    public const string OAuthBaseUrlConfigKey = "OAuth:BaseUrl";

    /// <summary>Config key: kill-switch — set false to disable OAuth AS entirely.</summary>
    public const string OAuthEnabledConfigKey = "OAuth:Enabled";
}
