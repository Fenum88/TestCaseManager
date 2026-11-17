using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence.Configurations;

/// <summary>
/// Datenbank-Konfiguration für <see cref="TestCaseFile"/>.
/// </summary>
public class TestCaseFileConfiguration : IEntityTypeConfiguration<TestCaseFile>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TestCaseFile> builder)
    {
        builder.ToTable("TestCaseFiles");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.FileNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.Designation)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(f => f.ImportedTestCaseId);

        builder.HasMany(f => f.Entities)
            .WithOne(e => e.TestCaseFile)
            .HasForeignKey(e => e.TestCaseFileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
