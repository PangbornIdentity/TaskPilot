using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskPilot.Constants;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.OAuth;

/// <summary>
/// Integration tests for the v1.16.0 OAuth 2.1 / MCP Bearer feature.
/// Covers: discovery endpoints, DCR, end-to-end PKCE flow, Bearer auth on /mcp,
/// data isolation, LastModifiedBy attribution, and regression of X-Api-Key on /mcp.
///
/// Uses a dedicated <see cref="OAuthWebAppFactory"/> that:
///  - Adds UseOpenIddict() to the patched DbContext so the four OpenIddict tables
///    are created by EnsureCreatedAsync.
///  - Post-configures OpenIddictServerOptions to disable the HTTPS-only restriction
///    (ID2083) so the http://localhost test server can serve OAuth endpoints.
/// </summary>
[Collection("OAuthIntegration")]
public class McpOAuthTests : IClassFixture<OAuthWebAppFactory>
{
    private readonly OAuthWebAppFactory _factory;

    public McpOAuthTests(OAuthWebAppFactory factory)
    {
        _factory = factory;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MCP SSE transport helpers
    //
    // MCP Streamable HTTP requires two things:
    //  1. Accept: application/json, text/event-stream  (else → 406)
    //  2. After initialize, subsequent calls need Mcp-Session-Id from the
    //     initialize response header (else → 400).
    //
    // McpSession encapsulates a connected session (one per auth credential).
    // ═══════════════════════════════════════════════════════════════════════════

    private const string McpAccept = "application/json, text/event-stream";
    private const string McpSessionHeader = "Mcp-Session-Id";

    /// <summary>
    /// A lightweight MCP session handle: stores the auth token/key and the
    /// session ID returned by the server after initialize.
    /// </summary>
    private sealed class McpSession
    {
        private readonly OAuthWebAppFactory _factory;
        private readonly string? _authorizationHeader;
        private readonly string? _apiKey;
        private string? _sessionId;
        private int _nextId = 1;

        public McpSession(OAuthWebAppFactory factory, string? authorizationHeader = null, string? apiKey = null)
        {
            _factory = factory;
            _authorizationHeader = authorizationHeader;
            _apiKey = apiKey;
        }

        public async Task<HttpResponseMessage> InitializeAsync()
        {
            var resp = await SendAsync("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test-client","version":"1.0"}}}""");

            // Capture Mcp-Session-Id for all subsequent calls
            if (resp.Headers.TryGetValues(McpSessionHeader, out var values))
                _sessionId = values.FirstOrDefault();

            return resp;
        }

        public Task<HttpResponseMessage> ToolsListAsync() =>
            SendAsync("""{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}""");

        public Task<HttpResponseMessage> CreateTaskAsync(string title)
        {
            var json = "{\"jsonrpc\":\"2.0\",\"id\":" + (++_nextId) + ",\"method\":\"tools/call\",\"params\":{\"name\":\"create_task\",\"arguments\":{\"title\":\"" + title + "\",\"taskTypeId\":1,\"area\":\"Personal\",\"priority\":\"Medium\",\"status\":\"NotStarted\",\"targetDateType\":\"ThisWeek\"}}}";
            return SendAsync(json);
        }

        public Task<HttpResponseMessage> ListTasksAsync() =>
            SendAsync("{\"jsonrpc\":\"2.0\",\"id\":" + (++_nextId) + ",\"method\":\"tools/call\",\"params\":{\"name\":\"list_tasks\",\"arguments\":{}}}");

        public Task<HttpResponseMessage> GetStatsAsync() =>
            SendAsync("{\"jsonrpc\":\"2.0\",\"id\":" + (++_nextId) + ",\"method\":\"tools/call\",\"params\":{\"name\":\"get_stats\",\"arguments\":{}}}");

        private async Task<HttpResponseMessage> SendAsync(string jsonRpc)
        {
            // Create a fresh HttpClient per request (factory is stateless in WebApplicationFactory)
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

            var request = new HttpRequestMessage(HttpMethod.Post, "/mcp");
            request.Content = new StringContent(jsonRpc, Encoding.UTF8, "application/json");
            request.Headers.TryAddWithoutValidation("Accept", McpAccept);

            if (_authorizationHeader is not null)
                request.Headers.TryAddWithoutValidation("Authorization", _authorizationHeader);

            if (_apiKey is not null)
                request.Headers.TryAddWithoutValidation(AuthConstants.ApiKeyHeader, _apiKey);

            if (_sessionId is not null)
                request.Headers.TryAddWithoutValidation(McpSessionHeader, _sessionId);

            return await client.SendAsync(request);
        }
    }

    /// <summary>Create a Bearer-authenticated MCP session and initialize it.</summary>
    private async Task<McpSession> CreateBearerMcpSessionAsync(string accessToken)
    {
        var session = new McpSession(_factory, authorizationHeader: $"Bearer {accessToken}");
        var initResp = await session.InitializeAsync();
        Assert.Equal(HttpStatusCode.OK, initResp.StatusCode);
        return session;
    }

    /// <summary>Create an API-key-authenticated MCP session and initialize it.</summary>
    private async Task<McpSession> CreateApiKeyMcpSessionAsync(string apiKey)
    {
        var session = new McpSession(_factory, apiKey: apiKey);
        var initResp = await session.InitializeAsync();
        Assert.Equal(HttpStatusCode.OK, initResp.StatusCode);
        return session;
    }

    /// <summary>Anonymous (no auth) MCP session — used for 401 tests.</summary>
    private McpSession CreateAnonymousMcpSession() => new McpSession(_factory);

    /// <summary>
    /// Send a single raw POST to /mcp with the Accept header but without a session.
    /// Used only for auth-rejection tests where the server returns 401 before
    /// the MCP protocol handles the request.
    /// </summary>
    private async Task<HttpResponseMessage> RawMcpPostAsync(string jsonRpc, string? authorizationHeader = null)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        request.Content = new StringContent(jsonRpc, Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation("Accept", McpAccept);
        if (authorizationHeader is not null)
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
        return await client.SendAsync(request);
    }

    private static string McpToolsListJson() =>
        """{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}""";

    // ═══════════════════════════════════════════════════════════════════════════
    // PKCE helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static (string Verifier, string Challenge) GeneratePkce()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var verifier = Base64UrlEncode(bytes);
        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        var challenge = Base64UrlEncode(challengeBytes);
        return (verifier, challenge);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // ═══════════════════════════════════════════════════════════════════════════
    // Full PKCE authorization_code flow helper
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Performs the complete authorization_code + PKCE S256 flow programmatically:
    ///  1. DCR at POST /connect/register (localhost redirect).
    ///  2. Register a new user and obtain a cookie session.
    ///  3. GET /connect/authorize with PKCE params (OpenIddict validates and renders consent page).
    ///  4. POST /connect/authorize with consent=granted → OpenIddict redirects to redirect_uri?code=...
    ///  5. Exchange the code at POST /connect/token → access_token.
    ///
    /// Returns (accessToken, userId) for the newly registered user.
    /// </summary>
    private async Task<(string AccessToken, string UserId)> ObtainBearerTokenAsync(
        string? email = null,
        string? password = null)
    {
        const string RedirectUri = "http://localhost/callback";
        email ??= $"oauth_{Guid.NewGuid():N}@example.com";
        password ??= "OAuthTest1!";

        // ── Step 1: DCR ──────────────────────────────────────────────────────
        var anon = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var dcrResp = await anon.PostAsJsonAsync("/connect/register", new
        {
            redirect_uris = new[] { RedirectUri },
            grant_types = new[] { "authorization_code" },
            client_name = $"test-client-{Guid.NewGuid():N}",
            scope = "mcp"
        });
        dcrResp.EnsureSuccessStatusCode();
        var dcrBody = await dcrResp.Content.ReadFromJsonAsync<JsonElement>();
        var clientId = dcrBody.GetProperty("client_id").GetString()!;

        // ── Step 2: Register user (sets auth cookie on cookieClient) ─────────
        var cookieClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var regResp = await cookieClient.PostAsJsonAsync("/api/v1/account/register", new
        {
            Email = email,
            Password = password
        });
        regResp.EnsureSuccessStatusCode();
        var regBody = await regResp.Content.ReadFromJsonAsync<JsonElement>();
        var userId = regBody.GetProperty("data").GetProperty("id").GetString()!;

        // ── Step 3: GET /connect/authorize with PKCE ─────────────────────────
        var (verifier, challenge) = GeneratePkce();
        var state = Guid.NewGuid().ToString("N");
        var nonce = Guid.NewGuid().ToString("N");

        var authorizeUrl = "/connect/authorize" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            "&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
            $"&scope={Uri.EscapeDataString("mcp")}" +
            $"&state={Uri.EscapeDataString(state)}" +
            $"&nonce={Uri.EscapeDataString(nonce)}" +
            $"&code_challenge={Uri.EscapeDataString(challenge)}" +
            "&code_challenge_method=S256";

        // GET first — OpenIddict validates and hands off to the Razor Page.
        // This seals the OpenIddict request into an encrypted cookie on the response.
        var getAuthResp = await cookieClient.GetAsync(authorizeUrl);
        var getBody = await getAuthResp.Content.ReadAsStringAsync();

        // Expected: 200 (consent page rendered) or 302 (already consented — auto-approved)
        if (getAuthResp.StatusCode == HttpStatusCode.Found)
        {
            // Already consented on a prior call in the same fixture — extract code directly
            var loc = getAuthResp.Headers.Location
                      ?? throw new InvalidOperationException("Redirect missing Location header");
            var q = HttpUtility.ParseQueryString(loc.Query);
            var existingCode = q["code"] ?? throw new InvalidOperationException($"No code in: {loc}");
            return await ExchangeCodeAsync(cookieClient, clientId, existingCode, RedirectUri, verifier, userId);
        }

        if (getAuthResp.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException(
                $"GET /connect/authorize returned {getAuthResp.StatusCode}: {getBody}");

        // ── Step 4: POST consent via the REAL rendered form ──────────────────
        // Parse the "Allow Access" form from the rendered consent page. This picks up BOTH
        // the antiforgery __RequestVerificationToken (proving the page emits it and the
        // antiforgery cookie was set on GET) AND every OAuth parameter the page round-trips
        // as a hidden field (proving params survive the POST body, which OpenIddict's
        // ExtractGetOrPostRequest reads on POST). HandleCookies carries the antiforgery
        // cookie automatically. If either consent-page fix regresses, the POST 400s here.
        var fields = ParseFirstFormHiddenFields(getBody);
        if (!fields.ContainsKey("__RequestVerificationToken"))
            throw new InvalidOperationException("Consent page did not render an antiforgery token.");
        if (!fields.ContainsKey("client_id"))
            throw new InvalidOperationException("Consent page did not round-trip OAuth params (no client_id hidden field).");

        var postAuthResp = await cookieClient.PostAsync(
            "/connect/authorize", new FormUrlEncodedContent(fields));

        // OpenIddict signs in and issues a 302 to redirect_uri?code=...&state=...
        if (postAuthResp.StatusCode != HttpStatusCode.Found)
        {
            var errorBody = await postAuthResp.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"POST /connect/authorize did not redirect. Status: {postAuthResp.StatusCode}\nBody: {errorBody}");
        }

        var location = postAuthResp.Headers.Location
                       ?? throw new InvalidOperationException("POST /connect/authorize redirect missing Location header.");

        var query = HttpUtility.ParseQueryString(location.Query);
        var code = query["code"]
                   ?? throw new InvalidOperationException($"No 'code' in redirect URI: {location}");

        return await ExchangeCodeAsync(cookieClient, clientId, code, RedirectUri, verifier, userId);
    }

    /// <summary>
    /// Extracts the hidden input fields (incl. the antiforgery __RequestVerificationToken)
    /// from the first &lt;form&gt; in the rendered consent HTML — i.e. the "Allow Access" form —
    /// so the consent POST replays exactly what a real browser would submit.
    /// </summary>
    private static Dictionary<string, string> ParseFirstFormHiddenFields(string html)
    {
        var formMatch = Regex.Match(html, "<form\\b[^>]*>(.*?)</form>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var scope = formMatch.Success ? formMatch.Groups[1].Value : html;

        var fields = new Dictionary<string, string>();
        foreach (Match input in Regex.Matches(scope, "<input\\b[^>]*>", RegexOptions.IgnoreCase))
        {
            var tag = input.Value;
            if (!tag.Contains("type=\"hidden\"", StringComparison.OrdinalIgnoreCase)) continue;
            var name = Regex.Match(tag, "name=\"([^\"]*)\"", RegexOptions.IgnoreCase);
            if (!name.Success) continue;
            var value = Regex.Match(tag, "value=\"([^\"]*)\"", RegexOptions.IgnoreCase);
            fields[HttpUtility.HtmlDecode(name.Groups[1].Value)] =
                value.Success ? HttpUtility.HtmlDecode(value.Groups[1].Value) : "";
        }
        return fields;
    }

    private static async Task<(string AccessToken, string UserId)> ExchangeCodeAsync(
        HttpClient client,
        string clientId,
        string code,
        string redirectUri,
        string verifier,
        string userId)
    {
        var tokenResp = await client.PostAsync("/connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = clientId,
                ["code_verifier"] = verifier
            }));

        if (!tokenResp.IsSuccessStatusCode)
        {
            var errorBody = await tokenResp.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"POST /connect/token failed with {tokenResp.StatusCode}: {errorBody}");
        }

        var tokenBody = await tokenResp.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenBody.GetProperty("access_token").GetString()
                          ?? throw new InvalidOperationException("No access_token in token response.");

        return (accessToken, userId);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // §10.1 — Discovery endpoints
    // ═══════════════════════════════════════════════════════════════════════════

    // MCP-OAUTH-001
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetOAuthProtectedResourceMetadata_Returns200WithRequiredFields()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var resp = await client.GetAsync("/.well-known/oauth-protected-resource");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(body.TryGetProperty("resource", out var resource), "Missing 'resource' field");
        Assert.Contains("/mcp", resource.GetString());

        Assert.True(body.TryGetProperty("authorization_servers", out var servers),
            "Missing 'authorization_servers' field");
        Assert.True(servers.GetArrayLength() > 0, "authorization_servers must be non-empty");

        Assert.True(body.TryGetProperty("scopes_supported", out var scopes),
            "Missing 'scopes_supported' field");
        Assert.Contains(scopes.EnumerateArray(), s => s.GetString() == AuthConstants.McpScope);
    }

    // MCP-OAUTH-002
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetOAuthAuthorizationServerMetadata_Returns200WithRequiredFields()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var resp = await client.GetAsync("/.well-known/oauth-authorization-server");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

        // RFC 8414 mandatory fields
        Assert.True(body.TryGetProperty("issuer", out _), "Missing 'issuer'");
        Assert.True(body.TryGetProperty("authorization_endpoint", out var authEp),
            "Missing 'authorization_endpoint'");
        Assert.Contains("/connect/authorize", authEp.GetString());

        Assert.True(body.TryGetProperty("token_endpoint", out var tokenEp),
            "Missing 'token_endpoint'");
        Assert.Contains("/connect/token", tokenEp.GetString());

        // PKCE S256 required
        Assert.True(body.TryGetProperty("code_challenge_methods_supported", out var methods),
            "Missing 'code_challenge_methods_supported'");
        Assert.Contains(methods.EnumerateArray(), m => m.GetString() == "S256");

        // authorization_code grant required
        Assert.True(body.TryGetProperty("grant_types_supported", out var grants),
            "Missing 'grant_types_supported'");
        Assert.Contains(grants.EnumerateArray(), g => g.GetString() == "authorization_code");
    }

    // MCP-OAUTH-003 — discovery endpoints must be anonymous (no auth required)
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DiscoveryEndpoints_AreAnonymous_NoAuthRequired()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var pr = await client.GetAsync("/.well-known/oauth-protected-resource");
        var as_ = await client.GetAsync("/.well-known/oauth-authorization-server");

        Assert.Equal(HttpStatusCode.OK, pr.StatusCode);
        Assert.Equal(HttpStatusCode.OK, as_.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // §10.2 — DCR: POST /connect/register
    // ═══════════════════════════════════════════════════════════════════════════

    // MCP-OAUTH-010
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DcrRegister_ValidLocalhostRedirectUri_Returns201WithClientId()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var resp = await client.PostAsJsonAsync("/connect/register", new
        {
            redirect_uris = new[] { "http://localhost:3000/callback" },
            grant_types = new[] { "authorization_code" },
            client_name = "Test Client",
            scope = "mcp"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(body.TryGetProperty("client_id", out var clientId), "Missing client_id");
        Assert.False(string.IsNullOrEmpty(clientId.GetString()), "client_id must not be empty");

        Assert.True(body.TryGetProperty("token_endpoint_auth_method", out var authMethod));
        Assert.Equal("none", authMethod.GetString()); // public client

        Assert.True(body.TryGetProperty("scope", out var scope));
        Assert.Equal(AuthConstants.McpScope, scope.GetString());
    }

    // MCP-OAUTH-011
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DcrRegister_ChatGptRedirectUri_Returns201()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var resp = await client.PostAsJsonAsync("/connect/register", new
        {
            redirect_uris = new[] { "https://chatgpt.com/aip/g-xxx/oauth/callback" },
            grant_types = new[] { "authorization_code" },
            client_name = "ChatGPT Connector",
            scope = "mcp"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    // MCP-OAUTH-012 — disallowed redirect host
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DcrRegister_DisallowedRedirectUri_Returns400WithInvalidRedirectUriError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var resp = await client.PostAsJsonAsync("/connect/register", new
        {
            redirect_uris = new[] { "https://evil.example.com/steal" },
            grant_types = new[] { "authorization_code" },
            scope = "mcp"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_redirect_uri", body.GetProperty("error").GetString());
    }

    // MCP-OAUTH-013 — disallowed grant type
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DcrRegister_ClientCredentialsGrant_Returns400WithInvalidClientMetadataError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var resp = await client.PostAsJsonAsync("/connect/register", new
        {
            redirect_uris = new[] { "http://localhost/callback" },
            grant_types = new[] { "client_credentials" },
            scope = "mcp"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_client_metadata", body.GetProperty("error").GetString());
    }

    // MCP-OAUTH-014 — disallowed scope
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DcrRegister_UnknownScope_Returns400WithInvalidClientMetadataError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var resp = await client.PostAsJsonAsync("/connect/register", new
        {
            redirect_uris = new[] { "http://localhost/callback" },
            grant_types = new[] { "authorization_code" },
            scope = "openid profile email"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_client_metadata", body.GetProperty("error").GetString());
    }

    // MCP-OAUTH-015 — missing redirect URI
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DcrRegister_NoRedirectUri_Returns400()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var resp = await client.PostAsJsonAsync("/connect/register", new
        {
            grant_types = new[] { "authorization_code" },
            scope = "mcp"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_client_metadata", body.GetProperty("error").GetString());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // §10.3 — /mcp authentication: no-auth and invalid-auth cases
    // ═══════════════════════════════════════════════════════════════════════════

    // MCP-OAUTH-020
    [Fact]
    [Trait("Category", "Integration")]
    public async Task McpEndpoint_NoAuth_Returns401()
    {
        // No auth header — should be rejected before session is established
        var resp = await RawMcpPostAsync(McpToolsListJson(), authorizationHeader: null);

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // MCP-OAUTH-021 — 401 for garbage Bearer must carry WWW-Authenticate: Bearer header
    [Fact]
    [Trait("Category", "Integration")]
    public async Task McpEndpoint_GarbageBearerToken_401CarriesWwwAuthenticateHeader()
    {
        var resp = await RawMcpPostAsync(McpToolsListJson(), "Bearer garbage.token.value");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);

        // RFC 6750 §3: WWW-Authenticate header must be present on 401 from Bearer validation
        Assert.True(resp.Headers.Contains("WWW-Authenticate"),
            "401 from Bearer validation must include WWW-Authenticate header");

        var wwwAuth = resp.Headers.WwwAuthenticate.ToString();
        Assert.Contains("Bearer", wwwAuth, StringComparison.OrdinalIgnoreCase);
    }

    // MCP-OAUTH-022 — completely fabricated bearer token
    [Fact]
    [Trait("Category", "Integration")]
    public async Task McpEndpoint_FabricatedBearerToken_Returns401()
    {
        var resp = await RawMcpPostAsync(McpToolsListJson(), "Bearer not.a.real.token");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // MCP-OAUTH-023 — structurally valid JWT from wrong issuer
    [Fact]
    [Trait("Category", "Integration")]
    public async Task McpEndpoint_WrongIssuerBearerToken_Returns401()
    {
        // Build a fake JWT header.payload.invalidsignature — looks like a JWT but is not
        // signed by the test server's key.
        var header = Base64UrlEncode(Encoding.UTF8.GetBytes("""{"alg":"RS256","typ":"JWT"}"""));
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(
            $$"""{"iss":"https://evil.example.com","sub":"fakeuserid","aud":"https://taskpilot.azurewebsites.net/mcp","exp":{{now + 3600}},"iat":{{now}}}"""));
        var fakeJwt = $"{header}.{payload}.invalidsignature";

        var resp = await RawMcpPostAsync(McpToolsListJson(), $"Bearer {fakeJwt}");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // §10.4 — X-Api-Key regression: existing API key auth still works on /mcp
    // ═══════════════════════════════════════════════════════════════════════════

    // MCP-OAUTH-030
    [Fact]
    [Trait("Category", "Integration")]
    public async Task McpEndpoint_ValidApiKey_AuthenticatesAndListsTools()
    {
        // Create a user + API key via cookie auth
        var (cookieClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var genResp = await cookieClient.PostAsJsonAsync("/api/v1/apikeys",
            new { Name = $"mcp-regression-{Guid.NewGuid():N}" });
        genResp.EnsureSuccessStatusCode();

        var genBody = await genResp.Content.ReadFromJsonAsync<JsonElement>();
        var plainTextKey = genBody.GetProperty("data").GetProperty("plainTextKey").GetString()!;

        // Start a proper MCP session with the API key
        var session = await CreateApiKeyMcpSessionAsync(plainTextKey);
        var resp = await session.ToolsListAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // MCP-OAUTH-031 — API key data isolation: user sees only their own tasks
    [Fact]
    [Trait("Category", "Integration")]
    public async Task McpEndpoint_ApiKey_DataIsolation_UserSeesOnlyOwnTasks()
    {
        // User A creates a task via REST API
        var (clientA, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var uniqueTitle = $"IsolationTask_ApiKey_{Guid.NewGuid():N}";
        (await clientA.PostAsJsonAsync("/api/v1/tasks", new
        {
            Title = uniqueTitle,
            TaskTypeId = 1, Area = 0, Priority = 1, Status = 0,
            TargetDateType = 1, IsRecurring = false
        })).EnsureSuccessStatusCode();

        // User B registers and generates their own API key
        var (clientB, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var keyRespB = await clientB.PostAsJsonAsync("/api/v1/apikeys",
            new { Name = $"isolation-key-B-{Guid.NewGuid():N}" });
        keyRespB.EnsureSuccessStatusCode();
        var plainTextKeyB = (await keyRespB.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("plainTextKey").GetString()!;

        // User B calls list_tasks via MCP — must NOT see User A's task
        var sessionB = await CreateApiKeyMcpSessionAsync(plainTextKeyB);
        var resp = await sessionB.ListTasksAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var raw = await resp.Content.ReadAsStringAsync();
        Assert.DoesNotContain(uniqueTitle, raw);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // §10.5 — Valid Bearer token: /mcp access succeeds
    // ═══════════════════════════════════════════════════════════════════════════

    // MCP-OAUTH-040
    [Fact]
    [Trait("Category", "Integration")]
    public async Task McpEndpoint_ValidMcpScopedBearerToken_Returns200()
    {
        var (accessToken, _) = await ObtainBearerTokenAsync();

        // CreateBearerMcpSessionAsync calls initialize and asserts 200
        var session = await CreateBearerMcpSessionAsync(accessToken);
        _ = session; // session established = 200 confirmed
    }

    // MCP-OAUTH-041 — tools/list returns tool definitions
    [Fact]
    [Trait("Category", "Integration")]
    public async Task McpEndpoint_BearerToken_ToolsListReturnsExpectedTools()
    {
        var (accessToken, _) = await ObtainBearerTokenAsync();
        var session = await CreateBearerMcpSessionAsync(accessToken);

        var resp = await session.ToolsListAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var raw = await resp.Content.ReadAsStringAsync();
        Assert.Contains("list_tasks", raw);
        Assert.Contains("create_task", raw);
    }

    // MCP-OAUTH-042 — tools/call create_task succeeds via Bearer
    [Fact]
    [Trait("Category", "Integration")]
    public async Task McpEndpoint_BearerToken_CreateTask_Succeeds()
    {
        var (accessToken, _) = await ObtainBearerTokenAsync();
        var session = await CreateBearerMcpSessionAsync(accessToken);

        var uniqueTitle = $"BearerCreated_{Guid.NewGuid():N}";
        var resp = await session.CreateTaskAsync(uniqueTitle);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var raw = await resp.Content.ReadAsStringAsync();
        Assert.Contains(uniqueTitle, raw);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // §10.6 — Bearer data isolation: user A's token cannot see user B's tasks
    // ═══════════════════════════════════════════════════════════════════════════

    // MCP-OAUTH-050
    [Fact]
    [Trait("Category", "Integration")]
    public async Task McpEndpoint_BearerToken_DataIsolation_UserATokenCannotSeeUserBTasks()
    {
        // User A creates a task via REST API
        var (clientA, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var uniqueTitle = $"IsolationTask_OAuth_{Guid.NewGuid():N}";
        (await clientA.PostAsJsonAsync("/api/v1/tasks", new
        {
            Title = uniqueTitle,
            TaskTypeId = 1, Area = 0, Priority = 1, Status = 0,
            TargetDateType = 1, IsRecurring = false
        })).EnsureSuccessStatusCode();

        // User B gets a Bearer token and lists tasks — must NOT see User A's task
        var (tokenB, _) = await ObtainBearerTokenAsync();
        var sessionB = await CreateBearerMcpSessionAsync(tokenB);

        var listResp = await sessionB.ListTasksAsync();

        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var raw = await listResp.Content.ReadAsStringAsync();
        Assert.DoesNotContain(uniqueTitle, raw);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // §10.7 — LastModifiedBy attribution
    // ═══════════════════════════════════════════════════════════════════════════

    // MCP-OAUTH-060 — Bearer write records "oauth:{subject}"
    [Fact]
    [Trait("Category", "Integration")]
    public async Task McpCreateTask_ViaBearerToken_LastModifiedByStartsWithOauth()
    {
        var (accessToken, _) = await ObtainBearerTokenAsync();
        var session = await CreateBearerMcpSessionAsync(accessToken);

        var uniqueTitle = $"OAuthModifiedBy_{Guid.NewGuid():N}";
        var resp = await session.CreateTaskAsync(uniqueTitle);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        // The create_task tool serializes the task entity which includes lastModifiedBy.
        // The OAuth path sets it to "oauth:{userId}".
        var raw = await resp.Content.ReadAsStringAsync();
        Assert.Contains("oauth:", raw);
    }

    // MCP-OAUTH-061 — API key write still records "api:{keyName}"
    [Fact]
    [Trait("Category", "Integration")]
    public async Task McpCreateTask_ViaApiKey_LastModifiedByStartsWithApi()
    {
        var (cookieClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var keyName = $"api-attrib-key-{Guid.NewGuid():N}";
        var keyResp = await cookieClient.PostAsJsonAsync("/api/v1/apikeys", new { Name = keyName });
        keyResp.EnsureSuccessStatusCode();
        var plainTextKey = (await keyResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("plainTextKey").GetString()!;

        var session = await CreateApiKeyMcpSessionAsync(plainTextKey);
        var uniqueTitle = $"ApiKeyModifiedBy_{Guid.NewGuid():N}";
        var resp = await session.CreateTaskAsync(uniqueTitle);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var raw = await resp.Content.ReadAsStringAsync();
        Assert.Contains($"api:{keyName}", raw);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // §10.8 — End-to-end: full PKCE flow → /mcp tool call
    // ═══════════════════════════════════════════════════════════════════════════

    // MCP-OAUTH-070
    [Fact]
    [Trait("Category", "Integration")]
    public async Task EndToEnd_PkceFlow_TokenUsableOnMcp()
    {
        var (accessToken, _) = await ObtainBearerTokenAsync();
        var session = await CreateBearerMcpSessionAsync(accessToken);

        var statsResp = await session.GetStatsAsync();

        Assert.Equal(HttpStatusCode.OK, statsResp.StatusCode);
        var raw = await statsResp.Content.ReadAsStringAsync();
        // get_stats returns a JSON TaskStatsResponse in the MCP SSE envelope.
        // The outer MCP envelope JSON-encodes the inner stats JSON as a string,
        // so property names appear as Unicode escapes ("TotalActive").
        // Extract the inner stats JSON from the "text" field and parse it.
        var dataLine = raw.Split('\n').FirstOrDefault(l => l.StartsWith("data:"))?.Substring(5).Trim()
                       ?? throw new InvalidOperationException($"No SSE data line in: {raw}");
        var envelope = JsonDocument.Parse(dataLine);
        var textContent = envelope.RootElement
            .GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()!;
        var statsJson = JsonDocument.Parse(textContent);
        Assert.True(statsJson.RootElement.TryGetProperty("TotalActive", out _),
            $"Expected TotalActive in stats JSON. Got: {textContent}");
    }

    // MCP-OAUTH-071 — token is scoped to the correct user; other users cannot see the task
    [Fact]
    [Trait("Category", "Integration")]
    public async Task EndToEnd_BearerToken_IssuedToCorrectUser_CreatedTaskBelongsToThatUser()
    {
        var uniqueTitle = $"E2E_UserBound_{Guid.NewGuid():N}";
        var (accessToken, _) = await ObtainBearerTokenAsync();
        var session = await CreateBearerMcpSessionAsync(accessToken);

        var createResp = await session.CreateTaskAsync(uniqueTitle);

        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        Assert.Contains(uniqueTitle, await createResp.Content.ReadAsStringAsync());

        // A different user's Bearer token must not see this task
        var (otherToken, _) = await ObtainBearerTokenAsync();
        var otherSession = await CreateBearerMcpSessionAsync(otherToken);

        var listResp = await otherSession.ListTasksAsync();

        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        Assert.DoesNotContain(uniqueTitle, await listResp.Content.ReadAsStringAsync());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // §10.9 — Both auth schemes work side-by-side on /mcp
    // ═══════════════════════════════════════════════════════════════════════════

    // MCP-OAUTH-080
    [Fact]
    [Trait("Category", "Integration")]
    public async Task McpEndpoint_BothAuthSchemesAccepted_BearerAndApiKey()
    {
        // Bearer path: ObtainBearerTokenAsync + initialize
        var (accessToken, _) = await ObtainBearerTokenAsync();
        var bearerSession = await CreateBearerMcpSessionAsync(accessToken);
        _ = bearerSession; // initialized successfully = Bearer auth works

        // API key path: register user + generate key + initialize
        var (cookieClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var keyResp = await cookieClient.PostAsJsonAsync("/api/v1/apikeys",
            new { Name = $"side-by-side-{Guid.NewGuid():N}" });
        keyResp.EnsureSuccessStatusCode();
        var plainTextKey = (await keyResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("plainTextKey").GetString()!;

        var apiKeySession = await CreateApiKeyMcpSessionAsync(plainTextKey);
        _ = apiKeySession; // initialized successfully = API key auth works
    }
}
