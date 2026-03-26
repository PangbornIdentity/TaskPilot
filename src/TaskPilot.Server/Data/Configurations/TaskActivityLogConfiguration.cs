using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskPilot.Server.Entities;

namespace TaskPilot.Server.Data.Configurations;

public class TaskActivityLogConfiguration : IEntityTypeConfiguration<TaskActivityLog>
{
    public void Configure(EntityTypeBuilder<TaskActivityLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        builder.Property(a => a.FieldChanged).IsRequired().HasMaxLength(100);
        builder.Property(a => a.ChangedBy).IsRequired();

        builder.HasIndex(a => a.TaskId);
        builder.HasIndex(a => a.Timestamp);
    }
}
