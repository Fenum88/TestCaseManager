using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Services;
using DomainProfile = TestCaseManager.Domain.Entities.CompareProfile;
using DomainRule = TestCaseManager.Domain.Entities.CompareProfileRule;

namespace TestCaseManager.Infrastructure.Services;

/// <summary>
/// Speichert die Abgleichprofile in der Datenbank. Dadurch arbeiten alle
/// Nutzer mit demselben Bestand – anders als beim früheren Speicher, der die
/// Profile in einer Datei auf dem jeweiligen Arbeitsplatz ablegte.
///
/// Der Speicher ist als Singleton registriert, öffnet aber je Vorgang einen
/// eigenen Dienstbereich: Ein über die Laufzeit der Anwendung offener
/// Datenbankkontext wäre für lang laufende Fenster ungeeignet.
/// </summary>
public class DatabaseCompareProfileStore : ICompareProfileStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseCompareProfileStore> _logger;

    /// <summary>Erzeugt den Speicher (Dependency Injection).</summary>
    /// <param name="scopeFactory">Fabrik für DI-Dienstbereiche.</param>
    /// <param name="logger">Logger für Diagnose und Fehlerprotokollierung.</param>
    public DatabaseCompareProfileStore(IServiceScopeFactory scopeFactory,
        ILogger<DatabaseCompareProfileStore> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CompareProfile>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var gespeicherte = await unitOfWork.CompareProfiles.GetAllWithRulesAsync(cancellationToken);
        return gespeicherte.Select(ToModel).ToList();
    }

    /// <inheritdoc />
    public async Task SaveAsync(IEnumerable<CompareProfile> profiles,
        CancellationToken cancellationToken = default)
    {
        var neue = profiles.ToList();

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Die Oberfläche speichert stets die vollständige Liste. Der Bestand
        // wird deshalb abgeglichen: gleichnamige Profile werden aktualisiert,
        // fehlende entfernt, neue angelegt. Ein pauschales Löschen und
        // Neuanlegen würde die Auditfelder aller Profile zurücksetzen.
        var vorhandene = await unitOfWork.CompareProfiles.GetAllWithRulesAsync(cancellationToken);
        var nachName = vorhandene.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var behalten = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sortierung = 0;

        foreach (var profil in neue)
        {
            var name = profil.Name.Trim();
            if (name.Length == 0)
            {
                continue;
            }
            behalten.Add(name);

            if (nachName.TryGetValue(name, out var ziel))
            {
                ziel.Name = name;
                ziel.Description = profil.Description.Trim();
                ziel.SortOrder = sortierung++;
                unitOfWork.CompareProfiles.ReplaceRules(ziel, ToRules(profil));
                continue;
            }

            var angelegt = new DomainProfile
            {
                Name = name,
                Description = profil.Description.Trim(),
                SortOrder = sortierung++
            };
            angelegt.Rules.AddRange(ToRules(profil));
            await unitOfWork.CompareProfiles.AddAsync(angelegt, cancellationToken);
        }

        var entfallene = vorhandene.Where(p => !behalten.Contains(p.Name)).ToList();
        if (entfallene.Count > 0)
        {
            unitOfWork.CompareProfiles.RemoveRange(entfallene);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("{Count} Abgleichprofil(e) in der Datenbank gespeichert "
            + "({Removed} entfernt).", behalten.Count, entfallene.Count);
    }

    /// <summary>Bildet eine gespeicherte Entität auf das Anwendungsmodell ab.</summary>
    /// <param name="entity">Das gespeicherte Profil.</param>
    private static CompareProfile ToModel(DomainProfile entity) => new()
    {
        Name = entity.Name,
        Description = entity.Description,
        KeyRules = entity.Rules
            .OrderBy(r => r.SortOrder)
            .Select(r => new CompareKeyRule
            {
                Entity = r.Entity,
                KeyColumns = SplitColumns(r.KeyColumns)
            })
            .ToList()
    };

    /// <summary>Bildet die Regeln eines Anwendungsmodells auf Entitäten ab.</summary>
    /// <param name="profile">Das zu speichernde Profil.</param>
    private static IEnumerable<DomainRule> ToRules(CompareProfile profile)
    {
        var sortierung = 0;
        foreach (var regel in profile.KeyRules)
        {
            var entity = regel.Entity.Trim();
            var spalten = regel.KeyColumns
                .Select(c => c.Trim())
                .Where(c => c.Length > 0)
                .ToList();

            // Regeln ohne Entität oder ohne Schlüsselattribute haben keine
            // Wirkung und werden gar nicht erst gespeichert.
            if (entity.Length == 0 || spalten.Count == 0)
            {
                continue;
            }

            yield return new DomainRule
            {
                Entity = entity,
                KeyColumns = string.Join(", ", spalten),
                SortOrder = sortierung++
            };
        }
    }

    /// <summary>Zerlegt die kommagetrennte Attributliste einer Regel.</summary>
    /// <param name="value">Die gespeicherte Liste (z. B. "datum, art").</param>
    private static IReadOnlyList<string> SplitColumns(string value)
        => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
}
