using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using TaskPilot.Constants;
using TaskPilot.Data;
using TaskPilot.Extensions;
using TaskPilot.Middleware;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TaskPilot Server");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/taskpilot-.log", rollingInterval: RollingInterval.Day));

    // Database (includes UseOpenIddict() on DbContext options)
    builder.Services.AddTaskPilotDatabase(builder.Configuration, builder.Environment);

    // Identity
    builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 10;
        options.SignIn.RequireConfirmedAccount = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // Authentication (cookie + API key + Bearer)
    builder.Services.AddTaskPilotAuthentication();

    // OAuth 2.1 AS + Bearer validation (additive — skipped if OAuth:Enabled=false or misconfigured)
    builder.Services.AddTaskPilotOAuth(
        builder.Configuration,
        builder.Environment,
        Log.Logger);

    builder.Services.AddAuthorization(options =>
    {
        // McpApiKey: satisfied by X-Api-Key OR OAuth Bearer — both schemes must be listed.
        options.AddPolicy("McpApiKey", policy =>
            policy.AddAuthenticationSchemes(AuthConstants.ApiKeyScheme, AuthConstants.BearerScheme)
                  .RequireAuthenticatedUser());
    });

    // Repositories & Services
    builder.Services.AddTaskPilotRepositories();
    builder.Services.AddTaskPilotServices();
    builder.Services.AddTaskPilotChangelog(builder.Environment);
    builder.Services.AddTaskPilotValidators();
    builder.Services.AddTaskPilotMcp();
    builder.Services.AddTaskPilotHealth();

    // Controllers + Razor Pages
    builder.Services.AddControllers();
    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/");
        options.Conventions.AllowAnonymousToPage("/Auth/Login");
        options.Conventions.AllowAnonymousToPage("/Auth/Register");
        options.Conventions.AllowAnonymousToPage("/Error");
        options.Conventions.AllowAnonymousToPage("/Health/Index");
        // OAuth consent page: requires cookie auth (handled by [Authorize(AuthenticationSchemes=CookieScheme)] on the model)
        // but must NOT be blocked by the global AuthorizeFolder. The page model's own [Authorize]
        // attribute takes over and redirects to /auth/login when the user is not signed in.
        options.Conventions.AllowAnonymousToPage("/Connect/Authorize");
        options.Conventions.AllowAnonymousToPage("/Connect/Token");
    });

    // CORS (for dev clients)
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.SetIsOriginAllowed(origin =>
                      Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                      uri.Host == "localhost")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()));

    // Swagger (dev only)
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "TaskPilot API", Version = "v1" });
    });

    var app = builder.Build();

    // Startup schema management:
    // - Development (SQLite): EnsureCreatedAsync — fast, no migration history needed locally
    //   NOTE: After adding the AddOpenIddict EF migration, local taskpilot.db must be deleted
    //   and recreated (EnsureCreatedAsync does not apply migrations to an existing file).
    // - Production (Azure SQL): MigrateAsync — applies pending SQL Server migrations (incl. AddOpenIddict)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (app.Environment.IsDevelopment())
        {
            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            // Retry migration up to 5 times — Azure SQL serverless may be waking from autopause
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    await db.Database.MigrateAsync();
                    break;
                }
                catch (Exception ex) when (attempt < 5)
                {
                    Log.Warning(ex, "Database migration attempt {Attempt}/5 failed, retrying in {Delay}s", attempt, attempt * 5);
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 5));
                }
            }
        }
    }

    // ── Forwarded Headers (must be FIRST in the pipeline) ────────────────────
    // Azure App Service terminates TLS at the edge and forwards requests to the
    // app as HTTP with X-Forwarded-Proto: https and X-Forwarded-For set.
    // Without this middleware, Request.Scheme is "http" in production and
    // OpenIddict's transport-security check returns 400 on every OAuth endpoint.
    //
    // KnownNetworks and KnownProxies are cleared because Azure App Service does
    // not expose a fixed proxy IP — the platform strips client-spoofed forwarded
    // headers at the edge before they reach the app, so trusting any forwarded
    // proto header is safe in this deployment topology.
    // (See Microsoft docs: "Configure ASP.NET Core to work with proxy servers and
    //  load balancers" → Azure App Service guidance.)
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        // Azure App Service strips client-spoofed headers at the edge, so
        // we can trust any proxy without pinning a known IP.
        KnownIPNetworks = { },
        KnownProxies = { }
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskPilot API v1"));
    }

    app.UseGlobalExceptionHandler();
    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseCors();

    app.UseAuthentication();

    // OpenIddict uses ASP.NET Core endpoint routing (registered via UseAspNetCore() in AddServer).
    // It integrates automatically once UseAuthentication + UseAuthorization are in place.
    // /connect/* and /.well-known/oauth-authorization-server are registered as endpoint routes.
    app.UseAuthorization();

    app.UseApiAudit();

    app.UseStaticFiles();

    app.MapControllers();
    app.MapRazorPages();

    // /mcp: accepts X-Api-Key (ApiKey scheme) OR OAuth Bearer (BearerScheme).
    // The McpApiKey policy lists both schemes — per-request the ForwardDefaultSelector
    // in AddTaskPilotAuthentication picks the right one automatically.
    app.MapMcp("/mcp").RequireAuthorization("McpApiKey");

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "TaskPilot Server terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Make Program accessible to test projects
public partial class Program { }
