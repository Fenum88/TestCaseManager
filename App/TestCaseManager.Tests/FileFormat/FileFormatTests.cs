using TestCaseManager.Application.FileFormat;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Tests.FileFormat;

/// <summary>
/// Tests für das Standard-Dateiformat (Parser und Writer) einschließlich
/// des verlustfreien Round-Trips Text → Modell → Text.
/// </summary>
public class FileFormatTests
{
    private const string Sample =
        "b_Gebaeude\n" +
        "    (0)_Gebaeude\n" +
        "        tuer=2\n" +
        "        baujahr=\"01.01.1965\"\n" +
        "        stockwerke=4\n" +
        "\n" +
        "b_Personen\n" +
        "    (0)_Personen\n" +
        "        name=Rainer\n" +
        "        nachname=Bongo\n" +
        "    (1)_Personen\n" +
        "        name=Sarah\n" +
        "        nachname=Bongo\n" +
        "        geschlecht=4\n";

    private readonly IndentedKeyValueFileParser _parser = new();
    private readonly IndentedKeyValueFileWriter _writer = new();

    [Fact]
    public void Parse_ErkenntEntitiesZeilenUndWerte()
    {
        var file = _parser.Parse("0001", "Vertragsbeginn", TestCaseFileSection.Input, Sample);

        Assert.Equal(2, file.Entities.Count);

        var gebaeude = file.Entities[0];
        Assert.Equal("Gebaeude", gebaeude.Name);
        Assert.Single(gebaeude.Rows);
        Assert.Equal(0, gebaeude.Rows[0].RowIndex);
        Assert.Equal(3, gebaeude.Rows[0].Values.Count);

        var personen = file.Entities[1];
        Assert.Equal("Personen", personen.Name);
        Assert.Equal(2, personen.Rows.Count);
        Assert.Equal(2, personen.Rows[0].Values.Count);
        Assert.Equal(3, personen.Rows[1].Values.Count);
    }

    [Fact]
    public void Parse_ErkenntAnfuehrungszeichenUndDatentypen()
    {
        var file = _parser.Parse("0001", "Vertragsbeginn", TestCaseFileSection.Input, Sample);
        var gebaeude = file.Entities[0].Rows[0].Values;

        var tuer = gebaeude.Single(v => v.ColumnName == "tuer");
        Assert.Equal("2", tuer.Value);
        Assert.False(tuer.IsQuoted);
        Assert.Equal(ParameterDataType.Integer, tuer.DataType);

        var baujahr = gebaeude.Single(v => v.ColumnName == "baujahr");
        Assert.Equal("01.01.1965", baujahr.Value); // ohne Anführungszeichen
        Assert.True(baujahr.IsQuoted);
        Assert.Equal(ParameterDataType.Text, baujahr.DataType);
    }

    [Fact]
    public void Write_ErzeugtWiedereinlesbaresFormat()
    {
        var original = _parser.Parse("0001", "Vertragsbeginn", TestCaseFileSection.Input, Sample);

        var text = _writer.Write(original);

        // Erneutes Einlesen muss dieselbe Struktur ergeben.
        var reparsed = _parser.Parse("0001", "Vertragsbeginn", TestCaseFileSection.Input, text);

        Assert.Equal(original.Entities.Count, reparsed.Entities.Count);
        for (var e = 0; e < original.Entities.Count; e++)
        {
            var a = original.Entities[e];
            var b = reparsed.Entities[e];
            Assert.Equal(a.Name, b.Name);
            Assert.Equal(a.Rows.Count, b.Rows.Count);

            for (var r = 0; r < a.Rows.Count; r++)
            {
                Assert.Equal(a.Rows[r].RowIndex, b.Rows[r].RowIndex);
                Assert.Equal(
                    a.Rows[r].Values.Select(v => (v.ColumnName, v.Value, v.IsQuoted)),
                    b.Rows[r].Values.Select(v => (v.ColumnName, v.Value, v.IsQuoted)));
            }
        }
    }

    [Fact]
    public void Write_EnthaeltErwarteteZeilen()
    {
        var file = _parser.Parse("0001", "Vertragsbeginn", TestCaseFileSection.Input, Sample);

        var text = _writer.Write(file).Replace("\r\n", "\n");

        Assert.Contains("b_Gebaeude\n", text);
        Assert.Contains("    (0)_Gebaeude\n", text);
        Assert.Contains("        tuer=2\n", text);
        Assert.Contains("        baujahr=\"01.01.1965\"\n", text);
        Assert.Contains("    (1)_Personen\n", text);
    }

    [Fact]
    public void GetFileExtension_LiefertPassendeEndung()
    {
        var input = _parser.Parse("0001", "X", TestCaseFileSection.Input, "b_A\n    (0)_A\n        k=1\n");
        var output = _parser.Parse("0001", "X", TestCaseFileSection.Output, "b_A\n    (0)_A\n        k=1\n");

        Assert.Equal(".Eingabe", _writer.GetFileExtension(input));
        Assert.Equal(".Ausgabe", _writer.GetFileExtension(output));
    }
}
