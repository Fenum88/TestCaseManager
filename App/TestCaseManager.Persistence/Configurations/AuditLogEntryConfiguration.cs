using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence.Configurations;

/// <summary>
/// Datenbank-Konfiguration für <see cref="AuditLogEntry"/>.
/// </summary>
public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLog");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserName)
            .IsRequired()
            .HasMaxLength(AuditableEntityConfiguration.MaxUserNameLength);

        builder.Property(a => a.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Details)
            .HasMaxLength(500);

        // Neueste Einträge zuerst – Index für das Dashboard.
        builder.HasIndex(a => a.Timestamp);
    }
}
