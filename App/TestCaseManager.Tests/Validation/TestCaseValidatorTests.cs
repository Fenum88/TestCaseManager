using TestCaseManager.Application.Validation;
using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Tests.Validation;

/// <summary>
/// Tests für die Testfall-Validierung (Pflichtfelder, Maximallängen,
/// Datentypen, doppelte Parameternamen).
/// </summary>
public class TestCaseValidatorTests
{
    private readonly TestCaseValidator _validator = new();

    private static TestCase ValidTestCase() => new()
    {
        Name = "Gültiger Testfall",
        Version = "1.0",
        Status = TestCaseStatus.Draft
    };

    [Fact]
    public void Validate_GueltigerTestfall_LiefertKeineFehler()
    {
        var errors = _validator.Validate(ValidTestCase());

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_FehlenderName_LiefertFehler()
    {
        var testCase = ValidTestCase();
        testCase.Name = "   ";

        var errors = _validator.Validate(testCase);

        Assert.Contains(errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_ZuLangerName_LiefertFehler()
    {
        var testCase = ValidTestCase();
        testCase.Name = new string('x', TestCaseValidator.MaxNameLength + 1);

        var errors = _validator.Validate(testCase);

        Assert.Contains(errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_DoppelteParameternamen_LiefertFehler()
    {
        var testCase = ValidTestCase();
        testCase.Parameters.Add(new TestParameter { Name = "Zinssatz", DataType = ParameterDataType.Text });
        testCase.Parameters.Add(new TestParameter { Name = "zinssatz", DataType = ParameterDataType.Text });

        var errors = _validator.Validate(testCase);

        Assert.Contains(errors, e => e.Message.Contains("mehrfach"));
    }

    [Fact]
    public void Validate_ParameterWertPasstNichtZumDatentyp_LiefertFehler()
    {
        var testCase = ValidTestCase();
        testCase.Parameters.Add(new TestParameter
        {
            Name = "Anzahl",
            DataType = ParameterDataType.Integer,
            Value = "kein Zahlenwert"
        });

        var errors = _validator.Validate(testCase);

        Assert.Contains(errors, e => e.PropertyName.Contains("Anzahl"));
    }

    [Fact]
    public void Validate_PflichtParameterOhneWert_LiefertFehler()
    {
        var testCase = ValidTestCase();
        testCase.Parameters.Add(new TestParameter
        {
            Name = "Pflichtparameter",
            DataType = ParameterDataType.Text,
            IsRequired = true,
            Value = null,
            DefaultValue = null
        });

        var errors = _validator.Validate(testCase);

        Assert.Contains(errors, e => e.Message.Contains("Pflichtfeld"));
    }

    [Fact]
    public void Validate_ErwartetesErgebnisOhneWert_LiefertFehler()
    {
        var testCase = ValidTestCase();
        testCase.ExpectedResults.Add(new ExpectedResult
        {
            Name = "Summe",
            DataType = ParameterDataType.Decimal,
            ExpectedValue = string.Empty
        });

        var errors = _validator.Validate(testCase);

        Assert.Contains(errors, e => e.PropertyName.Contains("Summe"));
    }
}
