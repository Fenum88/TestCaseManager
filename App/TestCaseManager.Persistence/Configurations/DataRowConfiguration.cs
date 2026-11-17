using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence.Configurations;

/// <summary>
/// Datenbank-Konfiguration für <see cref="DataRow"/> (eine Zeile einer Entity).
/// </summary>
public class DataRowConfiguration : IEntityTypeConfiguration<DataRow>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DataRow> builder)
    {
        builder.ToTable("DataRows");

        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.DataEntityId);

        builder.HasMany(r => r.Values)
            .WithOne(v => v.DataRow)
            .HasForeignKey(v => v.DataRowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
