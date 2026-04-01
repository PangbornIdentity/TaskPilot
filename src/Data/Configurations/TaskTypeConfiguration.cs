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
            .OnDelete(DeleteBehavior.SetNull);
    }
}
