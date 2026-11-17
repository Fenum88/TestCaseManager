using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence.Configurations;

/// <summary>
/// Datenbank-Konfiguration für <see cref="DataEntity"/> (eine „Tabelle"
/// innerhalb einer Testfalldatei).
/// </summary>
public class DataEntityConfiguration : IEntityTypeConfiguration<DataEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DataEntity> builder)
    {
        builder.ToTable("DataEntities");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(e => e.TestCaseFileId);

        builder.HasMany(e => e.Rows)
            .WithOne(r => r.DataEntity)
            .HasForeignKey(r => r.DataEntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
