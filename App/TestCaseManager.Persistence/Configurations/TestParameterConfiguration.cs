using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TestCaseManager.Application.Validation;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence.Configurations;

/// <summary>
/// Datenbank-Konfiguration für <see cref="TestParameter"/>.
/// </summary>
public class TestParameterConfiguration : IEntityTypeConfiguration<TestParameter>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TestParameter> builder)
    {
        builder.ToTable("TestParameters");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(TestCaseValidator.MaxItemNameLength);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        // Parametername muss innerhalb eines Testfalls eindeutig sein.
        builder.HasIndex(p => new { p.TestCaseId, p.Name }).IsUnique();
    }
}
