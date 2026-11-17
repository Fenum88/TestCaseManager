using TestCaseManager.Application.FileFormat;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Tests.FileFormat;

/// <summary>
/// Tests für Entity-Präfixe: Das Präfix ist reine Formatkennzeichnung und wird
/// einheitlich abgeschnitten – <c>b_Gebaeude</c> → <c>Gebaeude</c> und
/// <c>a_Raeume</c> → <c>Raeume</c>. Entscheidend ist dabei, dass die Zeilen
/// trotzdem der richtigen Entität zugeordnet werden.
/// </summary>
public class EntityPrefixTests
{
    private const string Inhalt =
        "b_Gebaeude\n" +
        "    (0)_Gebaeude\n" +
        "        tuer=2\n" +
        "        gebaeudeId=5\n" +
        "\n" +
        "a_Raeume\n" +
        "    (0)_Raeume\n" +
        "        gebaeudeId=5\n" +
        "        raumnummer=1\n" +
        "    (1)_Raeume\n" +
        "        gebaeudeId=5\n" +
        "        raumnummer=2\n";

    [Fact]
    public void Parse_SchneidetJedesPraefixAb()
    {
        var file = new IndentedKeyValueFileParser()
            .Parse("0001", "Test", TestCaseFileSection.Output, Inhalt);

        Assert.Equal(2, file.Entities.Count);

        // Beide Präfixe entfallen einheitlich im Namen.
        Assert.Equal("Gebaeude", file.Entities[0].Name);
        Assert.Equal("Raeume", file.Entities[1].Name);

        // Entscheidend: die Zeilen landen bei der richtigen Entität,
        // nicht bei der vorhergehenden.
        Assert.Single(file.Entities[0].Rows);
        Assert.Equal(2, file.Entities[1].Rows.Count);
    }

    [Fact]
    public void Write_SchreibtEntitaetenMitStandardpraefix()
    {
        var parser = new IndentedKeyValueFileParser();
        var writer = new IndentedKeyValueFileWriter();

        var file = parser.Parse("0001", "Test", TestCaseFileSection.Output, Inhalt);
        var geschrieben = writer.Write(file);

        // Ohne Präfix im Namen schreibt der Writer einheitlich "b_".
        Assert.Contains("b_Gebaeude", geschrieben);
        Assert.Contains("b_Raeume", geschrieben);
        Assert.Contains("    (0)_Raeume", geschrieben);
        Assert.DoesNotContain("b_a_Raeume", geschrieben);
    }

    [Fact]
    public void Parse_WertMitUnterstrichWirdNichtAlsEntityGelesen()
    {
        // "a_wert=3" ist ein Wert, kein Entity-Kopf (enthält "=").
        var inhalt =
            "b_Gebaeude\n" +
            "    (0)_Gebaeude\n" +
            "        a_wert=3\n";

        var file = new IndentedKeyValueFileParser()
            .Parse("0001", "Test", TestCaseFileSection.Output, inhalt);

        var entity = Assert.Single(file.Entities);
        Assert.Equal("Gebaeude", entity.Name);
        var value = Assert.Single(entity.Rows[0].Values);
        Assert.Equal("a_wert", value.ColumnName);
        Assert.Equal("3", value.Value);
    }
}
