using System.Text.Json;
using ContactHub.Domain.Entities;
using ContactHub.Domain.ValueObjects;

namespace ContactHub.Api.Tests.Domain;

/// <summary>
/// The Angular form validates with frontend/src/app/components/contact-form/contact-rules.json.
/// These tests keep that file in step with the domain, so the browser and the API never disagree.
/// </summary>
[TestFixture]
public class FrontendRulesTests
{
    private static JsonElement _rules;

    [OneTimeSetUp]
    public void LoadRules()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "global.json")))
        {
            dir = dir.Parent;
        }

        Assert.That(dir, Is.Not.Null, "repo root (global.json) not found");
        var path = Path.Combine(dir!.FullName, "frontend", "src", "app", "components", "contact-form", "contact-rules.json");
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        _rules = json.RootElement.Clone();
    }

    private static int MaxLength(string field) => _rules.GetProperty("maxLength").GetProperty(field).GetInt32();

    [Test]
    public void MaxLengths_MatchDomain()
    {
        Assert.That(MaxLength("firstName"), Is.EqualTo(Contact.MaxNameLength));
        Assert.That(MaxLength("surname"), Is.EqualTo(Contact.MaxNameLength));
        Assert.That(MaxLength("street"), Is.EqualTo(Address.MaxStreet));
        Assert.That(MaxLength("postalCode"), Is.EqualTo(Address.MaxPostalCode));
        Assert.That(MaxLength("city"), Is.EqualTo(Address.MaxCity));
        Assert.That(MaxLength("country"), Is.EqualTo(Address.MaxCountry));
    }

    [Test]
    public void AgeAndPhoneLimits_MatchDomain()
    {
        var phone = _rules.GetProperty("phoneDigits");

        Assert.That(_rules.GetProperty("maxAgeYears").GetInt32(), Is.EqualTo(Contact.MaxAgeYears));
        Assert.That(phone.GetProperty("min").GetInt32(), Is.EqualTo(PhoneNumber.MinDigits));
        Assert.That(phone.GetProperty("max").GetInt32(), Is.EqualTo(PhoneNumber.MaxDigits));
    }

    [Test]
    public void IbanLengths_MatchDomain()
    {
        var frontend = _rules.GetProperty("ibanLengths").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetInt32());

        Assert.That(frontend, Is.EquivalentTo(Iban.CountryLengths));
    }
}
