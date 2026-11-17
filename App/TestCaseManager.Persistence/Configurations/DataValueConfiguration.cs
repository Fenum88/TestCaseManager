using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence.Configurations;

/// <summary>
/// Datenbank-Konfiguration für <see cref="DataValue"/> (eine „Zelle",
/// d. h. Spalte=Wert einer Zeile).
/// </summary>
public class DataValueConfiguration : IEntityTypeConfiguration<DataValue>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DataValue> builder)
    {
        builder.ToTable("DataValues");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.ColumnName)
            .IsRequired()
            .HasMaxLength(200);

        // Werte können frei lang sein (z. B. JSON-Inhalte); keine Längenbegrenzung.
        builder.Property(v => v.Value);

        builder.HasIndex(v => v.DataRowId);
    }
}
