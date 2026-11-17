using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Services;

namespace TestCaseManager.Infrastructure.Services;

/// <summary>
/// Speichert die Testszenarien in einer JSON-Datei. Der Zugriff ist über ein
/// Semaphore serialisiert, damit gleichzeitige Lese-/Schreibvorgänge die Datei
/// nicht beschädigen.
/// </summary>
public class JsonTestScenarioStore : ITestScenarioStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TestScenarioStoreOptions _options;
    private readonly ILogger<JsonTestScenarioStore> _logger;

    /// <summary>Erzeugt den Speicher (Dependency Injection).</summary>
    /// <param name="options">Dateipfad.</param>
    /// <param name="logger">Logger für Diagnose und Fehlerprotokollierung.</param>
    public JsonTestScenarioStore(TestScenarioStoreOptions options, ILogger<JsonTestScenarioStore> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TestScenario>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_options.FilePath))
            {
                return [];
            }

            var json = await File.ReadAllTextAsync(_options.FilePath, cancellationToken);
            return JsonSerializer.Deserialize<List<TestScenario>>(json, SerializerOptions) ?? [];
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _logger.LogError(ex, "Testszenarien konnten nicht aus {Path} gelesen werden.", _options.FilePath);
            return [];
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(IEnumerable<TestScenario> scenarios, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(_options.FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var list = scenarios.ToList();
            var json = JsonSerializer.Serialize(list, SerializerOptions);
            await File.WriteAllTextAsync(_options.FilePath, json, cancellationToken);
            _logger.LogInformation("{Count} Testszenario(s) nach {Path} gespeichert.", list.Count, _options.FilePath);
        }
        finally
        {
            _gate.Release();
        }
    }
}
