using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Application.Services;

/// <summary>
/// Implementierung der Tag-Anwendungsfälle.
/// Neue Tags entstehen implizit beim Speichern eines Testfalls
/// (siehe <see cref="TestCaseService"/>), daher bietet dieser Service
/// nur lesende Operationen an.
/// </summary>
public class TagService : ITagService
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Erzeugt den Service mit der Arbeitseinheit.</summary>
    /// <param name="unitOfWork">Arbeitseinheit mit allen Repositories.</param>
    public TagService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tags = await _unitOfWork.Tags.GetAllAsync(cancellationToken);
        return tags.OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }
}
