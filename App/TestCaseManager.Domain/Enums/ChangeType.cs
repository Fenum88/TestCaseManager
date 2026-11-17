namespace TestCaseManager.Domain.Enums;

/// <summary>
/// Art einer protokollierten Änderung in Historie und Audit-Log.
/// </summary>
public enum ChangeType
{
    /// <summary>Datensatz wurde neu angelegt.</summary>
    Created = 0,

    /// <summary>Datensatz wurde geändert.</summary>
    Modified = 1,

    /// <summary>Datensatz wurde gelöscht.</summary>
    Deleted = 2
}
