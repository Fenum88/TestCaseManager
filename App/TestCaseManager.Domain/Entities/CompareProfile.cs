using TestCaseManager.Domain.Common;

namespace TestCaseManager.Domain.Entities;

/// <summary>
/// Ein benanntes Abgleichprofil: bündelt die Schlüsselregeln, nach denen beim
/// Vergleich zweier Berechnungssysteme die Zeilen einer Entität einander
/// zugeordnet werden.
///
/// Die Profile liegen in der Datenbank und nicht mehr in einer lokalen Datei,
/// damit alle Nutzer mit demselben Bestand arbeiten.
/// </summary>
public class CompareProfile : AuditableEntity
{
    /// <summary>Anzeigename des Profils (systemweit eindeutig).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optionale Beschreibung.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Reihenfolge in der Anzeige.</summary>
    public int SortOrder { get; set; }

    /// <summary>Die Schlüsselregeln dieses Profils.</summary>
    public List<CompareProfileRule> Rules { get; set; } = [];
}

/// <summary>
/// Eine Schlüsselregel innerhalb eines Abgleichprofils: Für die genannte
/// Entität werden die Zeilen über die angegebenen Attribute zugeordnet
/// (statt über ihre Position).
///
/// Die Schlüsselattribute stehen als kommagetrennte Liste in einer Spalte.
/// Eine eigene Tabelle je Attribut brächte hier keinen Nutzen: Es wird nie
/// nach einzelnen Attributen gesucht, sondern immer die vollständige,
/// geordnete Liste gelesen.
/// </summary>
public class CompareProfileRule : EntityBase
{
    /// <summary>Fremdschlüssel zum enthaltenden Profil.</summary>
    public Guid CompareProfileId { get; set; }

    /// <summary>Das enthaltende Profil (Navigationseigenschaft).</summary>
    public CompareProfile? CompareProfile { get; set; }

    /// <summary>Name der Entität (z. B. "Provision").</summary>
    public string Entity { get; set; } = string.Empty;

    /// <summary>Die Schlüsselattribute als kommagetrennte Liste (z. B. "datum, art").</summary>
    public string KeyColumns { get; set; } = string.Empty;

    /// <summary>Reihenfolge der Regel innerhalb des Profils.</summary>
    public int SortOrder { get; set; }
}
