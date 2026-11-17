using TestCaseManager.Application.Common.Interfaces;
using TestCaseManager.Domain.Entities;

namespace TestCaseManager.Application.Services;

/// <summary>
/// Implementierung der Kategorien-Anwendungsfälle.
/// Neue Kategorien entstehen implizit beim Speichern eines Testfalls
/// (siehe <see cref="TestCaseService"/>), daher bietet dieser Service
/// nur lesende Operationen an.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Erzeugt den Service mit der Arbeitseinheit.</summary>
    /// <param name="unitOfWork">Arbeitseinheit mit allen Repositories.</param>
    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        return categories.OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }
}
