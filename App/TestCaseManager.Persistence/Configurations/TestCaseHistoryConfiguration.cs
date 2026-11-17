using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence.Configurations;

/// <summary>
/// Datenbank-Konfiguration für <see cref="TestCaseHistory"/>.
/// </summary>
public class TestCaseHistoryConfiguration : IEntityTypeConfiguration<TestCaseHistory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TestCaseHistory> builder)
    {
        builder.ToTable("TestCaseHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.ChangedBy)
            .IsRequired()
            .HasMaxLength(AuditableEntityConfiguration.MaxUserNameLength);

        builder.Property(h => h.PropertyName)
            .HasMaxLength(300);

        // Abruf der Historie eines Testfalls chronologisch absteigend.
        builder.HasIndex(h => new { h.TestCaseId, h.ChangedAt });
    }
}
