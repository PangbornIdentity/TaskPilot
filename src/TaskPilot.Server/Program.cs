using Microsoft.AspNetCore.Identity;
using Serilog;
using Serilog.Events;
using TaskPilot.Server.Data;
using TaskPilot.Server.Extensions;
using TaskPilot.Server.Middleware;

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
    builder.Services.AddTaskPilotDatabase(builder.Configuration);

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
    builder.Services.AddTaskPilotValidators();

    // Controllers
    builder.Services.AddControllers();

    // CORS (for Blazor WASM in dev)
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins("https://localhost:5001", "http://localhost:5000")
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

    // Auto-migrate on startup in development
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

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

    app.MapControllers();

    // Serve Blazor WASM static files
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");

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
