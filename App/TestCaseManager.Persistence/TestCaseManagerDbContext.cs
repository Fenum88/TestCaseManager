using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Domain.Common;
using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Persistence;

/// <summary>
/// Entity-Framework-Core-Kontext der Anwendung (PostgreSQL via Npgsql).
/// Neben dem reinen Datenzugriff übernimmt der Kontext zwei
/// Querschnittsaufgaben automatisch bei jedem Speichern:
/// <list type="bullet">
/// <item>Füllen der Auditfelder (<c>CreatedAt/By</c>, <c>ModifiedAt/By</c>).</item>
/// <item>Schreiben von Änderungshistorie (<see cref="TestCaseHistory"/>)
/// und Audit-Log (<see cref="AuditLogEntry"/>) mit alten und neuen Werten.</item>
/// </list>
/// Dadurch kann keine Codestelle das Protokollieren "vergessen".
/// </summary>
public class TestCaseManagerDbContext : DbContext
{
    /// <summary>Name der impliziten Verknüpfungsentität für Testfall-Tags.</summary>
    internal const string TestCaseTagJoinEntity = "TestCaseTag";

    private readonly ICurrentUserService? _currentUserService;
    private readonly IDateTimeProvider? _dateTimeProvider;

    /// <summary>
    /// Erzeugt den Kontext. Benutzer- und Zeitdienst sind optional, damit
    /// der Kontext auch zur Designzeit (Migrationen) erzeugt werden kann.
    /// </summary>
    /// <param name="options">EF-Core-Optionen (Provider, Verbindung).</param>
    /// <param name="currentUserService">Liefert den aktuellen Benutzer für Audit-Zwecke.</param>
    /// <param name="dateTimeProvider">Liefert die aktuelle UTC-Zeit.</param>
    public TestCaseManagerDbContext(DbContextOptions<TestCaseManagerDbContext> options,
        ICurrentUserService? currentUserService = null,
        IDateTimeProvider? dateTimeProvider = null)
        : base(options)
    {
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <summary>Alle Testfälle.</summary>
    public DbSet<TestCase> TestCases => Set<TestCase>();

    /// <summary>Alle Eingabeparameter.</summary>
    public DbSet<TestParameter> TestParameters => Set<TestParameter>();

    /// <summary>Alle erwarteten Ergebnisse.</summary>
    public DbSet<ExpectedResult> ExpectedResults => Set<ExpectedResult>();

    /// <summary>Alle Kategorien.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Alle Tags.</summary>
    public DbSet<Tag> Tags => Set<Tag>();

    /// <summary>Alle Dateianhänge.</summary>
    public DbSet<Attachment> Attachments => Set<Attachment>();

    /// <summary>Änderungshistorie aller Testfälle.</summary>
    public DbSet<TestCaseHistory> TestCaseHistories => Set<TestCaseHistory>();

    /// <summary>Übergreifendes Audit-Log.</summary>
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    /// <summary>Datei-Testfälle (dateibasiertes Import-/Export-Subsystem).</summary>
    public DbSet<ImportedTestCase> ImportedTestCases => Set<ImportedTestCase>();

    /// <summary>Dateien der Datei-Testfälle.</summary>
    public DbSet<TestCaseFile> TestCaseFiles => Set<TestCaseFile>();

    /// <summary>Entities (Tabellen) innerhalb der Testfalldateien.</summary>
    public DbSet<DataEntity> DataEntities => Set<DataEntity>();

    /// <summary>Zeilen der Entities.</summary>
    public DbSet<DataRow> DataRows => Set<DataRow>();

    /// <summary>Werte (Zellen) der Zeilen.</summary>
    public DbSet<DataValue> DataValues => Set<DataValue>();

    /// <summary>Gemeinsam genutzte Abgleichprofile.</summary>
    public DbSet<CompareProfile> CompareProfiles => Set<CompareProfile>();

    /// <summary>Schlüsselregeln der Abgleichprofile.</summary>
    public DbSet<CompareProfileRule> CompareProfileRules => Set<CompareProfileRule>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Alle IEntityTypeConfiguration-Klassen dieser Assembly anwenden.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TestCaseManagerDbContext).Assembly);
    }

    /// <inheritdoc />
    public override int SaveChanges()
        => SaveChangesAsync().GetAwaiter().GetResult();

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider?.UtcNow ?? DateTime.UtcNow;
        var user = _currentUserService?.UserName ?? "System";

        ChangeTracker.DetectChanges();
        ApplyAuditFields(now, user);
        await WriteHistoryAndAuditLogAsync(now, user, cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Füllt die Auditfelder aller geänderten Entitäten und markiert den
    /// übergeordneten Testfall als geändert, wenn nur untergeordnete
    /// Elemente (Parameter, Ergebnisse, Anhänge, Tag-Zuordnungen)
    /// verändert wurden.
    /// </summary>
    private void ApplyAuditFields(DateTime now, string user)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = user;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = user;
                    // Erstellungsdaten dürfen nachträglich nie überschrieben werden.
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;
                    break;
            }
        }

        foreach (var testCaseId in CollectTestCaseIdsWithChangedChildren())
        {
            var parentEntry = ChangeTracker.Entries<TestCase>()
                .FirstOrDefault(e => e.Entity.Id == testCaseId);

            if (parentEntry is { State: EntityState.Unchanged })
            {
                parentEntry.Entity.ModifiedAt = now;
                parentEntry.Entity.ModifiedBy = user;
            }
        }
    }

    /// <summary>
    /// Ermittelt die Ids aller Testfälle, deren untergeordnete Elemente
    /// in dieser Arbeitseinheit verändert wurden.
    /// </summary>
    private HashSet<Guid> CollectTestCaseIdsWithChangedChildren()
    {
        var ids = new HashSet<Guid>();

        foreach (var entry in ChangeTracker.Entries()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            switch (entry.Entity)
            {
                case TestParameter p:
                    ids.Add(p.TestCaseId);
                    break;
                case ExpectedResult r:
                    ids.Add(r.TestCaseId);
                    break;
                case Attachment a:
                    ids.Add(a.TestCaseId);
                    break;
                default:
                    if (entry.Metadata.Name == TestCaseTagJoinEntity)
                    {
                        ids.Add(GetJoinKey(entry, "TestCaseId"));
                    }
                    break;
            }
        }

        return ids;
    }

    /// <summary>
    /// Erzeugt Historien- und Audit-Log-Einträge für alle in dieser
    /// Arbeitseinheit anstehenden Änderungen.
    /// </summary>
    private async Task WriteHistoryAndAuditLogAsync(DateTime now, string user, CancellationToken cancellationToken)
    {
        // Momentaufnahme, da beim Hinzufügen der Protokolleinträge neue
        // Einträge im ChangeTracker entstehen.
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        // Für gelöschte Testfälle darf keine Historie mehr geschrieben werden
        // (Fremdschlüsselziel entfällt); die Löschung landet im Audit-Log.
        var deletedTestCaseIds = entries
            .Where(e => e.Entity is TestCase && e.State == EntityState.Deleted)
            .Select(e => ((TestCase)e.Entity).Id)
            .ToHashSet();

        var history = new List<TestCaseHistory>();
        var auditLog = new List<AuditLogEntry>();

        foreach (var entry in entries)
        {
            if (entry.Metadata.Name == TestCaseTagJoinEntity)
            {
                await AppendTagAssignmentHistoryAsync(entry, deletedTestCaseIds, history, now, user, cancellationToken);
                continue;
            }

            switch (entry.Entity)
            {
                case TestCase testCase:
                    auditLog.Add(CreateAuditEntry(entry, "Testfall", testCase.Id, testCase.Name, now, user));
                    await AppendTestCaseHistoryAsync(entry, testCase, history, now, user, cancellationToken);
                    break;

                case TestParameter parameter when !deletedTestCaseIds.Contains(parameter.TestCaseId):
                    AppendChildHistory(entry, parameter.TestCaseId, $"Parameter '{parameter.Name}'",
                        TestParameterDiffProperties, history, now, user);
                    break;

                case ExpectedResult result when !deletedTestCaseIds.Contains(result.TestCaseId):
                    AppendChildHistory(entry, result.TestCaseId, $"Ergebnis '{result.Name}'",
                        ExpectedResultDiffProperties, history, now, user);
                    break;

                case Attachment attachment:
                    auditLog.Add(CreateAuditEntry(entry, "Anhang", attachment.Id, attachment.FileName, now, user));
                    if (!deletedTestCaseIds.Contains(attachment.TestCaseId))
                    {
                        AppendChildHistory(entry, attachment.TestCaseId, $"Anhang '{attachment.FileName}'",
                            AttachmentDiffProperties, history, now, user);
                    }
                    break;

                case Category category:
                    auditLog.Add(CreateAuditEntry(entry, "Kategorie", category.Id, category.Name, now, user));
                    break;

                case Tag tag:
                    auditLog.Add(CreateAuditEntry(entry, "Tag", tag.Id, tag.Name, now, user));
                    break;

                case ImportedTestCase imported:
                    auditLog.Add(CreateAuditEntry(entry, "Datei-Testfall", imported.Id, imported.Number, now, user));
                    break;
            }
        }

        TestCaseHistories.AddRange(history);
        AuditLogEntries.AddRange(auditLog);
    }

    /// <summary>Deutsche Anzeigenamen der protokollierten Testfall-Eigenschaften.</summary>
    private static readonly Dictionary<string, string> TestCaseDiffProperties = new()
    {
        [nameof(TestCase.Name)] = "Name",
        [nameof(TestCase.Description)] = "Beschreibung",
        [nameof(TestCase.Version)] = "Version",
        [nameof(TestCase.Status)] = "Status",
        [nameof(TestCase.CategoryId)] = "Kategorie"
    };

    /// <summary>Deutsche Anzeigenamen der protokollierten Parameter-Eigenschaften.</summary>
    private static readonly Dictionary<string, string> TestParameterDiffProperties = new()
    {
        [nameof(TestParameter.Name)] = "Name",
        [nameof(TestParameter.DataType)] = "Datentyp",
        [nameof(TestParameter.Value)] = "Wert",
        [nameof(TestParameter.DefaultValue)] = "Standardwert",
        [nameof(TestParameter.Description)] = "Beschreibung",
        [nameof(TestParameter.IsRequired)] = "Pflichtfeld"
    };

    /// <summary>Deutsche Anzeigenamen der protokollierten Ergebnis-Eigenschaften.</summary>
    private static readonly Dictionary<string, string> ExpectedResultDiffProperties = new()
    {
        [nameof(ExpectedResult.Name)] = "Name",
        [nameof(ExpectedResult.DataType)] = "Datentyp",
        [nameof(ExpectedResult.ExpectedValue)] = "Erwarteter Wert",
        [nameof(ExpectedResult.Tolerance)] = "Toleranz",
        [nameof(ExpectedResult.Description)] = "Beschreibung"
    };

    /// <summary>Deutsche Anzeigenamen der protokollierten Anhang-Eigenschaften.</summary>
    private static readonly Dictionary<string, string> AttachmentDiffProperties = new()
    {
        [nameof(Attachment.FileName)] = "Dateiname",
        [nameof(Attachment.Description)] = "Beschreibung"
    };

    /// <summary>
    /// Schreibt Historieneinträge für einen Testfall selbst:
    /// ein Eintrag beim Anlegen, je geänderter Eigenschaft ein Eintrag
    /// mit altem und neuem Wert beim Ändern.
    /// </summary>
    private async Task AppendTestCaseHistoryAsync(EntityEntry entry, TestCase testCase,
        List<TestCaseHistory> history, DateTime now, string user, CancellationToken cancellationToken)
    {
        if (entry.State == EntityState.Added)
        {
            history.Add(new TestCaseHistory
            {
                TestCaseId = testCase.Id,
                ChangeType = ChangeType.Created,
                ChangedBy = user,
                ChangedAt = now,
                NewValue = testCase.Name
            });
            return;
        }

        if (entry.State != EntityState.Modified)
        {
            return; // Löschungen landen ausschließlich im Audit-Log.
        }

        foreach (var property in entry.Properties.Where(p => p.IsModified))
        {
            if (!TestCaseDiffProperties.TryGetValue(property.Metadata.Name, out var displayName)
                || Equals(property.OriginalValue, property.CurrentValue))
            {
                continue;
            }

            string? oldValue;
            string? newValue;

            if (property.Metadata.Name == nameof(TestCase.CategoryId))
            {
                // Kategorie-Ids in lesbare Namen auflösen.
                oldValue = await ResolveCategoryNameAsync((Guid?)property.OriginalValue, cancellationToken);
                newValue = await ResolveCategoryNameAsync((Guid?)property.CurrentValue, cancellationToken);
            }
            else
            {
                oldValue = FormatValue(property.OriginalValue);
                newValue = FormatValue(property.CurrentValue);
            }

            history.Add(new TestCaseHistory
            {
                TestCaseId = testCase.Id,
                ChangeType = ChangeType.Modified,
                ChangedBy = user,
                ChangedAt = now,
                PropertyName = displayName,
                OldValue = oldValue,
                NewValue = newValue
            });
        }
    }

    /// <summary>
    /// Schreibt Historieneinträge für ein untergeordnetes Element eines
    /// Testfalls (Parameter, Ergebnis, Anhang).
    /// </summary>
    private static void AppendChildHistory(EntityEntry entry, Guid testCaseId, string elementDisplayName,
        Dictionary<string, string> diffProperties, List<TestCaseHistory> history, DateTime now, string user)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                history.Add(new TestCaseHistory
                {
                    TestCaseId = testCaseId,
                    ChangeType = ChangeType.Created,
                    ChangedBy = user,
                    ChangedAt = now,
                    PropertyName = elementDisplayName
                });
                break;

            case EntityState.Deleted:
                history.Add(new TestCaseHistory
                {
                    TestCaseId = testCaseId,
                    ChangeType = ChangeType.Deleted,
                    ChangedBy = user,
                    ChangedAt = now,
                    PropertyName = elementDisplayName
                });
                break;

            case EntityState.Modified:
                foreach (var property in entry.Properties.Where(p => p.IsModified))
                {
                    if (!diffProperties.TryGetValue(property.Metadata.Name, out var displayName)
                        || Equals(property.OriginalValue, property.CurrentValue))
                    {
                        continue;
                    }

                    history.Add(new TestCaseHistory
                    {
                        TestCaseId = testCaseId,
                        ChangeType = ChangeType.Modified,
                        ChangedBy = user,
                        ChangedAt = now,
                        PropertyName = $"{elementDisplayName} – {displayName}",
                        OldValue = FormatValue(property.OriginalValue),
                        NewValue = FormatValue(property.CurrentValue)
                    });
                }
                break;
        }
    }

    /// <summary>
    /// Schreibt Historieneinträge für hinzugefügte oder entfernte
    /// Tag-Zuordnungen (Einträge der Verknüpfungstabelle).
    /// </summary>
    private async Task AppendTagAssignmentHistoryAsync(EntityEntry entry, HashSet<Guid> deletedTestCaseIds,
        List<TestCaseHistory> history, DateTime now, string user, CancellationToken cancellationToken)
    {
        var testCaseId = GetJoinKey(entry, "TestCaseId");
        if (deletedTestCaseIds.Contains(testCaseId))
        {
            return;
        }

        var tagId = GetJoinKey(entry, "TagId");
        var tagName = Tags.Local.FirstOrDefault(t => t.Id == tagId)?.Name
                      ?? (await Tags.FindAsync([tagId], cancellationToken))?.Name
                      ?? tagId.ToString();

        history.Add(new TestCaseHistory
        {
            TestCaseId = testCaseId,
            ChangeType = entry.State == EntityState.Deleted ? ChangeType.Deleted : ChangeType.Created,
            ChangedBy = user,
            ChangedAt = now,
            PropertyName = $"Tag '{tagName}'"
        });
    }

    /// <summary>Erzeugt einen Audit-Log-Eintrag für eine Entitätsänderung.</summary>
    private static AuditLogEntry CreateAuditEntry(EntityEntry entry, string entityDisplayName,
        Guid entityId, string details, DateTime now, string user)
    {
        return new AuditLogEntry
        {
            Timestamp = now,
            UserName = user,
            Action = entry.State switch
            {
                EntityState.Added => ChangeType.Created,
                EntityState.Deleted => ChangeType.Deleted,
                _ => ChangeType.Modified
            },
            EntityName = entityDisplayName,
            EntityId = entityId,
            Details = details
        };
    }

    /// <summary>Liest einen Schlüsselwert aus einem Verknüpfungseintrag (auch bei Löschungen).</summary>
    private static Guid GetJoinKey(EntityEntry entry, string propertyName)
    {
        var property = entry.Property(propertyName);
        var value = entry.State == EntityState.Deleted ? property.OriginalValue : property.CurrentValue;
        return (Guid)(value ?? Guid.Empty);
    }

    /// <summary>Löst eine Kategorie-Id in ihren Namen auf (für lesbare Historie).</summary>
    private async Task<string?> ResolveCategoryNameAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId is null)
        {
            return null;
        }

        var category = Categories.Local.FirstOrDefault(c => c.Id == categoryId)
                       ?? await Categories.FindAsync([categoryId.Value], cancellationToken);
        return category?.Name ?? categoryId.ToString();
    }

    /// <summary>Formatiert einen Eigenschaftswert für die lesbare Historie.</summary>
    private static string? FormatValue(object? value) => value switch
    {
        null => null,
        bool b => b ? "Ja" : "Nein",
        TestCaseStatus status => status.ToDisplayName(),
        ParameterDataType dataType => dataType.ToDisplayName(),
        _ => value.ToString()
    };
}
