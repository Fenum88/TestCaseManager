using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence.Configurations;

/// <summary>
/// Datenbank-Konfiguration für <see cref="CompareProfile"/>.
/// Der Profilname ist der eindeutige fachliche Schlüssel.
/// </summary>
public class CompareProfileConfiguration : IEntityTypeConfiguration<CompareProfile>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CompareProfile> builder)
    {
        builder.ToTable("CompareProfiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.ConfigureAuditFields();

        // Profilname systemweit eindeutig - er identifiziert das Profil
        // fachlich (auch beim CSV-Import, der über den Namen zusammenführt).
        builder.HasIndex(p => p.Name).IsUnique();

        builder.HasMany(p => p.Rules)
            .WithOne(r => r.CompareProfile)
            .HasForeignKey(r => r.CompareProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Datenbank-Konfiguration für <see cref="CompareProfileRule"/>.</summary>
public class CompareProfileRuleConfiguration : IEntityTypeConfiguration<CompareProfileRule>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CompareProfileRule> builder)
    {
        builder.ToTable("CompareProfileRules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Entity)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.KeyColumns)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasIndex(r => r.CompareProfileId);
    }
}
