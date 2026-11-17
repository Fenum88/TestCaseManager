using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Services;

namespace TestCaseManager.Infrastructure.Services;

/// <summary>
/// Führt externe Berechnungssysteme als eigenständige Prozesse aus. Der Aufruf
/// erfolgt streng parametrisiert (kein Zusammenbau von Kommandozeilen als
/// Zeichenkette), sodass Sonderzeichen in Pfaden keine Injektion ermöglichen.
/// stdout und stderr werden vollständig eingesammelt und zurückgegeben.
/// </summary>
public class CalculationSystemService : ICalculationSystemService
{
    private readonly ILogger<CalculationSystemService> _logger;

    /// <summary>Erzeugt den Dienst (Dependency Injection).</summary>
    /// <param name="logger">Logger für Diagnose und Fehlerprotokollierung.</param>
    public CalculationSystemService(ILogger<CalculationSystemService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CalculationRunResult> RunAsync(string executable, string scriptPath,
        string inputDirectory, string outputDirectory, string? argumentsTemplate = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptPath);

        var output = new ConcurrentQueue<CalculationOutputLine>();

        if (!File.Exists(scriptPath))
        {
            output.Enqueue(new CalculationOutputLine(
                $"FEHLER: Skript nicht gefunden: {scriptPath}", IsError: true));
            return new CalculationRunResult { ExitCode = -1, Output = output.ToList() };
        }

        if (!Directory.Exists(inputDirectory))
        {
            output.Enqueue(new CalculationOutputLine(
                $"FEHLER: Eingabeordner nicht gefunden: {inputDirectory}", IsError: true));
            return new CalculationRunResult { ExitCode = -1, Output = output.ToList() };
        }

        // Zielordner sicherstellen, damit das Skript hineinschreiben kann.
        Directory.CreateDirectory(outputDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        // Argumente aus dem Aufrufmuster ableiten und einzeln übergeben
        // (parametrisiert, kein String-Concat → keine Injektionsgefahr).
        foreach (var argument in BuildArguments(argumentsTemplate, scriptPath, inputDirectory, outputDirectory))
        {
            startInfo.ArgumentList.Add(argument);
        }

        // Python zu UTF-8-Ausgabe zwingen, damit Umlaute korrekt ankommen.
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        startInfo.Environment["PYTHONUTF8"] = "1";

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                output.Enqueue(new CalculationOutputLine(e.Data, IsError: false));
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                output.Enqueue(new CalculationOutputLine(e.Data, IsError: true));
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            _logger.LogInformation(
                "Berechnungssystem {Script} beendet mit Code {ExitCode}.", scriptPath, process.ExitCode);

            return new CalculationRunResult { ExitCode = process.ExitCode, Output = output.ToList() };
        }
        catch (Win32Exception ex)
        {
            // Häufigster Fall: das Programm (z. B. "python") ist nicht installiert
            // oder nicht im PATH.
            _logger.LogError(ex, "Berechnungssystem {Executable} konnte nicht gestartet werden.", executable);
            output.Enqueue(new CalculationOutputLine(
                $"FEHLER: '{executable}' konnte nicht gestartet werden. "
                + "Ist Python installiert und im PATH? Details: " + ex.Message, IsError: true));
            return new CalculationRunResult { ExitCode = -1, Output = output.ToList() };
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            output.Enqueue(new CalculationOutputLine("Abgebrochen.", IsError: true));
            return new CalculationRunResult { ExitCode = -1, Output = output.ToList() };
        }
    }

    /// <summary>
    /// Wandelt das Aufrufmuster in eine Liste einzelner Argumente um. Das Muster
    /// wird an Leerzeichen zerlegt; in jedem Token werden die Platzhalter
    /// <c>{script}</c>, <c>{input}</c> und <c>{output}</c> ersetzt. Da jedes
    /// Token ein eigenes Argument bleibt, dürfen die eingesetzten Pfade
    /// Leerzeichen enthalten, ohne dass eine Kommandozeile zerbricht.
    /// </summary>
    private static IEnumerable<string> BuildArguments(string? template, string scriptPath,
        string inputDirectory, string outputDirectory)
    {
        var effective = string.IsNullOrWhiteSpace(template)
            ? CalculationSystem.DefaultArgumentsTemplate
            : template;

        var tokens = effective.Split(' ', StringSplitOptions.RemoveEmptyEntries
            | StringSplitOptions.TrimEntries);

        foreach (var token in tokens)
        {
            yield return token
                .Replace("{script}", scriptPath)
                .Replace("{input}", inputDirectory)
                .Replace("{output}", outputDirectory);
        }
    }

    /// <summary>Beendet den Prozess bei Abbruch, ohne eine Folgeausnahme zu werfen.</summary>
    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Prozess bereits beendet – nichts zu tun.
        }
    }
}
