using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Server.AspNetCore;
using TaskPilot.Data;
using TaskPilot.Entities;

namespace TaskPilot.Tests.Integration.OAuth;

/// <summary>
/// A dedicated <see cref="WebApplicationFactory{TEntryPoint}"/> for OAuth integration tests.
///
/// Differences from the shared <see cref="TaskPilotWebAppFactory"/>:
///
///  1. The patched <see cref="ApplicationDbContext"/> subclass here also calls
///     <c>builder.UseOpenIddict()</c> during <c>OnModelCreating</c>, which registers
///     the four OpenIddict entity sets (Applications, Authorizations, Scopes, Tokens).
///     <c>EnsureCreatedAsync</c> will therefore create those tables in the test SQLite
///     database, allowing the <see cref="TaskPilot.Extensions.OAuthSeedWorker"/> and the
///     full PKCE / token-exchange flow to work end-to-end.
///
///  2. <c>OAuth:BaseUrl</c> is pinned to <c>http://localhost</c> so the OpenIddict
///     Authorization Server starts with a deterministic issuer in the test environment.
///
///  3. Every other override is identical to <see cref="TaskPilotWebAppFactory"/>
///     (passkey entity suppression, HasDefaultValue removal, Hmac:SecretKey).
///
///  NOTE: The <c>NoOpAntiforgery</c> shim is intentionally NOT registered here.
///  The consent page now correctly emits <c>@Html.AntiForgeryToken()</c> and the
///  test helper <c>ObtainBearerTokenAsync</c> extracts the token from the rendered
///  HTML and carries the antiforgery cookie via the <c>HandleCookies = true</c>
///  HttpClient, exercising the real antiforgery pipeline.
/// </summary>
[CollectionDefinition("OAuthIntegration")]
public class OAuthIntegrationCollection : ICollectionFixture<OAuthWebAppFactory> { }

public class OAuthWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private string? _dbPath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"taskpilot_oauth_test_{Guid.NewGuid():N}.db");
        var localDbPath = _dbPath!;

        builder.ConfigureServices(services =>
        {
            // Remove the default DbContextOptions<ApplicationDbContext> registration.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Re-register with SQLite + UseOpenIddict() so OpenIddict tables are
            // created by EnsureCreatedAsync during app startup.
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite($"Data Source={localDbPath}");
                options.UseOpenIddict();   // ← critical: maps OpenIddict entity sets
            },
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Scoped);

            // Post-configure the OpenIddict ASP.NET Core integration to allow HTTP in tests.
            // The WebApplicationFactory test server uses http://localhost; OpenIddict's default
            // requires HTTPS (error ID2083). Setting DisableTransportSecurityRequirement=true
            // on OpenIddictServerAspNetCoreOptions bypasses this check for test requests only.
            services.PostConfigure<OpenIddictServerAspNetCoreOptions>(options =>
            {
                options.DisableTransportSecurityRequirement = true;
            });

            // Replace ApplicationDbContext with the OAuth-aware patched subclass.
            var appDbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ApplicationDbContext));
            if (appDbDescriptor != null) services.Remove(appDbDescriptor);

            services.AddScoped<ApplicationDbContext>(sp =>
            {
                var opts = sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
                return new OAuthPatchedApplicationDbContext(opts);
            });
        });

        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hmac:SecretKey"] = "test-secret-key-for-oauth-integration-tests",
                // Pin the issuer so OpenIddict AS starts deterministically.
                // The WebApplicationFactory test server uses http://localhost by default.
                ["OAuth:BaseUrl"] = "http://localhost"
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

    // ──────────────────────────────────────────────────────────────────────────
    // Patched DbContext for OAuth tests.
    //
    // Extends the standard patched pattern with ONE addition:
    //   builder.UseOpenIddict() is called AFTER ApplyConfigurationsFromAssembly
    //   so that the four OpenIddict entity sets (OpenIddictApplications, etc.)
    //   are registered and EnsureCreatedAsync creates their tables.
    //
    // All other overrides (passkey suppression, HasDefaultValue removal) are
    // carried forward unchanged from TaskPilotWebAppFactory.PatchedApplicationDbContext.
    // ──────────────────────────────────────────────────────────────────────────
    private sealed class OAuthPatchedApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Step 1: Configure Identity tables without passkey entity discovery.
            ConfigureIdentity(modelBuilder);

            // Step 2: Apply every production IEntityTypeConfiguration.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Step 3: Register OpenIddict entity sets (Applications, Authorizations, Scopes, Tokens).
            // This is the key addition over TaskPilotWebAppFactory.PatchedApplicationDbContext —
            // without it, EnsureCreatedAsync would not create the OpenIddict tables and
            // OAuthSeedWorker / token issuance would fail.
            modelBuilder.UseOpenIddict();

            // Step 4: Remove the HasDefaultValue annotation on TaskItem.Area that EF10
            // rejects on a SQLite provider when combined with HasConversion<int>().
            var areaProperty = modelBuilder.Entity<TaskItem>()
                .Metadata.FindProperty(nameof(TaskItem.Area));
            areaProperty?.SetDefaultValue(null);
        }

        private static void ConfigureIdentity(ModelBuilder modelBuilder)
        {
            // Suppress EF10 passkey entity types introduced in .NET 9/10.
            var identityAssembly = typeof(IdentityUser).Assembly;
            var efAssembly = typeof(IdentityDbContext).Assembly;
            foreach (var asm in new[] { identityAssembly, efAssembly })
            {
                foreach (var type in asm.GetExportedTypes()
                    .Where(t => t.Name.Contains("Passkey", StringComparison.OrdinalIgnoreCase)
                             && t.IsClass && !t.IsAbstract))
                {
                    try { modelBuilder.Ignore(type); } catch { /* best effort */ }
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
