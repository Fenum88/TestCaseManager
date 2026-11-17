using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TestCaseManager.Domain.Common;

namespace TestCaseManager.Persistence.Configurations;

/// <summary>
/// Gemeinsame Konfiguration der Auditfelder für alle Entitäten,
/// die von <see cref="AuditableEntity"/> erben.
/// </summary>
internal static class AuditableEntityConfiguration
{
    /// <summary>Maximale Länge von Benutzernamen in Auditfeldern.</summary>
    internal const int MaxUserNameLength = 100;

    /// <summary>
    /// Konfiguriert die Auditfelder (Pflichtfelder, Maximallängen).
    /// </summary>
    /// <typeparam name="TEntity">Der zu konfigurierende Entitätstyp.</typeparam>
    /// <param name="builder">Der EntityTypeBuilder der Entität.</param>
    internal static void ConfigureAuditFields<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity
    {
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy).IsRequired().HasMaxLength(MaxUserNameLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(MaxUserNameLength);
    }
}
