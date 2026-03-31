using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Data;

namespace TaskPilot.Tests.Integration;

public class TaskPilotWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private string? _dbPath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"taskpilot_test_{Guid.NewGuid():N}.db");

        builder.ConfigureServices(services =>
        {
            // Remove the real DbContext registration (which may be SQL Server in non-dev environments)
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Always use a unique SQLite file for integration tests — never Azure SQL
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={_dbPath}"));
        });

        builder.UseEnvironment("Development");

        // Override HMAC secret for tests
        builder.ConfigureAppConfiguration((ctx, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hmac:SecretKey"] = "test-secret-key-for-integration-tests"
            }));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        if (_dbPath is not null && File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { /* best effort */ }
        }
    }
}
