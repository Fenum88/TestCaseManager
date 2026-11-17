using Microsoft.EntityFrameworkCore;
using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Persistence.Repositories;

/// <summary>
/// EF-Core-Implementierung des Repositories für Abgleichprofile.
/// </summary>
public class CompareProfileRepository : RepositoryBase<CompareProfile>, ICompareProfileRepository
{
    /// <summary>Erzeugt das Repository für den übergebenen Kontext.</summary>
    /// <param name="context">Gemeinsamer Kontext der Arbeitseinheit.</param>
    public CompareProfileRepository(TestCaseManagerDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Task<List<CompareProfile>> GetAllWithRulesAsync(CancellationToken cancellationToken = default)
        => Set
            .Include(p => p.Rules.OrderBy(r => r.SortOrder))
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public void RemoveRange(IEnumerable<CompareProfile> profiles)
        => Set.RemoveRange(profiles);

    /// <inheritdoc />
    public void ReplaceRules(CompareProfile profile, IEnumerable<CompareProfileRule> rules)
    {
        var regeln = Context.Set<CompareProfileRule>();

        regeln.RemoveRange(profile.Rules);
        profile.Rules.Clear();

        foreach (var regel in rules)
        {
            regel.CompareProfileId = profile.Id;
            // Ausdrücklich als Neuanlage kennzeichnen (siehe Schnittstelle).
            regeln.Add(regel);
            profile.Rules.Add(regel);
        }
    }
}
