using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskPilot.Entities;

namespace TaskPilot.Data.Configurations;

public class ApiAuditLogConfiguration : IEntityTypeConfiguration<ApiAuditLog>
{
    public void Configure(EntityTypeBuilder<ApiAuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ApiKeyName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.HttpMethod).IsRequired().HasMaxLength(10);
        builder.Property(a => a.Endpoint).IsRequired().HasMaxLength(500);
        builder.Property(a => a.RequestBodyHash).IsRequired();
        builder.Property(a => a.UserId).IsRequired();

        builder.HasIndex(a => a.ApiKeyId);
        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => new { a.UserId, a.Timestamp });
    }
}
