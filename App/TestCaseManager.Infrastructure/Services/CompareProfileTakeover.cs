using System.IO;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Services;

namespace TestCaseManager.Infrastructure.Services;

/// <summary>
/// Überführt einmalig die zuvor lokal gespeicherten Abgleichprofile in die
/// Datenbank. Nötig, weil die Profile bis zur Umstellung je Arbeitsplatz in
/// einer JSON-Datei lagen.
///
/// Die Übernahme läuft nur, solange die Datenbank noch keine Profile enthält.
/// Danach wird die Datei umbenannt und bleibt als Sicherung liegen – so kann
/// sie weder ein zweites Mal eingelesen werden noch geht sie verloren.
/// </summary>
public class CompareProfileTakeover
{
    /// <summary>Endung, die die bereits übernommene Datei erhält.</summary>
    public const string TakenOverSuffix = ".uebernommen";

    private readonly ICompareProfileStore _target;
    private readonly CompareProfileStoreOptions _options;
    private readonly ILogger<CompareProfileTakeover> _logger;

    /// <summary>Erzeugt die Übernahme (Dependency Injection).</summary>
    /// <param name="target">Der neue Speicher (Datenbank).</param>
    /// <param name="options">Pfad der bisherigen JSON-Datei.</param>
    /// <param name="logger">Logger für Statusmeldungen.</param>
    public CompareProfileTakeover(ICompareProfileStore target,
        CompareProfileStoreOptions options, ILogger<CompareProfileTakeover> logger)
    {
        _target = target;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Führt die Übernahme aus, sofern erforderlich.
    /// </summary>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    /// <returns>Anzahl der übernommenen Profile (0, wenn nichts zu tun war).</returns>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.FilePath) || !File.Exists(_options.FilePath))
        {
            return 0;
        }

        // Enthält die Datenbank bereits Profile, ist die Umstellung erfolgt.
        // Die Datei würde sonst einen gepflegten Bestand überschreiben.
        var vorhandene = await _target.GetAllAsync(cancellationToken);
        if (vorhandene.Count > 0)
        {
            return 0;
        }

        var ausDatei = await new JsonCompareProfileStore(_options,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<JsonCompareProfileStore>.Instance)
            .GetAllAsync(cancellationToken);

        if (ausDatei.Count == 0)
        {
            return 0;
        }

        await _target.SaveAsync(ausDatei, cancellationToken);
        Archiviere();

        _logger.LogInformation("{Count} Abgleichprofil(e) aus {Path} in die Datenbank übernommen.",
            ausDatei.Count, _options.FilePath);
        return ausDatei.Count;
    }

    /// <summary>
    /// Benennt die übernommene Datei um. Schlägt das fehl (etwa weil die Datei
    /// geöffnet ist), bleibt die Übernahme trotzdem gültig – beim nächsten
    /// Start verhindert der gefüllte Datenbankbestand eine Wiederholung.
    /// </summary>
    private void Archiviere()
    {
        try
        {
            var ziel = _options.FilePath + TakenOverSuffix;
            if (File.Exists(ziel))
            {
                File.Delete(ziel);
            }
            File.Move(_options.FilePath, ziel);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Die übernommene Datei {Path} konnte nicht umbenannt werden.",
                _options.FilePath);
        }
    }
}
