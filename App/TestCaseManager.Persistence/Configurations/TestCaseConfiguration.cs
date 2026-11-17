using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TestCaseManager.Application.Validation;
using TestCaseManager.Domain.Entities;
using TestCaseManager.Persistence;

namespace TestCaseManager.Persistence.Configurations;

/// <summary>
/// Datenbank-Konfiguration für <see cref="TestCase"/>:
/// Tabellenname, Pflichtfelder, Maximallängen, Indizes und alle
/// Beziehungen zu untergeordneten Entitäten.
/// </summary>
public class TestCaseConfiguration : IEntityTypeConfiguration<TestCase>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TestCase> builder)
    {
        builder.ToTable("TestCases");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(TestCaseValidator.MaxNameLength);

        builder.Property(t => t.Description)
            .HasMaxLength(TestCaseValidator.MaxDescriptionLength);

        builder.Property(t => t.Version)
            .IsRequired()
            .HasMaxLength(TestCaseValidator.MaxVersionLength);

        builder.ConfigureAuditFields();

        // Doppelte Namen bereits auf Datenbankebene verhindern.
        builder.HasIndex(t => t.Name).IsUnique();

        // Indizes für häufige Filter- und Sortierkriterien.
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.CategoryId);
        builder.HasIndex(t => t.ModifiedAt);

        // 1:n – Kategorie bleibt beim Löschen eines Testfalls bestehen;
        // beim Löschen einer Kategorie werden Testfälle entkoppelt.
        builder.HasOne(t => t.Category)
            .WithMany(c => c.TestCases)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // 1:n – untergeordnete Elemente hängen am Testfall (Kaskadenlöschung).
        builder.HasMany(t => t.Parameters)
            .WithOne(p => p.TestCase)
            .HasForeignKey(p => p.TestCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.ExpectedResults)
            .WithOne(r => r.TestCase)
            .HasForeignKey(r => r.TestCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Attachments)
            .WithOne(a => a.TestCase)
            .HasForeignKey(a => a.TestCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.History)
            .WithOne(h => h.TestCase)
            .HasForeignKey(h => h.TestCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // n:m – Tags über eine explizit benannte Verknüpfungstabelle.
        builder.HasMany(t => t.Tags)
            .WithMany(tag => tag.TestCases)
            .UsingEntity<Dictionary<string, object>>(
                TestCaseManagerDbContext.TestCaseTagJoinEntity,
                right => right.HasOne<Tag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<TestCase>().WithMany().HasForeignKey("TestCaseId").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("TestCaseTags");
                    join.HasKey("TestCaseId", "TagId");
                    join.HasIndex("TagId");
                });
    }
}
