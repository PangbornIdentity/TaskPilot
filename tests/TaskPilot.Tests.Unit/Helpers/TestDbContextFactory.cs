using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using TaskPilot.Data;
using TaskPilot.Entities;

namespace TaskPilot.Tests.Unit.Helpers;

/// <summary>
/// Creates an <see cref="ApplicationDbContext"/> backed by a unique in-memory database
/// for unit tests, working around the EF10 incompatibility where
/// <c>HasDefaultValue(0)</c> on an enum property with <c>HasConversion&lt;int&gt;()</c>
/// throws an <see cref="InvalidOperationException"/>.
///
/// The workaround builds a clean model via a throw-away builder context, injecting
/// that model via <c>DbContextOptionsBuilder.UseModel</c> so that the real
/// <c>ApplicationDbContext</c> instances skip <c>OnModelCreating</c> entirely.
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
    /// Minimal DbContext used only to construct the EF model without any
    /// relational HasDefaultValue annotations.
    /// </summary>
    private sealed class ModelBuilderContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseInMemoryDatabase("_model_build_");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ignore all Identity entity types — they are not needed for unit tests
            modelBuilder.Ignore<IdentityUser>();
            modelBuilder.Ignore<IdentityRole>();
            modelBuilder.Ignore<IdentityUserClaim<string>>();
            modelBuilder.Ignore<IdentityUserLogin<string>>();
            modelBuilder.Ignore<IdentityUserToken<string>>();
            modelBuilder.Ignore<IdentityUserRole<string>>();
            modelBuilder.Ignore<IdentityRoleClaim<string>>();

            // Ignore newer Identity types that appeared in .NET 9/10
            var identityAssembly = typeof(IdentityUser).Assembly;
            foreach (var type in identityAssembly.GetTypes()
                .Where(t => t.Namespace?.StartsWith("Microsoft.AspNetCore.Identity") == true
                            && t.IsClass && !t.IsAbstract))
            {
                try { modelBuilder.Ignore(type); } catch { /* best-effort */ }
            }

            // Configure only what unit tests need
            modelBuilder.Entity<TaskItem>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property(t => t.Title).IsRequired().HasMaxLength(200);
                b.Property(t => t.UserId).IsRequired();
                b.Property(t => t.LastModifiedBy).IsRequired();
                b.Property(t => t.Area).HasConversion<int>();
                b.Property(t => t.Priority).HasConversion<int>();
                b.Property(t => t.Status).HasConversion<int>();
                b.Property(t => t.TargetDateType).HasConversion<int>();
                b.Property(t => t.RecurrencePattern).HasConversion<int?>();
                b.HasQueryFilter(t => !t.IsDeleted);

                b.HasMany(t => t.TaskTags)
                    .WithOne(tt => tt.Task)
                    .HasForeignKey(tt => tt.TaskId);

                b.HasMany(t => t.ActivityLogs)
                    .WithOne(a => a.Task)
                    .HasForeignKey(a => a.TaskId);
            });

            modelBuilder.Entity<Tag>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property(t => t.Name).IsRequired().HasMaxLength(50);
                b.Property(t => t.Color).IsRequired().HasMaxLength(7);
                b.Property(t => t.UserId).IsRequired();
            });

            modelBuilder.Entity<TaskTag>(b =>
            {
                b.HasKey(tt => new { tt.TaskId, tt.TagId });
                b.HasOne(tt => tt.Tag)
                    .WithMany(t => t.TaskTags)
                    .HasForeignKey(tt => tt.TagId);
            });

            modelBuilder.Entity<TaskType>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property(t => t.Name).IsRequired().HasMaxLength(50);
                b.HasMany(t => t.Tasks)
                    .WithOne(ti => ti.TaskType)
                    .HasForeignKey(ti => ti.TaskTypeId);
            });

            modelBuilder.Entity<TaskActivityLog>(b =>
            {
                b.HasKey(a => a.Id);
            });

            modelBuilder.Entity<ApiKey>(b =>
            {
                b.HasKey(k => k.Id);
            });

            modelBuilder.Entity<ApiAuditLog>(b =>
            {
                b.HasKey(a => a.Id);
            });
        }

        public IModel GetModel() => this.Model;
    }
}
