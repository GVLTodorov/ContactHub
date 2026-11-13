using ContactHub.Domain.Entities;
using ContactHub.Domain.Exceptions;
using ContactHub.Domain.ValueObjects;

namespace ContactHub.Api.Tests;

[TestFixture]
public class DomainEdgeTests
{
    [Test]
    public void Address_NullParts_AreRejectedAsEmpty()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Address.Create(null, null, null, null))!;

        Assert.That(
            ex.Errors.Keys,
            Is.EquivalentTo(new[] { "address.street", "address.postalCode", "address.city", "address.country" }));
    }

    [Test]
    public void Contact_Create_NullNames_AreRejectedAsEmpty()
    {
        var address = Address.Create("Main St 1", "10115", "Berlin", "Germany");
        var phone = PhoneNumber.Create("+4930123456");
        var iban = Iban.Create("DE89370400440532013000");
        var now = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        var ex = Assert.Throws<DomainValidationException>(
            () => Contact.Create(null, null, new DateOnly(1990, 1, 1), address, phone, iban, now))!;

        Assert.That(ex.Errors.Keys, Is.EquivalentTo(new[] { "firstName", "surname" }));
    }

    [Test]
    public void Contact_Rename_NullNames_ThrowsAndRestoresState()
    {
        var address = Address.Create("Main St 1", "10115", "Berlin", "Germany");
        var now = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var contact = Contact.Create(
            "Ada", "Lovelace", new DateOnly(1990, 1, 1), address, PhoneNumber.Create("+4930123456"), Iban.Create("DE89370400440532013000"), now);

        Assert.Throws<DomainValidationException>(() => contact.Rename(null, null, now.AddHours(1)));

        Assert.That(contact.FirstName, Is.EqualTo("Ada"));
        Assert.That(contact.Surname, Is.EqualTo("Lovelace"));
    }

    [TestCase("", "")]
    [TestCase("Address", "address")]
    [TestCase("Address.City", "address.city")]
    [TestCase("address..City", "address..city")]
    public void ToCamelCase_ConvertsEachSegment(string input, string expected)
    {
        Assert.That(ValidationKeys.ToCamelCase(input), Is.EqualTo(expected));
    }

    [Test]
    public void DomainValidationException_From_PrefixesAndGroupsErrors()
    {
        var result = new FluentValidation.Results.ValidationResult(
        [
            new FluentValidation.Results.ValidationFailure("City", "a"),
            new FluentValidation.Results.ValidationFailure("City", "a"),
            new FluentValidation.Results.ValidationFailure("City", "b"),
        ]);

        var withPrefix = DomainValidationException.From(result, "address");
        var withoutPrefix = DomainValidationException.From(result);

        Assert.That(withPrefix.Errors["address.city"], Is.EqualTo(new[] { "a", "b" }));
        Assert.That(withoutPrefix.Errors["city"], Is.EqualTo(new[] { "a", "b" }));
    }

    [Test]
    public void Iban_ShortValue_FormatsLastGroupPartially()
    {
        var iban = Iban.Create("NO9386011117947");

        Assert.That(iban.ToFormatted(), Is.EqualTo("NO93 8601 1117 947"));
    }

    [Test]
    public void Iban_WithLetters_ComputesChecksum()
    {
        Assert.That(Iban.IsValid("gb82 west 1234 5698 7654 32"), Is.True);
        Assert.That(Iban.IsValid("GB82WEST1234569876543!"), Is.False);
    }

    [Test]
    public void PhoneNumber_WhitespaceAndNull_AreInvalid()
    {
        Assert.That(PhoneNumber.IsValid("   "), Is.False);
        Assert.That(PhoneNumber.IsValid(null), Is.False);
        Assert.That(() => PhoneNumber.Create(null), Throws.TypeOf<DomainValidationException>());
    }
}
