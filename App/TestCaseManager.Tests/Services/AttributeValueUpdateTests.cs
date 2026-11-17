using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.FileFormat;
using TestCaseManager.Domain.Enums;
using TestCaseManager.Infrastructure.Services;
using TestCaseManager.Tests.TestSupport;

namespace TestCaseManager.Tests.Services;

/// <summary>
/// Tests für das Bearbeiten gespeicherter Attributwerte
/// (<see cref="TestCaseFileService.UpdateAttributeValuesAsync"/>): Übernahme
/// in die Datenbank, Neuableitung des Datentyps, Schutz vor Änderungen an
/// fremden Testfällen und Formattreue beim erneuten Auslesen.
/// </summary>
public class AttributeValueUpdateTests : IDisposable
{
    private const string InputContent =
        "b_Gebaeude\n" +
        "    (0)_Gebaeude\n" +
        "        tuer=2\n" +
        "        baujahr=\"01.01.1965\"\n" +
        "\n" +
        "b_Personen\n" +
        "    (0)_Personen\n" +
        "        nachname=Bongo\n";

    private readonly string _root;

    /// <summary>Legt zwei Testfälle an, um auch die Abgrenzung prüfen zu können.</summary>
    public AttributeValueUpdateTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "tcm-edittests-" + Guid.NewGuid().ToString("N"));

        foreach (var number in new[] { "0001", "0002" })
        {
            Directory.CreateDirectory(Path.Combine(_root, "Input", number + "_TF"));
            File.WriteAllText(
                Path.Combine(_root, "Input", number + "_TF", "0001_Vertragsbeginn.Eingabe"),
                InputContent);
        }
    }

    private static TestCaseFileService BuildService(InMemoryTestContext env)
        => new(env.UnitOfWork, new IndentedKeyValueFileParser(), new IndentedKeyValueFileWriter(),
            new TestCaseFileOptions { ImportOutputSection = false },
            NullLogger<TestCaseFileService>.Instance);

    /// <summary>Liest beide Testfälle ein und liefert die Detaildaten des ersten.</summary>
    private static async Task<(ImportedTestCaseDetail First, ImportedTestCaseDetail Second)>
        ImportAsync(TestCaseFileService service, string root)
    {
        await service.ImportFolderAsync(root);

        var summaries = (await service.GetImportedAsync()).OrderBy(s => s.Number).ToList();
        var first = await service.GetImportedDetailAsync(summaries[0].Id);
        var second = await service.GetImportedDetailAsync(summaries[1].Id);
        return (first!, second!);
    }

    [Fact]
    public async Task Update_AendertWertInDerDatenbank()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);
        var (detail, _) = await ImportAsync(service, _root);

        var tuer = detail.Attributes.Single(a => a.ColumnName == "tuer");
        var changed = await service.UpdateAttributeValuesAsync(detail.Id,
            [new AttributeValueUpdate(tuer.ValueId, "7")]);

        Assert.Equal(1, changed);

        await using var verify = env.CreateFreshContext();
        var stored = await verify.Set<TestCaseManager.Domain.Entities.DataValue>()
            .SingleAsync(v => v.Id == tuer.ValueId);
        Assert.Equal("7", stored.Value);
    }

    [Fact]
    public async Task Update_LeitetDatentypNeuAb()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);
        var (detail, _) = await ImportAsync(service, _root);

        // Aus einer Zahl wird Text: der Datentyp muss folgen.
        var tuer = detail.Attributes.Single(a => a.ColumnName == "tuer");
        await service.UpdateAttributeValuesAsync(detail.Id,
            [new AttributeValueUpdate(tuer.ValueId, "unbekannt")]);

        await using var verify = env.CreateFreshContext();
        var stored = await verify.Set<TestCaseManager.Domain.Entities.DataValue>()
            .SingleAsync(v => v.Id == tuer.ValueId);

        Assert.Equal("unbekannt", stored.Value);
        Assert.Equal(ParameterDataType.Text, stored.DataType);
    }

    [Fact]
    public async Task Update_AusTextWirdZahl_EntferntAnfuehrungszeichen()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);
        var (detail, _) = await ImportAsync(service, _root);

        // baujahr steht in der Datei in Anführungszeichen.
        var baujahr = detail.Attributes.Single(a => a.ColumnName == "baujahr");
        Assert.True(baujahr.IsQuoted);

        await service.UpdateAttributeValuesAsync(detail.Id,
            [new AttributeValueUpdate(baujahr.ValueId, "42")]);

        await using var verify = env.CreateFreshContext();
        var stored = await verify.Set<TestCaseManager.Domain.Entities.DataValue>()
            .SingleAsync(v => v.Id == baujahr.ValueId);

        Assert.Equal(ParameterDataType.Integer, stored.DataType);
        Assert.False(stored.IsQuoted);
    }

    /// <summary>
    /// Das Dateiformat quotet Text nicht einheitlich (<c>baujahr="…"</c> mit,
    /// <c>nachname=…</c> ohne). Bleibt ein Feld Text, muss seine bisherige
    /// Schreibweise erhalten bleiben – sonst verändert eine Bearbeitung das
    /// Dateiabbild an einer Stelle, die gar nicht gemeint war.
    /// </summary>
    [Fact]
    public async Task Update_TextBleibtText_BehaeltBisherigeSchreibweise()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);
        var (detail, _) = await ImportAsync(service, _root);

        var nachname = detail.Attributes.Single(a => a.ColumnName == "nachname");
        var baujahr = detail.Attributes.Single(a => a.ColumnName == "baujahr");
        Assert.False(nachname.IsQuoted);
        Assert.True(baujahr.IsQuoted);

        await service.UpdateAttributeValuesAsync(detail.Id,
        [
            new AttributeValueUpdate(nachname.ValueId, "Mueller"),
            new AttributeValueUpdate(baujahr.ValueId, "31.12.1999")
        ]);

        await using var verify = env.CreateFreshContext();
        var werte = await verify.Set<TestCaseManager.Domain.Entities.DataValue>()
            .Where(v => v.Id == nachname.ValueId || v.Id == baujahr.ValueId)
            .ToListAsync();

        Assert.False(werte.Single(v => v.Id == nachname.ValueId).IsQuoted);
        Assert.True(werte.Single(v => v.Id == baujahr.ValueId).IsQuoted);
    }

    [Fact]
    public async Task Update_UnveraenderterWert_WirdNichtGeschrieben()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);
        var (detail, _) = await ImportAsync(service, _root);

        var tuer = detail.Attributes.Single(a => a.ColumnName == "tuer");
        var changed = await service.UpdateAttributeValuesAsync(detail.Id,
            [new AttributeValueUpdate(tuer.ValueId, "2")]);

        Assert.Equal(0, changed);
    }

    [Fact]
    public async Task Update_MehrereWerte_WerdenGemeinsamUebernommen()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);
        var (detail, _) = await ImportAsync(service, _root);

        var tuer = detail.Attributes.Single(a => a.ColumnName == "tuer");
        var nachname = detail.Attributes.Single(a => a.ColumnName == "nachname");

        var changed = await service.UpdateAttributeValuesAsync(detail.Id,
        [
            new AttributeValueUpdate(tuer.ValueId, "9"),
            new AttributeValueUpdate(nachname.ValueId, "Mueller")
        ]);

        Assert.Equal(2, changed);

        var erneut = await service.GetImportedDetailAsync(detail.Id);
        Assert.Equal("9", erneut!.Attributes.Single(a => a.ColumnName == "tuer").Value);
        Assert.Equal("Mueller", erneut.Attributes.Single(a => a.ColumnName == "nachname").Value);
    }

    [Fact]
    public async Task Update_WertEinesAnderenTestfalls_WirdAbgewiesen()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);
        var (first, second) = await ImportAsync(service, _root);

        // Der Wert gehört zum zweiten Testfall, die Id verweist auf den ersten.
        var fremd = second.Attributes.Single(a => a.ColumnName == "tuer");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAttributeValuesAsync(first.Id,
                [new AttributeValueUpdate(fremd.ValueId, "99")]));
        Assert.Contains("gehört nicht zu diesem Testfall", ex.Message);

        // Der fremde Wert bleibt unverändert.
        await using var verify = env.CreateFreshContext();
        var stored = await verify.Set<TestCaseManager.Domain.Entities.DataValue>()
            .SingleAsync(v => v.Id == fremd.ValueId);
        Assert.Equal("2", stored.Value);
    }

    [Fact]
    public async Task Update_UnbekannterTestfall_WirftAussagekraeftigenFehler()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAttributeValuesAsync(Guid.NewGuid(),
                [new AttributeValueUpdate(Guid.NewGuid(), "1")]));
        Assert.Contains("existiert nicht mehr", ex.Message);
    }

    [Fact]
    public async Task Update_LeereListe_AendertNichts()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);
        var (detail, _) = await ImportAsync(service, _root);

        Assert.Equal(0, await service.UpdateAttributeValuesAsync(detail.Id, []));
    }

    [Fact]
    public async Task Update_DannExport_SchreibtGeaendertenWertFormatgleich()
    {
        using var env = new InMemoryTestContext();
        var service = BuildService(env);
        var (detail, _) = await ImportAsync(service, _root);

        var tuer = detail.Attributes.Single(a => a.ColumnName == "tuer");
        var nachname = detail.Attributes.Single(a => a.ColumnName == "nachname");
        await service.UpdateAttributeValuesAsync(detail.Id,
        [
            new AttributeValueUpdate(tuer.ValueId, "7"),
            new AttributeValueUpdate(nachname.ValueId, "Müller-Lüdenscheidt")
        ]);

        var exportRoot = Path.Combine(_root, "Export");
        await service.ExportFolderAsync(exportRoot);

        var written = await File.ReadAllTextAsync(Path.Combine(
            exportRoot, "Input", "0001_TF", "0001_Vertragsbeginn.Eingabe"));

        // Genau wie im Ursprungsformat: Zahl schlicht, nachname ohne
        // Anführungszeichen (so stand es auch vor der Bearbeitung dort).
        Assert.Contains("tuer=7", written);
        Assert.Contains("nachname=Müller-Lüdenscheidt", written);
        Assert.DoesNotContain("nachname=\"", written);
    }

    /// <summary>Entfernt den temporären Ordner.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
