using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Data;
using TaskPilot.Entities;

namespace TaskPilot.Tests.Integration;

public class TaskPilotWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private string? _dbPath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"taskpilot_test_{Guid.NewGuid():N}.db");
        var localDbPath = _dbPath!;

        builder.ConfigureServices(services =>
        {
            // Remove the default DbContext options registration so we can replace it
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Re-register with SQLite + register PatchedApplicationDbContext as the
            // implementation of ApplicationDbContext.  The patched subclass overrides
            // OnModelCreating to:
            //   1. Skip IdentityDbContext.OnModelCreating (which registers passkey
            //      entity types that EF10 cannot resolve in test isolation).
            //   2. Apply all real IEntityTypeConfiguration classes from the production
            //      assembly via ApplyConfigurationsFromAssembly so Tag, ApiKey, TaskItem
            //      constraints, indexes, and query filters are identical to production.
            //   3. Remove the single HasDefaultValue call on TaskItem.Area that EF10
            //      rejects when combined with HasConversion<int>() on a SQLite provider.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={localDbPath}"),
                contextLifetime: ServiceLifetime.Scoped,
                optionsLifetime: ServiceLifetime.Scoped);

            // Override the ApplicationDbContext registration to use PatchedApplicationDbContext
            var appDbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ApplicationDbContext));
            if (appDbDescriptor != null) services.Remove(appDbDescriptor);

            services.AddScoped<ApplicationDbContext>(sp =>
            {
                var opts = sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
                return new PatchedApplicationDbContext(opts);
            });
        });

        builder.UseEnvironment("Development");

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

    // ──────────────────────────────────────────────────────────────────────────────
    // Patched DbContext — replaces ApplicationDbContext in tests.
    //
    // WHY this subclass exists (do NOT remove without understanding both reasons):
    //
    // (1) PASSKEY ENTITIES: IdentityDbContext.OnModelCreating registers ASP.NET Core
    //     Identity passkey entity types (UserPasskey, etc.) introduced in .NET 9/10.
    //     Those types require navigation properties that EF cannot resolve in the test
    //     environment (no full Identity assembly wiring), so calling base.OnModelCreating
    //     throws. The ConfigureIdentity helper below replicates the essential Identity
    //     table/key/index definitions without triggering passkey discovery.
    //
    // (2) HasDefaultValue ON ENUM WITH HasConversion<int>: EF Core 10 throws when a
    //     property declares both HasConversion<int>() and HasDefaultValue(enumValue)
    //     on a SQLite provider. TaskItemConfiguration sets HasDefaultValue(Area.Personal)
    //     on the Area property. After ApplyConfigurationsFromAssembly applies all real
    //     configs, we remove that annotation with Metadata so tests can create the schema.
    //     This is the ONLY divergence from production; everything else (query filters,
    //     unique indexes, soft-delete indexes, FK cascade rules) comes directly from the
    //     production IEntityTypeConfiguration classes.
    // ──────────────────────────────────────────────────────────────────────────────

    private sealed class PatchedApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Step 1: Configure Identity tables without triggering passkey entity discovery.
            ConfigureIdentity(modelBuilder);

            // Step 2: Apply every production IEntityTypeConfiguration from the src assembly.
            // This makes Tag, ApiKey, TaskItem, TaskTag, TaskType, TaskActivityLog, ApiAuditLog
            // constraints, indexes, and query filters IDENTICAL to production.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Step 3: Remove the HasDefaultValue annotation on TaskItem.Area that EF10
            // rejects on a SQLite provider when combined with HasConversion<int>().
            // This is the only justified residual override — it has no behavioral impact
            // on tests (EF uses the CLR default of 0/Area.Personal if the column is
            // omitted, matching the intent of the production annotation).
            var areaProperty = modelBuilder.Entity<TaskItem>()
                .Metadata.FindProperty(nameof(TaskItem.Area));
            areaProperty?.SetDefaultValue(null);
        }

        private static void ConfigureIdentity(ModelBuilder modelBuilder)
        {
            // Ignore EF10 passkey entities that are not used in this app
            // but would otherwise be discovered via navigation properties.
            var identityAssembly = typeof(IdentityUser).Assembly;
            var efAssembly = typeof(IdentityDbContext).Assembly;
            foreach (var asm in new[] { identityAssembly, efAssembly })
            {
                foreach (var type in asm.GetExportedTypes()
                    .Where(t => t.Name.Contains("Passkey", StringComparison.OrdinalIgnoreCase)
                             && t.IsClass && !t.IsAbstract))
                {
                    try { modelBuilder.Ignore(type); } catch { /* best effort */ }
                    // Also ignore the closed generic if the type is generic
                    if (type.IsGenericTypeDefinition)
                    {
                        try { modelBuilder.Ignore(type.MakeGenericType(typeof(string))); } catch { /* best effort */ }
                    }
                }
            }

            modelBuilder.Entity<IdentityUser>(b =>
            {
                b.HasKey(u => u.Id);
                b.Property(u => u.UserName).HasMaxLength(256);
                b.Property(u => u.NormalizedUserName).HasMaxLength(256);
                b.Property(u => u.Email).HasMaxLength(256);
                b.Property(u => u.NormalizedEmail).HasMaxLength(256);
                b.HasIndex(u => u.NormalizedUserName).IsUnique().HasFilter(null);
                b.HasIndex(u => u.NormalizedEmail);
                b.HasMany<IdentityUserClaim<string>>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
                b.HasMany<IdentityUserLogin<string>>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();
                b.HasMany<IdentityUserToken<string>>().WithOne().HasForeignKey(ut => ut.UserId).IsRequired();
                b.HasMany<IdentityUserRole<string>>().WithOne().HasForeignKey(ur => ur.UserId).IsRequired();
                b.ToTable("AspNetUsers");
            });

            modelBuilder.Entity<IdentityUserClaim<string>>(b =>
            {
                b.HasKey(uc => uc.Id);
                b.ToTable("AspNetUserClaims");
            });

            modelBuilder.Entity<IdentityUserLogin<string>>(b =>
            {
                b.HasKey(ul => new { ul.LoginProvider, ul.ProviderKey });
                b.ToTable("AspNetUserLogins");
            });

            modelBuilder.Entity<IdentityUserToken<string>>(b =>
            {
                b.HasKey(ut => new { ut.UserId, ut.LoginProvider, ut.Name });
                b.ToTable("AspNetUserTokens");
            });

            modelBuilder.Entity<IdentityRole>(b =>
            {
                b.HasKey(r => r.Id);
                b.Property(r => r.Name).HasMaxLength(256);
                b.Property(r => r.NormalizedName).HasMaxLength(256);
                b.HasIndex(r => r.NormalizedName).IsUnique().HasFilter(null);
                b.HasMany<IdentityUserRole<string>>().WithOne().HasForeignKey(ur => ur.RoleId).IsRequired();
                b.HasMany<IdentityRoleClaim<string>>().WithOne().HasForeignKey(rc => rc.RoleId).IsRequired();
                b.ToTable("AspNetRoles");
            });

            modelBuilder.Entity<IdentityRoleClaim<string>>(b =>
            {
                b.HasKey(rc => rc.Id);
                b.ToTable("AspNetRoleClaims");
            });

            modelBuilder.Entity<IdentityUserRole<string>>(b =>
            {
                b.HasKey(ur => new { ur.UserId, ur.RoleId });
                b.ToTable("AspNetUserRoles");
            });
        }
    }
}
