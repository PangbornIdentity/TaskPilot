using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
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

    // Database
    builder.Services.AddTaskPilotDatabase(builder.Configuration, builder.Environment);

    // Identity
    builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // Authentication (cookie + API key)
    builder.Services.AddTaskPilotAuthentication();
    builder.Services.AddAuthorization();

    // Repositories & Services
    builder.Services.AddTaskPilotRepositories();
    builder.Services.AddTaskPilotServices();
    builder.Services.AddTaskPilotChangelog(builder.Environment);
    builder.Services.AddTaskPilotValidators();

    // Controllers + Razor Pages
    builder.Services.AddControllers();
    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/");
        options.Conventions.AllowAnonymousToPage("/Auth/Login");
        options.Conventions.AllowAnonymousToPage("/Auth/Register");
        options.Conventions.AllowAnonymousToPage("/Error");
    });

    // CORS (for Blazor WASM in dev)
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
    // - Production (Azure SQL): MigrateAsync — applies pending SQL Server migrations
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (app.Environment.IsDevelopment())
            await db.Database.EnsureCreatedAsync();
        else
            await db.Database.MigrateAsync();
    }

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
    app.UseAuthorization();

    app.UseApiAudit();

    app.UseStaticFiles();

    app.MapControllers();
    app.MapRazorPages();

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
