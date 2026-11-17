using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Services;

namespace TestCaseManager.Infrastructure.Services;

/// <summary>
/// Speichert die Berechnungssysteme in einer JSON-Datei. Existiert die Datei
/// beim ersten Zugriff noch nicht, wird sie aus der Erstbelegung (Seed) der
/// Konfiguration angelegt. Der Zugriff ist über ein Semaphore serialisiert,
/// damit gleichzeitige Lese-/Schreibvorgänge die Datei nicht beschädigen.
/// </summary>
public class JsonCalculationSystemStore : ICalculationSystemStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CalculationSystemStoreOptions _options;
    private readonly ILogger<JsonCalculationSystemStore> _logger;

    /// <summary>Erzeugt den Speicher (Dependency Injection).</summary>
    /// <param name="options">Dateipfad und Erstbelegung.</param>
    /// <param name="logger">Logger für Diagnose und Fehlerprotokollierung.</param>
    public JsonCalculationSystemStore(CalculationSystemStoreOptions options,
        ILogger<JsonCalculationSystemStore> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CalculationSystem>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_options.FilePath))
            {
                // Erststart: Seed übernehmen und persistieren.
                var seed = _options.Seed.ToList();
                await WriteAsync(seed, cancellationToken);
                return seed;
            }

            var json = await File.ReadAllTextAsync(_options.FilePath, cancellationToken);
            var systems = JsonSerializer.Deserialize<List<CalculationSystem>>(json, SerializerOptions);
            return systems ?? [];
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _logger.LogError(ex, "Berechnungssysteme konnten nicht aus {Path} gelesen werden.",
                _options.FilePath);
            // Auf die Erstbelegung zurückfallen, statt die Anwendung scheitern zu lassen.
            return _options.Seed.ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(IEnumerable<CalculationSystem> systems,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await WriteAsync(systems.ToList(), cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Schreibt die Liste als JSON (legt den Zielordner bei Bedarf an).</summary>
    private async Task WriteAsync(List<CalculationSystem> systems, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_options.FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(systems, SerializerOptions);
        await File.WriteAllTextAsync(_options.FilePath, json, cancellationToken);
        _logger.LogInformation("{Count} Berechnungssystem(e) nach {Path} gespeichert.",
            systems.Count, _options.FilePath);
    }
}
