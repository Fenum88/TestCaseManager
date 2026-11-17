using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;
using TestCaseManager.Domain.Factories;

namespace TestCaseManager.Tests.Domain;

/// <summary>
/// Tests für die Testfall-Fabrik (Erzeugung und tiefes Kopieren).
/// </summary>
public class TestCaseFactoryTests
{
    [Fact]
    public void CreateNew_LiefertEntwurfMitStandardwerten()
    {
        var testCase = TestCaseFactory.CreateNew();

        Assert.Equal(TestCaseStatus.Draft, testCase.Status);
        Assert.Equal("1.0", testCase.Version);
        Assert.NotEqual(Guid.Empty, testCase.Id);
    }

    [Fact]
    public void CreateCopy_KopiertParameterUndErgebnisseTief()
    {
        var source = new TestCase
        {
            Name = "Original",
            Description = "Beschreibung",
            Version = "2.0",
            Status = TestCaseStatus.Active
        };
        source.Parameters.Add(new TestParameter { Name = "P1", Value = "1", DataType = ParameterDataType.Integer });
        source.ExpectedResults.Add(new ExpectedResult { Name = "R1", ExpectedValue = "42" });

        var copy = TestCaseFactory.CreateCopy(source, "Kopie");

        Assert.Equal("Kopie", copy.Name);
        Assert.Equal(TestCaseStatus.Draft, copy.Status);       // Kopie startet als Entwurf
        Assert.NotEqual(source.Id, copy.Id);                   // eigener Schlüssel
        Assert.Single(copy.Parameters);
        Assert.Single(copy.ExpectedResults);

        // Tiefe Kopie: Änderung der Kopie wirkt nicht auf das Original zurück.
        copy.Parameters[0].Value = "999";
        Assert.Equal("1", source.Parameters[0].Value);
        Assert.NotEqual(source.Parameters[0].Id, copy.Parameters[0].Id);
    }

    [Fact]
    public void CreateCopy_OhneName_WirftAusnahme()
    {
        var source = new TestCase { Name = "Original" };

        Assert.Throws<ArgumentException>(() => TestCaseFactory.CreateCopy(source, "  "));
    }
}
