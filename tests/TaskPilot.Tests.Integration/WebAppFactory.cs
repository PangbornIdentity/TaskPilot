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
            // OnModelCreating to omit the HasDefaultValue(0) call that EF10 rejects
            // on enum properties with HasConversion<int>().
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
    // Its OnModelCreating configures everything Identity + TaskPilot needs without
    // the HasDefaultValue(0) call that EF10 rejects on enum properties.
    // ──────────────────────────────────────────────────────────────────────────────

    private sealed class PatchedApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Identity entities manually (bypasses IdentityDbContext.OnModelCreating
            // which would ultimately chain back through our override).
            ConfigureIdentity(modelBuilder);

            // Configure TaskPilot entities — without the broken HasDefaultValue(0)
            modelBuilder.Entity<TaskItem>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property(t => t.Title).IsRequired().HasMaxLength(200);
                b.Property(t => t.UserId).IsRequired();
                b.Property(t => t.LastModifiedBy).IsRequired();
                b.Property(t => t.Area).HasConversion<int>();       // no HasDefaultValue
                b.Property(t => t.Priority).HasConversion<int>();
                b.Property(t => t.Status).HasConversion<int>();
                b.Property(t => t.TargetDateType).HasConversion<int>();
                b.Property(t => t.RecurrencePattern).HasConversion<int?>();
                b.HasQueryFilter(t => !t.IsDeleted);
                b.HasIndex(t => t.UserId);
                b.HasIndex(t => t.Status);
                b.HasIndex(t => t.Priority);
                b.HasIndex(t => t.TargetDate);
                b.HasIndex(t => t.IsDeleted);
                b.HasIndex(t => new { t.UserId, t.Status });
                b.HasIndex(t => new { t.UserId, t.IsDeleted });

                b.HasMany(t => t.TaskTags)
                    .WithOne(tt => tt.Task)
                    .HasForeignKey(tt => tt.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasMany(t => t.ActivityLogs)
                    .WithOne(a => a.Task)
                    .HasForeignKey(a => a.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Tag>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property(t => t.Name).IsRequired().HasMaxLength(50);
                b.Property(t => t.Color).IsRequired().HasMaxLength(7);
                b.Property(t => t.UserId).IsRequired();
                b.HasIndex(t => new { t.UserId, t.Name }).IsUnique();
            });

            modelBuilder.Entity<TaskTag>(b =>
            {
                b.HasKey(tt => new { tt.TaskId, tt.TagId });
                b.HasOne(tt => tt.Tag)
                    .WithMany(t => t.TaskTags)
                    .HasForeignKey(tt => tt.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TaskType>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property(t => t.Id).ValueGeneratedOnAdd();
                b.Property(t => t.Name).IsRequired().HasMaxLength(50);
                b.HasMany(t => t.Tasks)
                    .WithOne(ti => ti.TaskType)
                    .HasForeignKey(ti => ti.TaskTypeId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<TaskActivityLog>(b =>
            {
                b.HasKey(a => a.Id);
                b.Property(a => a.ChangedBy).IsRequired();
            });

            modelBuilder.Entity<ApiKey>(b =>
            {
                b.HasKey(k => k.Id);
                b.Property(k => k.Name).IsRequired().HasMaxLength(100);
                b.Property(k => k.KeyHash).IsRequired();
                b.Property(k => k.KeyPrefix).IsRequired().HasMaxLength(8);
                b.Property(k => k.UserId).IsRequired();
                b.HasIndex(k => new { k.UserId, k.Name }).IsUnique();
            });

            modelBuilder.Entity<ApiAuditLog>(b =>
            {
                b.HasKey(a => a.Id);
            });
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
