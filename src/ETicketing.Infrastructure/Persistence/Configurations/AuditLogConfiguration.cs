using ETicketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ETicketing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps AuditLogs, indexed by entity so the history of one record is cheap to read.
/// </summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(log => log.Id);
        builder.Property(log => log.EntityName).HasMaxLength(50).IsRequired();
        builder.Property(log => log.EntityId).HasMaxLength(50).IsRequired();
        builder.Property(log => log.FromStatus).HasMaxLength(20);
        builder.Property(log => log.ToStatus).HasMaxLength(20).IsRequired();
        builder.HasIndex(log => new { log.EntityName, log.EntityId });
    }
}
