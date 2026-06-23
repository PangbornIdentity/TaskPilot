using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Serilog;
using TaskPilot.Extensions;
using TaskPilot.Constants;
using TaskPilot.Data;
using TaskPilot.Mcp;
using TaskPilot.Repositories;
using TaskPilot.Repositories.Interfaces;
using TaskPilot.Services;
using TaskPilot.Services.Health;
using TaskPilot.Services.Health.Checks;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.Validators;

namespace TaskPilot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskPilotDatabase(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (env.IsDevelopment())
                options.UseSqlite(connectionString);
            else
                options.UseSqlServer(connectionString);

            // Register OpenIddict EF Core stores against this DbContext.
            // UseOpenIddict() in ApplicationDbContext.OnModelCreating maps the tables.
            options.UseOpenIddict();
        });

        return services;
    }

    public static IServiceCollection AddTaskPilotAuthentication(this IServiceCollection services)
    {
        const string MultiScheme = "MultiScheme";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = MultiScheme;
            options.DefaultChallengeScheme = MultiScheme;
        })
        .AddPolicyScheme(MultiScheme, "Cookie, API Key, or Bearer", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                // Bearer branch must come before Cookie — Authorization header takes precedence.
                if (context.Request.Headers.ContainsKey(AuthConstants.ApiKeyHeader))
                    return AuthConstants.ApiKeyScheme;

                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (authHeader is not null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    return AuthConstants.BearerScheme;

                return AuthConstants.CookieScheme;
            };
        })
        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            AuthConstants.ApiKeyScheme, _ => { });

        // Configure cookie auth to redirect web requests to login page, return 401 for API
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/auth/login";
            options.LogoutPath = "/auth/logout";
            options.AccessDeniedPath = "/auth/login";
            options.Events.OnRedirectToLogin = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            };
        });

        return services;
    }

    /// <summary>
    /// Registers the OpenIddict Authorization Server and Bearer validation schemes.
    /// When <c>OAuth:Enabled</c> is false (or OAuth is misconfigured in non-dev), this method
    /// is a no-op — /mcp continues to work via X-Api-Key only.
    /// </summary>
    public static IServiceCollection AddTaskPilotOAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment env,
        Serilog.ILogger logger)
    {
        var enabled = configuration.GetValue("OAuth:Enabled", defaultValue: true);
        if (!enabled)
        {
            logger.Information("OAuth:Enabled is false — OAuth AS and Bearer scheme skipped; /mcp continues on X-Api-Key only.");
            return services;
        }

        // Derive the issuer/base URL from config; fall back to localhost for dev.
        var baseUrl = configuration[AuthConstants.OAuthBaseUrlConfigKey]
                      ?? (env.IsDevelopment() ? "http://localhost:5125" : null);

        if (string.IsNullOrWhiteSpace(baseUrl) && !env.IsDevelopment())
        {
            logger.Warning(
                "OAuth:BaseUrl is not configured in production. OAuth AS will not start. " +
                "/mcp continues to accept X-Api-Key. Set OAuth:BaseUrl (e.g. https://taskpilot.azurewebsites.net) to enable Bearer.");
            return services;
        }

        services.AddOpenIddict()

            // ── Authorization Server ────────────────────────────────────────────
            .AddServer(options =>
            {
                // Token + well-known endpoints
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetTokenEndpointUris("/connect/token")
                       .SetIntrospectionEndpointUris("/connect/introspect")
                       .SetRevocationEndpointUris("/connect/revocation")
                       .SetEndSessionEndpointUris("/connect/logout");

                // Dynamic Client Registration (RFC 7591):
                // OpenIddict 7.x does not expose a built-in DCR endpoint builder.
                // New clients are registered at startup (or via IOpenIddictApplicationManager
                // in a management API) and the registration_endpoint is advertised manually in
                // GET /.well-known/oauth-authorization-server via the OpenIddict metadata document.
                // ChatGPT's connector will POST to /connect/register; a lightweight
                // DcrController handles that (see Controllers/DcrController.cs).

                // Allowed grant types: authorization_code + refresh_token only.
                // client_credentials and implicit are rejected.
                options.AllowAuthorizationCodeFlow()
                       .AllowRefreshTokenFlow();

                // PKCE S256 mandatory; plain is rejected.
                options.RequireProofKeyForCodeExchange();

                // Scopes + resource
                options.RegisterScopes(AuthConstants.McpScope);

                // Signing and encryption key management
                if (env.IsDevelopment())
                {
                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();
                }
                else
                {
                    // Production: load from IConfiguration (Key Vault in iter2).
                    // If keys are absent, OpenIddict startup will throw — caught by the
                    // degraded-mode guard above (we won't reach here without baseUrl).
                    options.AddEphemeralEncryptionKey()
                           .AddEphemeralSigningKey();
                    // TODO (iter2): replace Ephemeral with certs from Key Vault:
                    // options.AddSigningCertificate(configuration["OpenIddict:SigningCertificate"]!)
                    //        .AddEncryptionCertificate(configuration["OpenIddict:EncryptionCertificate"]!);
                }

                // Disable access token encryption for MCP clients (Bearer tokens are
                // opaque reference tokens by default in OpenIddict 5+; self-contained JWT
                // with local validation is the right choice for our in-process setup).
                options.DisableAccessTokenEncryption();

                // Issuer is config-driven (dev: http://localhost:5125; prod: Azure URL)
                options.SetIssuer(new Uri(baseUrl!));

                // Use ASP.NET Core host integration (emit endpoints, challenge/forbid).
                // In Development, disable transport-security requirement so the AS responds
                // over plain http://localhost without error ID2083.
                // In non-Development the app runs behind Azure App Service TLS termination:
                // UseForwardedHeaders (in Program.cs) sets Request.Scheme = "https" from
                // X-Forwarded-Proto before OpenIddict inspects the request, so no relaxation
                // is needed — and none is applied.
                var aspNetCoreBuilder = options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough();

                if (env.IsDevelopment())
                    aspNetCoreBuilder.DisableTransportSecurityRequirement();
            })

            // ── EF Core stores (SQLite dev / SQL Server prod) ──────────────────
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>();
            })

            // ── Resource-server / Bearer token validation ─────────────────────
            .AddValidation(options =>
            {
                // In-process validation — no remote introspection call.
                options.UseLocalServer();

                // Require mcp scope on every /mcp request that uses Bearer.
                options.AddAudiences(AuthConstants.McpResourceIndicator);

                options.UseAspNetCore();
            });

        // Seed the mcp scope if it doesn't exist yet (idempotent on every startup).
        // This runs inside the DI container build, so it's registered as a hosted-service-style
        // IHostedService via OpenIddict's built-in worker.
        services.AddHostedService<OAuthSeedWorker>();

        return services;
    }

    public static IServiceCollection AddTaskPilotRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<ITaskTypeRepository, TaskTypeRepository>();
        return services;
    }

    public static IServiceCollection AddTaskPilotServices(this IServiceCollection services)
    {
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IStatsService, StatsService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<ITaskTypeService, TaskTypeService>();
        return services;
    }

    public static IServiceCollection AddTaskPilotChangelog(this IServiceCollection services, IWebHostEnvironment env)
    {
        var path = Path.Combine(env.ContentRootPath, "app-changelog.json");
        var json = File.Exists(path) ? File.ReadAllText(path) : "{}";
        services.AddSingleton<IChangelogService>(new ChangelogService(json));
        return services;
    }

    public static IServiceCollection AddTaskPilotValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateTaskRequestValidator>();
        return services;
    }

    public static IServiceCollection AddTaskPilotMcp(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMcpServer()
                .WithHttpTransport()
                .WithTools<TaskPilotMcpTools>();
        return services;
    }

    public static IServiceCollection AddTaskPilotHealth(this IServiceCollection services)
    {
        // Required checks (used by /ready and /full)
        services.AddScoped<IHealthCheckComponent, DatabaseHealthCheck>();
        services.AddScoped<IHealthCheckComponent, MigrationsHealthCheck>();
        services.AddScoped<IHealthCheckComponent, ConfigHealthCheck>();

        // Optional checks (used by /full only)
        services.AddScoped<IHealthCheckComponent, AuthHandlersHealthCheck>();
        services.AddScoped<IHealthCheckComponent, McpHealthCheck>();
        services.AddScoped<IHealthCheckComponent, TempWritableHealthCheck>();
        services.AddScoped<IHealthCheckComponent, AssemblyMetadataHealthCheck>();

        services.AddScoped<IHealthService, HealthService>();
        services.AddSingleton<AssetsService>();
        return services;
    }
}
