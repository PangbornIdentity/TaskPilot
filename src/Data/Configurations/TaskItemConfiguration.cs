using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskPilot.Entities;
using TaskPilot.Models.Enums;

namespace TaskPilot.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.TaskTypeId).IsRequired();
        builder.Property(t => t.Area).HasConversion<int>().HasDefaultValue(Area.Personal);
        builder.Property(t => t.LastModifiedBy).IsRequired();

        builder.Property(t => t.Priority).HasConversion<int>();
        builder.Property(t => t.Status).HasConversion<int>();
        builder.Property(t => t.TargetDateType).HasConversion<int>();
        builder.Property(t => t.RecurrencePattern).HasConversion<int?>();

        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasMany(t => t.TaskTags)
            .WithOne(tt => tt.Task)
            .HasForeignKey(tt => tt.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.ActivityLogs)
            .WithOne(a => a.Task)
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.TargetDate);
        builder.HasIndex(t => t.IsDeleted);
        builder.HasIndex(t => new { t.UserId, t.Status });
        builder.HasIndex(t => new { t.UserId, t.IsDeleted });
    }
}
