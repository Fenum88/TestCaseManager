using TestCaseManager.Application.Common.Validation;
using TestCaseManager.Domain.Enums;

namespace TestCaseManager.Tests.Validation;

/// <summary>
/// Tests für die datentypbezogene Wertprüfung <see cref="ParameterValueParser"/>.
/// </summary>
public class ParameterValueParserTests
{
    [Theory]
    [InlineData("42", ParameterDataType.Integer, true)]
    [InlineData("-7", ParameterDataType.Integer, true)]
    [InlineData("3,5", ParameterDataType.Integer, false)]
    [InlineData("abc", ParameterDataType.Integer, false)]
    [InlineData("3,5", ParameterDataType.Decimal, true)]   // deutsche Schreibweise
    [InlineData("3.5", ParameterDataType.Decimal, true)]   // invariante Schreibweise
    [InlineData("ja", ParameterDataType.Boolean, true)]
    [InlineData("false", ParameterDataType.Boolean, true)]
    [InlineData("vielleicht", ParameterDataType.Boolean, false)]
    [InlineData("2026-01-15", ParameterDataType.Date, true)]
    [InlineData("kein Datum", ParameterDataType.Date, false)]
    [InlineData("{\"a\":1}", ParameterDataType.Json, true)]
    [InlineData("{ungültig", ParameterDataType.Json, false)]
    [InlineData("beliebiger Text", ParameterDataType.Text, true)]
    public void IsValid_PrueftWertGegenDatentyp(string value, ParameterDataType dataType, bool expected)
    {
        var result = ParameterValueParser.IsValid(value, dataType);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(ParameterDataType.Integer)]
    [InlineData(ParameterDataType.Decimal)]
    [InlineData(ParameterDataType.Json)]
    public void IsValid_LeererWert_GiltAlsGueltig(ParameterDataType dataType)
    {
        // Pflichtfeldprüfungen übernimmt der Validator, nicht der Parser.
        Assert.True(ParameterValueParser.IsValid(null, dataType));
        Assert.True(ParameterValueParser.IsValid("   ", dataType));
    }
}
