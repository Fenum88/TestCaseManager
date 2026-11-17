namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Eine einzelne Wertänderung an einem gespeicherten Testfall-Attribut.
///
/// Adressiert wird über den Primärschlüssel des Wertes und nicht über den
/// Pfad (Entity · Zeile · Spalte): Das Meta-Modell erlaubt je Zeile
/// unterschiedliche Spalten, weshalb ein Pfad nicht zwingend eindeutig ist.
/// Datentyp und Anführungszeichen werden beim Speichern neu abgeleitet und
/// deshalb hier bewusst nicht mitgeführt.
/// </summary>
/// <param name="ValueId">Primärschlüssel des zu ändernden Wertes.</param>
/// <param name="NewValue">Der neue Rohwert, wie er eingegeben wurde.</param>
public readonly record struct AttributeValueUpdate(Guid ValueId, string? NewValue);
