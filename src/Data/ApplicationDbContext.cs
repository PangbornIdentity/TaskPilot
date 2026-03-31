using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskPilot.Entities;

namespace TaskPilot.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TaskTag> TaskTags => Set<TaskTag>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<ApiAuditLog> ApiAuditLogs => Set<ApiAuditLog>();
    public DbSet<TaskActivityLog> TaskActivityLogs => Set<TaskActivityLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = now;
                    entry.Entity.LastModifiedDate = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedDate = now;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
