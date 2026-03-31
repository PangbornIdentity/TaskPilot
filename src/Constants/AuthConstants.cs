namespace TaskPilot.Constants;

public static class AuthConstants
{
    public const string ApiKeyScheme = "ApiKey";
    public const string ApiKeyHeader = "X-Api-Key";
    public const string CookieScheme = "Identity.Application";

    public const string ApiKeyClaimType = "api_key_name";
    public const string UserIdClaimType = "sub";
}
