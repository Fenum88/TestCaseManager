namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Legt fest, über welche Attribute die Zeilen einer Entität beim Abgleich
/// einander zugeordnet werden. Beispiel: Für die Entität <c>Provision</c> ist
/// <c>datum</c> der Schlüssel – dann werden Einträge mit gleichem Datum
/// verglichen, unabhängig von ihrer Position in der Datei.
///
/// Ohne Regel für eine Entität wird auf den Zeilenindex zurückgefallen
/// (Position 0 gegen Position 0 usw.).
/// </summary>
public class CompareKeyRule
{
    /// <summary>Name der Entität (z. B. "Provision"), Groß-/Kleinschreibung egal.</summary>
    public string Entity { get; init; } = string.Empty;

    /// <summary>
    /// Die Schlüsselattribute in Reihenfolge (zusammengesetzte Schlüssel sind
    /// möglich, z. B. <c>datum</c> + <c>art</c>).
    /// </summary>
    public IReadOnlyList<string> KeyColumns { get; init; } = [];
}

/// <summary>
/// Ein benanntes Abgleichprofil: bündelt die Schlüsselregeln, die beim
/// Vergleich gelten sollen. So lassen sich mehrere Sichtweisen pflegen –
/// etwa „Provision nach Datum" gegenüber „nur Position".
/// </summary>
public class CompareProfile
{
    /// <summary>Anzeigename des Profils (eindeutig).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Optionale Beschreibung.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Die Schlüsselregeln dieses Profils.</summary>
    public IReadOnlyList<CompareKeyRule> KeyRules { get; init; } = [];
}

/// <summary>Einstellungen für den JSON-Speicher der Abgleichprofile.</summary>
public class CompareProfileStoreOptions
{
    /// <summary>Vollständiger Pfad zur JSON-Datei mit den Profilen.</summary>
    public string FilePath { get; init; } = string.Empty;
}
