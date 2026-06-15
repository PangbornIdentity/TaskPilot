using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using TaskPilot.Data;
using TaskPilot.Entities;

namespace TaskPilot.Tests.Unit.Helpers;

/// <summary>
/// Creates an <see cref="ApplicationDbContext"/> backed by a unique in-memory database
/// for unit tests.
///
/// WHY it does not call ApplicationDbContext.OnModelCreating directly:
///
/// (1) IDENTITY PASSKEYS: IdentityDbContext.OnModelCreating (called via base.OnModelCreating)
///     registers passkey entity types introduced in .NET 9/10 that cannot be resolved without
///     a full Identity assembly wiring — it would throw in unit-test isolation.
///
/// (2) HasDefaultValue ON ENUM WITH HasConversion&lt;int&gt;: EF Core 10 throws when a property
///     declares both HasConversion&lt;int&gt;() and HasDefaultValue(enumValue) on the in-memory
///     provider. TaskItemConfiguration sets HasDefaultValue(Area.Personal) on the Area property.
///
/// The workaround builds the model via a throw-away ModelBuilderContext that:
///   - ignores all Identity types (not needed for unit tests)
///   - calls ApplyConfigurationsFromAssembly so Tag, ApiKey, TaskItem query filters and
///     indexes are IDENTICAL to production
///   - removes only the problematic HasDefaultValue annotation on TaskItem.Area
///
/// The built IModel is cached and injected into every test ApplicationDbContext via
/// DbContextOptionsBuilder.UseModel so that OnModelCreating is skipped entirely.
/// </summary>
public static class TestDbContextFactory
{
    private static IModel? _cachedModel;
    private static readonly object _lock = new();

    public static ApplicationDbContext Create()
    {
        var model = GetOrBuildModel();
        var dbName = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .UseModel(model)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IModel GetOrBuildModel()
    {
        if (_cachedModel is not null) return _cachedModel;
        lock (_lock)
        {
            if (_cachedModel is not null) return _cachedModel;

            using var ctx = new ModelBuilderContext();
            _cachedModel = ctx.GetModel();
            return _cachedModel;
        }
    }

    /// <summary>
    /// Minimal DbContext used only to construct the EF model.
    /// Applies the production IEntityTypeConfiguration classes so constraints and
    /// query filters match production, then removes the one annotation that the
    /// in-memory provider cannot handle.
    /// </summary>
    private sealed class ModelBuilderContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseInMemoryDatabase("_model_build_");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ignore all Identity entity types — they are not needed for unit tests
            // and their passkey entities throw during model build.
            modelBuilder.Ignore<IdentityUser>();
            modelBuilder.Ignore<IdentityRole>();
            modelBuilder.Ignore<IdentityUserClaim<string>>();
            modelBuilder.Ignore<IdentityUserLogin<string>>();
            modelBuilder.Ignore<IdentityUserRole<string>>();
            modelBuilder.Ignore<IdentityUserToken<string>>();
            modelBuilder.Ignore<IdentityRoleClaim<string>>();

            // Ignore newer Identity types that appeared in .NET 9/10
            var identityAssembly = typeof(IdentityUser).Assembly;
            foreach (var type in identityAssembly.GetTypes()
                .Where(t => t.Namespace?.StartsWith("Microsoft.AspNetCore.Identity") == true
                            && t.IsClass && !t.IsAbstract))
            {
                try { modelBuilder.Ignore(type); } catch { /* best-effort */ }
            }

            // Apply all production IEntityTypeConfiguration classes so Tag, ApiKey,
            // TaskItem query filters, unique indexes, and FK cascade rules are identical
            // to production. This is the single source of truth for the model shape.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Remove the HasDefaultValue annotation on TaskItem.Area that EF Core 10
            // rejects on the in-memory provider when combined with HasConversion<int>().
            // This is the only justified residual override — tests observe Area == 0
            // (Area.Personal) by default, matching the production annotation's intent.
            var areaProperty = modelBuilder.Entity<TaskItem>()
                .Metadata.FindProperty(nameof(TaskItem.Area));
            areaProperty?.SetDefaultValue(null);
        }

        public IModel GetModel() => this.Model;
    }
}
