using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskPilot.Entities;

namespace TaskPilot.Data.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.HasKey(k => k.Id);

        builder.Property(k => k.Name).IsRequired().HasMaxLength(100);
        builder.Property(k => k.KeyHash).IsRequired();
        builder.Property(k => k.KeyPrefix).IsRequired().HasMaxLength(8);
        builder.Property(k => k.UserId).IsRequired();
        builder.Property(k => k.LastModifiedBy).IsRequired();

        builder.HasIndex(k => k.UserId);
        builder.HasIndex(k => k.KeyHash).IsUnique();

        builder.HasMany(k => k.AuditLogs)
            .WithOne(a => a.ApiKey)
            .HasForeignKey(a => a.ApiKeyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
