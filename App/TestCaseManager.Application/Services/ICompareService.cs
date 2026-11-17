using TestCaseManager.Application.Common.Models;

namespace TestCaseManager.Application.Services;

/// <summary>
/// Vergleicht die Ausgaben zweier Berechnungssysteme. Für jedes einander
/// entsprechende Ausgabedateipaar wird eine Vergleichsdatei in derselben
/// Ordnerstruktur erzeugt; zusätzlich entsteht eine Gesamt-CSV mit allen
/// Abgleichen.
/// </summary>
public interface ICompareService
{
    /// <summary>
    /// Vergleicht zwei zuvor berechnete Ausgabeordner attributweise.
    /// </summary>
    /// <param name="request">Quellordner, Beschriftungen und Zielpfade.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <returns>Kennzahlen und Meldungen des Vergleichs.</returns>
    Task<CompareResult> CompareOutputsAsync(CompareOutputsRequest request,
        CancellationToken cancellationToken = default);
}
