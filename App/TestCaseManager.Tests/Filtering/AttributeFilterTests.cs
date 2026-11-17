using TestCaseManager.Application.Common.Filtering;

namespace TestCaseManager.Tests.Filtering;

/// <summary>
/// Tests für den Attributfilter (<see cref="AttributeFilter"/>): Parsen und
/// Auswerten von Ausdrücken wie <c>tuer=2 AND nachname=Bongo</c>.
/// </summary>
public class AttributeFilterTests
{
    // Ein Testfall (Gebaeude + zwei Personen), wie er in der Datenbank läge.
    private static readonly (string Column, string? Value)[] TestCase0001 =
    [
        ("tuer", "2"),
        ("baujahr", "01.01.1965"),
        ("stockwerke", "4"),
        ("name", "Rainer"),
        ("nachname", "Bongo"),
        ("name", "Sarah"),
        ("nachname", "Bongo")
    ];

    private static bool Match(string filter, (string, string?)[] attrs)
    {
        Assert.True(AttributeFilter.TryParse(filter, out var parsed, out var error), error);
        return parsed!.Matches(attrs);
    }

    [Theory]
    [InlineData("tuer=2", true)]
    [InlineData("tuer=3", false)]
    [InlineData("nachname=Bongo", true)]
    [InlineData("tuer=2 AND nachname=Bongo", true)]
    [InlineData("tuer=3 AND nachname=Bongo", false)]
    [InlineData("tuer=3 OR nachname=Bongo", true)]
    [InlineData("tuer=3 OR nachname=Meier", false)]
    public void Match_EinfacheUndVerknuepfteBedingungen(string filter, bool erwartet)
    {
        Assert.Equal(erwartet, Match(filter, TestCase0001));
    }

    [Theory]
    [InlineData("stockwerke>3", true)]
    [InlineData("stockwerke>4", false)]
    [InlineData("stockwerke>=4", true)]
    [InlineData("stockwerke<5", true)]
    [InlineData("tuer!=3", true)]
    [InlineData("name~ai", true)]     // "Rainer" enthält "ai"
    [InlineData("name~xyz", false)]
    public void Match_Vergleichs_UndEnthaeltOperatoren(string filter, bool erwartet)
    {
        Assert.Equal(erwartet, Match(filter, TestCase0001));
    }

    [Fact]
    public void Match_KlammernUndPraezedenz()
    {
        // AND bindet stärker als OR: ohne Klammern wäre das Ergebnis anders.
        Assert.True(Match("tuer=3 OR (tuer=2 AND nachname=Bongo)", TestCase0001));
        Assert.False(Match("(tuer=3 OR tuer=9) AND nachname=Bongo", TestCase0001));
    }

    [Fact]
    public void Match_QuotierterWertMitPunkten()
    {
        Assert.True(Match("baujahr=\"01.01.1965\"", TestCase0001));
        Assert.True(Match("baujahr=01.01.1965", TestCase0001));
    }

    [Fact]
    public void Match_ZahlIstFormatunabhaengig()
    {
        // "4" und "4.0" sind numerisch gleich.
        Assert.True(Match("stockwerke=4.0", TestCase0001));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("tuer")]
    [InlineData("tuer=")]
    [InlineData("tuer 2")]
    [InlineData("tuer=2 AND")]
    [InlineData("(tuer=2")]
    [InlineData("tuer=2 UND nachname=Bongo")]  // "UND" ist kein Operand nach Bedingung
    public void TryParse_UngueltigeAusdruecke_LiefernFehler(string filter)
    {
        Assert.False(AttributeFilter.TryParse(filter, out var parsed, out var error));
        Assert.Null(parsed);
        Assert.NotNull(error);
    }
}
