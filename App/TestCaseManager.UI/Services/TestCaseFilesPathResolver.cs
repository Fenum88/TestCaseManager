using System.IO;

namespace TestCaseManager.UI.Services;

/// <summary>
/// Ermittelt den Standard-Wurzelordner der Testfalldateien
/// (<c>Workspace/Testfälle</c>). Ist in der Konfiguration ein fester Pfad
/// hinterlegt, wird dieser verwendet; andernfalls sucht der Resolver vom
/// Anwendungsverzeichnis aufwärts nach dem Ordner, sodass die Anwendung
/// unabhängig vom Startort funktioniert.
/// </summary>
public static class TestCaseFilesPathResolver
{
    private const string WorkspaceFolder = "Workspace";
    private const string TestCaseFolder = "Testfälle";
    private const string InputFolder = "Input";
    private const string TestCaseFolderSuffix = "_TF";

    /// <summary>
    /// Ermittelt die Testfallnummern aus dem Eingabeordner (<c>Input</c>) eines
    /// Wurzelordners, d. h. aus den Unterordnern <c>{Nummer}_TF</c>.
    /// </summary>
    /// <param name="testCaseRoot">Wurzelordner der Testfälle (enthält <c>Input</c>).</param>
    /// <returns>Die gefundenen Testfallnummern, aufsteigend sortiert.</returns>
    public static IReadOnlyList<string> EnumerateTestCaseNumbers(string testCaseRoot)
    {
        var input = Path.Combine(testCaseRoot, InputFolder);
        if (!Directory.Exists(input))
        {
            return [];
        }

        return Directory.EnumerateDirectories(input)
            .Select(dir => Path.GetFileName(dir))
            .Where(name => name.EndsWith(TestCaseFolderSuffix, StringComparison.OrdinalIgnoreCase))
            .Select(name => name[..^TestCaseFolderSuffix.Length])
            .OrderBy(number => number, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Liefert den zu verwendenden Wurzelordner der Testfalldateien.
    /// </summary>
    /// <param name="configuredPath">In der appsettings.json hinterlegter Pfad (kann leer sein).</param>
    /// <param name="baseDirectory">Ausgangsverzeichnis der Suche (i. d. R. das Anwendungsverzeichnis).</param>
    /// <returns>Der ermittelte Pfad (existiert möglicherweise noch nicht).</returns>
    public static string Resolve(string? configuredPath, string baseDirectory)
        => ResolveWorkspaceSubfolder(configuredPath, baseDirectory, TestCaseFolder);

    /// <summary>
    /// Ermittelt einen beliebigen Unterordner des <c>Workspace</c>-Ordners
    /// (z. B. <c>Testfälle</c> oder <c>Berechnungssysteme</c>). Ist ein fester
    /// Pfad konfiguriert, wird dieser verwendet; andernfalls sucht die Methode
    /// vom Anwendungsverzeichnis aufwärts, sodass die Anwendung unabhängig vom
    /// Startort funktioniert.
    /// </summary>
    /// <param name="configuredPath">Konfigurierter Pfad (kann leer sein).</param>
    /// <param name="baseDirectory">Ausgangsverzeichnis der Suche.</param>
    /// <param name="subFolder">Name des gesuchten Workspace-Unterordners.</param>
    /// <returns>Der ermittelte Pfad (existiert möglicherweise noch nicht).</returns>
    public static string ResolveWorkspaceSubfolder(string? configuredPath, string baseDirectory,
        string subFolder)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        // Vom Anwendungsverzeichnis aufwärts nach "Workspace/{subFolder}" suchen.
        var current = new DirectoryInfo(baseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, WorkspaceFolder, subFolder);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            current = current.Parent;
        }

        // Fallback: neben dem Anwendungsverzeichnis.
        return Path.Combine(baseDirectory, WorkspaceFolder, subFolder);
    }
}
