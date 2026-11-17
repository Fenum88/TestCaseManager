using TestCaseManager.Application.Common.Models;

namespace TestCaseManager.Tests.Services;

/// <summary>
/// Tests für den CSV-Export/-Import der Abgleichprofile
/// (<see cref="CompareProfileCsv"/>): Rundlauf, Maskierung von Sonderzeichen,
/// manuell bearbeitete Dateien und fehlerhafte Eingaben.
/// </summary>
public class CompareProfileCsvTests
{
    private static readonly CompareProfile[] Beispiel =
    [
        new()
        {
            Name = "Provision nach Datum",
            Description = "Ordnet über das Datum zu.",
            KeyRules =
            [
                new CompareKeyRule { Entity = "Provision", KeyColumns = ["datum"] },
                new CompareKeyRule { Entity = "Raeume", KeyColumns = ["gebaeudeId", "raumnummer"] }
            ]
        },
        new() { Name = "Nur Position", Description = "Ohne Regeln." }
    ];

    [Fact]
    public void Write_ErzeugtKopfzeileUndEineZeileJeRegel()
    {
        var csv = CompareProfileCsv.Write(Beispiel);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r')).ToList();

        Assert.Equal("Profil;Beschreibung;Entität;Schlüsselattribute", lines[0]);
        Assert.Equal("Provision nach Datum;Ordnet über das Datum zu.;Provision;datum", lines[1]);
        Assert.Equal("Provision nach Datum;Ordnet über das Datum zu.;Raeume;gebaeudeId, raumnummer", lines[2]);
        // Profil ohne Regeln: Entität und Schlüssel leer.
        Assert.Equal("Nur Position;Ohne Regeln.;;", lines[3]);
    }

    [Fact]
    public void RoundTrip_LiefertDieProfileInhaltsgleichZurueck()
    {
        var csv = CompareProfileCsv.Write(Beispiel);

        Assert.True(CompareProfileCsv.TryParse(csv, out var profiles, out var error), error);
        Assert.Equal(2, profiles.Count);

        var ersteres = profiles[0];
        Assert.Equal("Provision nach Datum", ersteres.Name);
        Assert.Equal("Ordnet über das Datum zu.", ersteres.Description);
        Assert.Equal(2, ersteres.KeyRules.Count);
        Assert.Equal(["datum"], ersteres.KeyRules[0].KeyColumns);
        Assert.Equal(["gebaeudeId", "raumnummer"], ersteres.KeyRules[1].KeyColumns);

        Assert.Equal("Nur Position", profiles[1].Name);
        Assert.Empty(profiles[1].KeyRules);
    }

    [Fact]
    public void RoundTrip_MaskiertSemikolonUndAnfuehrungszeichen()
    {
        CompareProfile[] heikel =
        [
            new()
            {
                Name = "Profil; mit Semikolon",
                Description = "Er sagte \"hallo\"; dann ging er.",
                KeyRules = [new CompareKeyRule { Entity = "Provision", KeyColumns = ["datum"] }]
            }
        ];

        var csv = CompareProfileCsv.Write(heikel);
        Assert.True(CompareProfileCsv.TryParse(csv, out var profiles, out var error), error);

        var profil = Assert.Single(profiles);
        Assert.Equal("Profil; mit Semikolon", profil.Name);
        Assert.Equal("Er sagte \"hallo\"; dann ging er.", profil.Description);
    }

    [Fact]
    public void Parse_ManuellBearbeiteteDatei_WirdUebernommen()
    {
        // So, wie es jemand in Excel tippen würde – auch ohne Kopfzeile
        // und mit Leerzeichen um die Schlüsselattribute.
        var csv =
            "Mein Profil;Selbst gebaut;Provision; datum , art \n" +
            "Mein Profil;;Gebaeude;gebaeudeId\n";

        Assert.True(CompareProfileCsv.TryParse(csv, out var profiles, out var error), error);

        var profil = Assert.Single(profiles);
        Assert.Equal("Mein Profil", profil.Name);
        Assert.Equal("Selbst gebaut", profil.Description);
        Assert.Equal(2, profil.KeyRules.Count);
        Assert.Equal(["datum", "art"], profil.KeyRules[0].KeyColumns);
        Assert.Equal(["gebaeudeId"], profil.KeyRules[1].KeyColumns);
    }

    [Fact]
    public void Parse_LeereZeilenWerdenUebersprungen()
    {
        var csv = CompareProfileCsv.Write(Beispiel) + "\n\n";

        Assert.True(CompareProfileCsv.TryParse(csv, out var profiles, out _));
        Assert.Equal(2, profiles.Count);
    }

    [Theory]
    [InlineData("", "leer")]
    [InlineData("   ", "leer")]
    [InlineData("Profil;Beschreibung;Entität;Schlüsselattribute\n", "keine Profile")]
    [InlineData("Profil;Beschreibung;Entität;Schlüsselattribute\n;ohne Namen;Provision;datum\n", "Profilname")]
    [InlineData("Profil;Beschreibung;Entität;Schlüsselattribute\nP;;Provision;\n", "Schlüsselattribute")]
    [InlineData("Profil;Beschreibung;Entität;Schlüsselattribute\nP;nur zwei\n", "mindestens")]
    public void Parse_UngueltigeEingaben_LiefernVerstaendlichenFehler(string csv, string erwartet)
    {
        Assert.False(CompareProfileCsv.TryParse(csv, out var profiles, out var error));
        Assert.Empty(profiles);
        Assert.NotNull(error);
        Assert.Contains(erwartet, error, StringComparison.OrdinalIgnoreCase);
    }
}
