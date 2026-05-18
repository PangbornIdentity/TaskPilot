using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using TaskPilot.Constants;
using TaskPilot.Services.Interfaces;

namespace TaskPilot.Extensions;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions { }

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyService apiKeyService)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AuthConstants.ApiKeyHeader, out var apiKeyValues))
            return AuthenticateResult.NoResult();

        var plainTextKey = apiKeyValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(plainTextKey))
            return AuthenticateResult.Fail("API key is empty.");

        var (isValid, keyName, userId, apiKeyId) = await apiKeyService.ValidateKeyAsync(plainTextKey);

        if (!isValid || keyName is null || userId is null || apiKeyId is null)
            return AuthenticateResult.Fail("Invalid or inactive API key.");

        // Stash the resolved ApiKey row id so ApiAuditMiddleware can write an
        // audit row with a real FK. Without this, the middleware logs a warning
        // and silently drops the audit entry.
        Context.Items["ApiKeyId"] = apiKeyId.Value;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(AuthConstants.ApiKeyClaimType, keyName),
            new Claim(ClaimTypes.AuthenticationMethod, AuthConstants.ApiKeyScheme)
        };

        var identity = new ClaimsIdentity(claims, AuthConstants.ApiKeyScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthConstants.ApiKeyScheme);

        return AuthenticateResult.Success(ticket);
    }
}
