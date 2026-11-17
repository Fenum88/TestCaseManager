using System.Text;

namespace TestCaseManager.Application.Common.Models;

/// <summary>
/// Wandelt Abgleichprofile in das deutsche CSV-Format (Semikolon-getrennt) und
/// zurück. Damit lassen sich Profile exportieren, in einer Tabellenkalkulation
/// bearbeiten und wieder einlesen.
///
/// Aufbau – eine Zeile je Regel, Profilangaben wiederholen sich:
/// <code>
/// Profil;Beschreibung;Entität;Schlüsselattribute
/// Provision nach Datum;Ordnet über das Datum zu.;Provision;datum
/// Provision nach Datum;Ordnet über das Datum zu.;Raeume;gebaeudeId, raumnummer
/// Nur Position;Ohne Regeln.;;
/// </code>
/// Ein Profil ohne Regeln steht mit leeren Feldern für Entität und Schlüssel.
/// Mehrere Schlüsselattribute werden innerhalb der Zelle mit Komma getrennt.
/// </summary>
public static class CompareProfileCsv
{
    private const char Separator = ';';

    /// <summary>Die Kopfzeile des Formats.</summary>
    public const string Header = "Profil;Beschreibung;Entität;Schlüsselattribute";

    /// <summary>Erzeugt den CSV-Inhalt für die übergebenen Profile.</summary>
    /// <param name="profiles">Die zu exportierenden Profile.</param>
    public static string Write(IEnumerable<CompareProfile> profiles)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Header);

        foreach (var profile in profiles)
        {
            if (profile.KeyRules.Count == 0)
            {
                // Profil ohne Regeln: Entität und Schlüssel bleiben leer.
                AppendRow(builder, profile.Name, profile.Description, string.Empty, string.Empty);
                continue;
            }

            foreach (var rule in profile.KeyRules)
            {
                AppendRow(builder, profile.Name, profile.Description,
                    rule.Entity, string.Join(", ", rule.KeyColumns));
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Liest Profile aus einem CSV-Inhalt. Zeilen desselben Profilnamens werden
    /// zu einem Profil mit mehreren Regeln zusammengefasst.
    /// </summary>
    /// <param name="csv">Der CSV-Inhalt.</param>
    /// <param name="profiles">Die gelesenen Profile (bei Erfolg).</param>
    /// <param name="error">Fehlermeldung mit Zeilenangabe (bei Misserfolg).</param>
    /// <returns><c>true</c>, wenn der Inhalt gültig ist.</returns>
    public static bool TryParse(string? csv, out IReadOnlyList<CompareProfile> profiles, out string? error)
    {
        profiles = [];
        error = null;

        if (string.IsNullOrWhiteSpace(csv))
        {
            error = "Die Datei ist leer.";
            return false;
        }

        var records = SplitRecords(csv);
        if (records.Count == 0)
        {
            error = "Die Datei enthält keine Zeilen.";
            return false;
        }

        // Kopfzeile ist optional, wird aber erkannt und übersprungen.
        var start = records[0].Count > 0
                    && records[0][0].Trim().Equals("Profil", StringComparison.OrdinalIgnoreCase)
            ? 1 : 0;

        // Reihenfolge der Profile beibehalten.
        var order = new List<string>();
        var byName = new Dictionary<string, (string Description, List<CompareKeyRule> Rules)>(
            StringComparer.OrdinalIgnoreCase);

        for (var i = start; i < records.Count; i++)
        {
            var fields = records[i];
            var lineNumber = i + 1;

            // Vollständig leere Zeilen überspringen.
            if (fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (fields.Count < 3)
            {
                error = $"Zeile {lineNumber}: Es werden mindestens die Spalten "
                        + "Profil, Beschreibung und Entität erwartet.";
                return false;
            }

            var name = fields[0].Trim();
            if (name.Length == 0)
            {
                error = $"Zeile {lineNumber}: Der Profilname fehlt.";
                return false;
            }

            var description = fields[1].Trim();
            var entity = fields[2].Trim();
            var keys = fields.Count > 3 ? fields[3].Trim() : string.Empty;

            if (!byName.TryGetValue(name, out var entry))
            {
                entry = (description, []);
                byName[name] = entry;
                order.Add(name);
            }
            else if (entry.Description.Length == 0 && description.Length > 0)
            {
                // Beschreibung aus der ersten Zeile übernehmen, die eine hat.
                byName[name] = entry = (description, entry.Rules);
            }

            if (entity.Length == 0)
            {
                // Zeile beschreibt nur das Profil (ohne Regel).
                continue;
            }

            var keyColumns = keys
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            if (keyColumns.Count == 0)
            {
                error = $"Zeile {lineNumber}: Zur Entität „{entity}“ fehlen die Schlüsselattribute.";
                return false;
            }

            entry.Rules.Add(new CompareKeyRule { Entity = entity, KeyColumns = keyColumns });
        }

        if (order.Count == 0)
        {
            error = "Die Datei enthält keine Profile.";
            return false;
        }

        profiles = order
            .Select(name => new CompareProfile
            {
                Name = name,
                Description = byName[name].Description,
                KeyRules = byName[name].Rules
            })
            .ToList();
        return true;
    }

    /// <summary>Hängt eine Zeile mit maskierten Feldern an.</summary>
    private static void AppendRow(StringBuilder builder, params string[] fields)
    {
        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(Separator);
            }
            builder.Append(Escape(fields[i]));
        }
        builder.AppendLine();
    }

    /// <summary>Maskiert ein Feld (Trennzeichen, Anführungszeichen, Zeilenumbrüche).</summary>
    private static string Escape(string value)
    {
        if (value.IndexOf(Separator) < 0 && value.IndexOf('"') < 0
            && value.IndexOf('\n') < 0 && value.IndexOf('\r') < 0)
        {
            return value;
        }
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>
    /// Zerlegt den CSV-Inhalt in Datensätze und Felder. Berücksichtigt
    /// Anführungszeichen, sodass Trennzeichen und Zeilenumbrüche innerhalb
    /// eines Feldes erhalten bleiben.
    /// </summary>
    private static List<List<string>> SplitRecords(string csv)
    {
        var records = new List<List<string>>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < csv.Length; i++)
        {
            var c = csv[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Verdoppeltes Anführungszeichen = ein echtes Zeichen.
                    if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;

                case Separator:
                    fields.Add(field.ToString());
                    field.Clear();
                    break;

                case '\r':
                    break; // Zeilenende wird über '\n' behandelt.

                case '\n':
                    fields.Add(field.ToString());
                    field.Clear();
                    records.Add(fields);
                    fields = [];
                    break;

                default:
                    field.Append(c);
                    break;
            }
        }

        // Letzten Datensatz übernehmen, falls die Datei ohne Zeilenumbruch endet.
        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            records.Add(fields);
        }

        return records;
    }
}
