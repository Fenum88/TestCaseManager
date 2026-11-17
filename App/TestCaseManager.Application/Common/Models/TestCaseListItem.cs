using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Schlankes Anzeigemodell (DTO) für die Testfallliste.
/// Enthält nur die Felder, die im Grid dargestellt werden – die
/// vollständige Entität wird erst beim Öffnen der Detailansicht geladen.
/// </summary>
public class TestCaseListItem
{
    /// <summary>Primärschlüssel des Testfalls.</summary>
    public Guid Id { get; init; }

    /// <summary>Name des Testfalls.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Beschreibung (gekürzt darstellbar).</summary>
    public string? Description { get; init; }

    /// <summary>Name der Kategorie oder <c>null</c>.</summary>
    public string? CategoryName { get; init; }

    /// <summary>Fachliche Version.</summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>Aktueller Status.</summary>
    public TestCaseStatus Status { get; init; }

    /// <summary>Ersteller des Testfalls.</summary>
    public string CreatedBy { get; init; } = string.Empty;

    /// <summary>Zeitpunkt der letzten Änderung (bzw. Erstellung).</summary>
    public DateTime LastChangedAt { get; init; }

    /// <summary>Anzahl der Eingabeparameter.</summary>
    public int ParameterCount { get; init; }

    /// <summary>Anzahl der erwarteten Ergebnisse.</summary>
    public int ExpectedResultCount { get; init; }

    /// <summary>Kommagetrennte Liste der Tags.</summary>
    public string Tags { get; init; } = string.Empty;

    /// <summary>
    /// Gibt an, ob es sich um einen eingelesenen Datei-Testfall handelt
    /// (statt eines interaktiv gepflegten Testfalls). Datei-Testfälle werden
    /// in der Liste nur angezeigt und unter „Import / Export" verwaltet.
    /// </summary>
    public bool IsImported { get; init; }

    /// <summary>Typ-Bezeichnung für die Anzeige („Interaktiv" bzw. „Datei-Testfall").</summary>
    public string Kind => IsImported ? "Datei-Testfall" : "Interaktiv";

    /// <summary>Anzeige der Parameteranzahl (leer bei Datei-Testfällen).</summary>
    public string ParameterCountDisplay => IsImported ? "–" : ParameterCount.ToString();

    /// <summary>Anzeige der Ergebnisanzahl (leer bei Datei-Testfällen).</summary>
    public string ExpectedResultCountDisplay => IsImported ? "–" : ExpectedResultCount.ToString();
}
