using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence.Configurations;

/// <summary>
/// Datenbank-Konfiguration für <see cref="ImportedTestCase"/>.
/// Die Testfallnummer ist der eindeutige fachliche Schlüssel.
/// </summary>
public class ImportedTestCaseConfiguration : IEntityTypeConfiguration<ImportedTestCase>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ImportedTestCase> builder)
    {
        builder.ToTable("ImportedTestCases");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Number)
            .IsRequired()
            .HasMaxLength(50);

        builder.ConfigureAuditFields();

        // Testfallnummer systemweit eindeutig (Upsert-Schlüssel).
        builder.HasIndex(t => t.Number).IsUnique();

        builder.HasMany(t => t.Files)
            .WithOne(f => f.ImportedTestCase)
            .HasForeignKey(f => f.ImportedTestCaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
