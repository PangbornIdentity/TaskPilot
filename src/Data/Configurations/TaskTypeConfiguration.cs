using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskPilot.Entities;

namespace TaskPilot.Data.Configurations;

public class TaskTypeConfiguration : IEntityTypeConfiguration<TaskType>
{
    public void Configure(EntityTypeBuilder<TaskType> builder)
    {
        builder.ToTable("TaskTypes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
        builder.Property(t => t.SortOrder);
        builder.Property(t => t.IsActive).HasDefaultValue(true);

        builder.HasMany(t => t.Tasks)
            .WithOne(ti => ti.TaskType)
            .HasForeignKey(ti => ti.TaskTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new TaskType { Id = 1, Name = "Task",    SortOrder = 1, IsActive = true },
            new TaskType { Id = 2, Name = "Goal",    SortOrder = 2, IsActive = true },
            new TaskType { Id = 3, Name = "Habit",   SortOrder = 3, IsActive = true },
            new TaskType { Id = 4, Name = "Meeting", SortOrder = 4, IsActive = true },
            new TaskType { Id = 5, Name = "Note",    SortOrder = 5, IsActive = true },
            new TaskType { Id = 6, Name = "Event",   SortOrder = 6, IsActive = true }
        );
    }
}
