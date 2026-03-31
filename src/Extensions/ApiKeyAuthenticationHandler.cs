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

        var (isValid, keyName, userId) = await apiKeyService.ValidateKeyAsync(plainTextKey);

        if (!isValid || keyName is null || userId is null)
            return AuthenticateResult.Fail("Invalid or inactive API key.");

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
