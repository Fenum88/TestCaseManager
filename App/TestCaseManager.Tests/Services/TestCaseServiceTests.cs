using Microsoft.EntityFrameworkCore;
using TestCaseManager.Application.Common.Models;
using TestCaseManager.Application.Common.Validation;
using TestCaseManager.Domain.Entities;
using TestCaseManager.Domain.Enums;
using TestCaseManager.Tests.TestSupport;

namespace TestCaseManager.Tests.Services;

/// <summary>
/// Integrationstests für <see cref="TestCaseManager.Application.Services.TestCaseService"/>
/// gegen eine EF-Core-InMemory-Datenbank. Prüfen die Geschäftslogik
/// (Speichern, Eindeutigkeit, Kopieren, Löschen) einschließlich der
/// automatischen Audit- und Historienerzeugung im DbContext.
/// </summary>
public class TestCaseServiceTests
{
    private static TestCase NewTestCase(string name) => new()
    {
        Name = name,
        Version = "1.0",
        Status = TestCaseStatus.Draft
    };

    [Fact]
    public async Task SaveAsync_NeuerTestfall_WirdGespeichertUndAuditGesetzt()
    {
        using var env = new InMemoryTestContext("erika");

        var testCase = NewTestCase("Zinsberechnung Basis");
        testCase.Parameters.Add(new TestParameter { Name = "Zinssatz", DataType = ParameterDataType.Decimal, Value = "3,5" });

        await env.TestCaseService.SaveAsync(testCase, "Zinsberechnung", ["Regression"]);

        await using var verify = env.CreateFreshContext();
        var saved = await verify.TestCases
            .Include(t => t.Parameters)
            .Include(t => t.Category)
            .Include(t => t.Tags)
            .SingleAsync();

        Assert.Equal("Zinsberechnung Basis", saved.Name);
        Assert.Equal("erika", saved.CreatedBy);
        Assert.Equal(env.DateTimeProvider.UtcNow, saved.CreatedAt);
        Assert.Single(saved.Parameters);
        Assert.Equal("Zinsberechnung", saved.Category!.Name);
        Assert.Contains(saved.Tags, t => t.Name == "Regression");
    }

    [Fact]
    public async Task SaveAsync_DoppelterName_WirftValidationException()
    {
        using var env = new InMemoryTestContext();
        await env.TestCaseService.SaveAsync(NewTestCase("Eindeutig"), null, []);

        var duplicate = NewTestCase("eindeutig"); // andere Groß-/Kleinschreibung

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => env.TestCaseService.SaveAsync(duplicate, null, []));

        Assert.Contains(ex.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task SaveAsync_UngueltigeEingabe_WirftValidationException()
    {
        using var env = new InMemoryTestContext();
        var testCase = NewTestCase("Mit fehlerhaftem Parameter");
        testCase.Parameters.Add(new TestParameter
        {
            Name = "Anzahl",
            DataType = ParameterDataType.Integer,
            Value = "keine Zahl"
        });

        await Assert.ThrowsAsync<ValidationException>(
            () => env.TestCaseService.SaveAsync(testCase, null, []));
    }

    [Fact]
    public async Task SaveAsync_BestehenderTestfall_SchreibtHistorieMitAltemUndNeuemWert()
    {
        using var env = new InMemoryTestContext();
        var testCase = NewTestCase("Statuswechsel");
        await env.TestCaseService.SaveAsync(testCase, null, []);

        // Zweiter Vorgang: Status ändern.
        var loaded = await env.TestCaseService.GetWithDetailsAsync(testCase.Id);
        loaded!.Status = TestCaseStatus.Active;
        await env.TestCaseService.SaveAsync(loaded, null, []);

        await using var verify = env.CreateFreshContext();
        var statusChange = await verify.TestCaseHistories
            .Where(h => h.PropertyName == "Status" && h.ChangeType == ChangeType.Modified)
            .SingleAsync();

        Assert.Equal("Entwurf", statusChange.OldValue);
        Assert.Equal("Aktiv", statusChange.NewValue);
    }

    [Fact]
    public async Task CopyAsync_ErzeugtEindeutigeKopie()
    {
        using var env = new InMemoryTestContext();
        var testCase = NewTestCase("Vorlage");
        testCase.Parameters.Add(new TestParameter { Name = "P", DataType = ParameterDataType.Text, Value = "x" });
        await env.TestCaseService.SaveAsync(testCase, null, []);

        var copy = await env.TestCaseService.CopyAsync(testCase.Id);

        Assert.Equal("Vorlage (Kopie)", copy.Name);

        await using var verify = env.CreateFreshContext();
        Assert.Equal(2, await verify.TestCases.CountAsync());
        var copyParams = await verify.TestParameters.Where(p => p.TestCaseId == copy.Id).ToListAsync();
        Assert.Single(copyParams);
    }

    [Fact]
    public async Task DeleteAsync_EntferntTestfallUndUntergeordneteElemente()
    {
        using var env = new InMemoryTestContext();
        var testCase = NewTestCase("Zu löschen");
        testCase.Parameters.Add(new TestParameter { Name = "P", DataType = ParameterDataType.Text });
        testCase.ExpectedResults.Add(new ExpectedResult { Name = "R", ExpectedValue = "1" });
        await env.TestCaseService.SaveAsync(testCase, null, []);

        await env.TestCaseService.DeleteAsync(testCase.Id);

        await using var verify = env.CreateFreshContext();
        Assert.Equal(0, await verify.TestCases.CountAsync());
        Assert.Equal(0, await verify.TestParameters.CountAsync());
        Assert.Equal(0, await verify.ExpectedResults.CountAsync());

        // Die Löschung ist im Audit-Log dokumentiert.
        Assert.Contains(await verify.AuditLogEntries.ToListAsync(),
            a => a.Action == ChangeType.Deleted && a.EntityName == "Testfall");
    }

    [Fact]
    public async Task SearchAsync_FiltertNachFreitextUndStatus()
    {
        using var env = new InMemoryTestContext();
        await env.TestCaseService.SaveAsync(
            new TestCase { Name = "Alpha Zins", Version = "1.0", Status = TestCaseStatus.Active }, null, []);
        await env.TestCaseService.SaveAsync(
            new TestCase { Name = "Beta Rundung", Version = "1.0", Status = TestCaseStatus.Draft }, null, []);

        var byText = await env.TestCaseService.SearchAsync(new TestCaseFilter { SearchText = "zins" });
        Assert.Single(byText);
        Assert.Equal("Alpha Zins", byText[0].Name);

        var byStatus = await env.TestCaseService.SearchAsync(new TestCaseFilter { Status = TestCaseStatus.Draft });
        Assert.Single(byStatus);
        Assert.Equal("Beta Rundung", byStatus[0].Name);
    }
}
