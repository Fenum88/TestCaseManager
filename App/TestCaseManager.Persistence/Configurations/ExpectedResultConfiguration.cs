using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TestCaseManager.Application.Validation;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence.Configurations;

/// <summary>
/// Datenbank-Konfiguration für <see cref="ExpectedResult"/>.
/// </summary>
public class ExpectedResultConfiguration : IEntityTypeConfiguration<ExpectedResult>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExpectedResult> builder)
    {
        builder.ToTable("ExpectedResults");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(TestCaseValidator.MaxItemNameLength);

        builder.Property(r => r.ExpectedValue)
            .IsRequired();

        builder.Property(r => r.Tolerance)
            .HasMaxLength(50);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.HasIndex(r => r.TestCaseId);
    }
}
