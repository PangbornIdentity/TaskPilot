using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskPilot.Entities;

namespace TaskPilot.Data.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Color).IsRequired().HasMaxLength(7);
        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.LastModifiedBy).IsRequired();

        builder.HasIndex(t => new { t.UserId, t.Name }).IsUnique();

        builder.HasMany(t => t.TaskTags)
            .WithOne(tt => tt.Tag)
            .HasForeignKey(tt => tt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
