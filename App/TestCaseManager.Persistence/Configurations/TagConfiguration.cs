using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence.Configurations;

/// <summary>
/// Datenbank-Konfiguration für <see cref="Tag"/>.
/// </summary>
public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(50);

        // Tag-Namen sind systemweit eindeutig.
        builder.HasIndex(t => t.Name).IsUnique();
    }
}
