using Api.Domain.Entities;
using Api.Domain.Exceptions;
using Api.Domain.ValueObjects;

namespace Api.Tests;

[TestFixture]
public class DomainTests
{
    private const string ValidIban = "DE89370400440532013000";
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private static Address ValidAddress() => Address.Create("Main St 1", "10115", "Berlin", "Germany");

    private static Contact NewContact(string first, string last, DateOnly dob) =>
        Contact.Create(first, last, dob, ValidAddress(), PhoneNumber.Create("+4930123456"), Iban.Create(ValidIban), Now);

    [Test]
    public void Iban_Valid_NormalizesAndFormats()
    {
        var iban = Iban.Create("de89 3704 0044 0532 0130 00");

        Assert.That(iban.Value, Is.EqualTo(ValidIban));
        Assert.That(iban.ToFormatted(), Is.EqualTo("DE89 3704 0044 0532 0130 00"));
        Assert.That(iban, Is.EqualTo(Iban.Create(ValidIban)));
    }

    [TestCase("DE89370400440532013001")]
    [TestCase("DE8937040044053201")]
    [TestCase("1234")]
    [TestCase("")]
    [TestCase(null)]
    public void Iban_Invalid_IsRejected(string? raw)
    {
        Assert.That(Iban.IsValid(raw), Is.False);
        Assert.That(() => Iban.Create(raw), Throws.TypeOf<DomainValidationException>());
    }

    [TestCase("+49 (30) 123-4567", "+49301234567")]
    [TestCase("030.123.4567", "0301234567")]
    public void PhoneNumber_Valid_IsNormalized(string raw, string expected)
    {
        Assert.That(PhoneNumber.Create(raw).Value, Is.EqualTo(expected));
    }

    [TestCase("123456")]
    [TestCase("1234567890123456")]
    [TestCase("12ab5678")]
    [TestCase("++1234567")]
    [TestCase("")]
    public void PhoneNumber_Invalid_IsRejected(string raw)
    {
        Assert.That(PhoneNumber.IsValid(raw), Is.False);
        Assert.That(() => PhoneNumber.Create(raw), Throws.TypeOf<DomainValidationException>());
    }

    [Test]
    public void Address_Create_TrimsAndHasValueEquality()
    {
        var a = Address.Create(" Main St 1 ", "10115", "Berlin", "Germany");

        Assert.That(a.Street, Is.EqualTo("Main St 1"));
        Assert.That(a, Is.EqualTo(ValidAddress()));
    }

    [Test]
    public void Address_Invalid_ReportsNestedKeys()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Address.Create("", "10115", new string('x', 101), "Germany"))!;

        Assert.That(ex.Errors.Keys, Is.EquivalentTo(new[] { "address.street", "address.city" }));
    }

    [Test]
    public void Contact_Create_TrimsNamesAndSetsTimestamps()
    {
        var c = NewContact(" Ada ", " Lovelace ", new DateOnly(1990, 1, 1));

        Assert.That(c.FirstName, Is.EqualTo("Ada"));
        Assert.That(c.Surname, Is.EqualTo("Lovelace"));
        Assert.That(c.CreatedAt, Is.EqualTo(Now));
        Assert.That(c.UpdatedAt, Is.EqualTo(Now));
        Assert.That(c.Id.Version, Is.EqualTo(7));
    }

    [Test]
    public void Contact_Create_FutureBirthDate_Throws()
    {
        var ex = Assert.Throws<DomainValidationException>(() => NewContact("Ada", "Lovelace", new DateOnly(2026, 6, 16)))!;

        Assert.That(ex.Errors, Does.ContainKey("dateOfBirth"));
    }

    [Test]
    public void Contact_Create_TooOldBirthDate_Throws()
    {
        var ex = Assert.Throws<DomainValidationException>(() => NewContact("Ada", "Lovelace", new DateOnly(1876, 6, 14)))!;

        Assert.That(ex.Errors, Does.ContainKey("dateOfBirth"));
    }

    [Test]
    public void Contact_Create_BlankName_Throws()
    {
        var ex = Assert.Throws<DomainValidationException>(() => NewContact("  ", "Lovelace", new DateOnly(1990, 1, 1)))!;

        Assert.That(ex.Errors, Does.ContainKey("firstName"));
    }

    private static readonly DateTime Later = Now.AddHours(1);

    [Test]
    public void Change_WithSameValues_LeavesUpdatedAtUnchanged()
    {
        var c = NewContact("Ada", "Lovelace", new DateOnly(1990, 1, 1));

        c.Rename(" Ada ", "Lovelace", Later);
        c.ChangeDateOfBirth(new DateOnly(1990, 1, 1), Later);
        c.ChangeAddress(ValidAddress(), Later);
        c.ChangePhoneNumber(PhoneNumber.Create("+49 30 123456"), Later);
        c.ChangeIban(Iban.Create(ValidIban), Later);

        Assert.That(c.UpdatedAt, Is.EqualTo(Now));
    }

    [Test]
    public void Change_WithNewValues_BumpsUpdatedAt()
    {
        var c = NewContact("Ada", "Lovelace", new DateOnly(1990, 1, 1));

        c.Rename("Grace", "Hopper", Later);
        Assert.That(c.FirstName, Is.EqualTo("Grace"));
        Assert.That(c.UpdatedAt, Is.EqualTo(Later));

        var later2 = Later.AddHours(1);
        c.ChangeDateOfBirth(new DateOnly(1985, 5, 5), later2);
        Assert.That(c.UpdatedAt, Is.EqualTo(later2));

        var later3 = later2.AddHours(1);
        c.ChangeAddress(Address.Create("Other St 2", "20095", "Hamburg", "Germany"), later3);
        Assert.That(c.UpdatedAt, Is.EqualTo(later3));

        var later4 = later3.AddHours(1);
        c.ChangePhoneNumber(PhoneNumber.Create("+4940999999"), later4);
        Assert.That(c.UpdatedAt, Is.EqualTo(later4));

        var later5 = later4.AddHours(1);
        c.ChangeIban(Iban.Create("GB82WEST12345698765432"), later5);
        Assert.That(c.UpdatedAt, Is.EqualTo(later5));
        Assert.That(c.CreatedAt, Is.EqualTo(Now));
    }

    [Test]
    public void Change_InvalidValue_ThrowsAndKeepsStateAndUpdatedAt()
    {
        var c = NewContact("Ada", "Lovelace", new DateOnly(1990, 1, 1));

        Assert.That(() => c.Rename(" ", "Lovelace", Later), Throws.TypeOf<DomainValidationException>());
        Assert.That(() => c.ChangeDateOfBirth(new DateOnly(2030, 1, 1), Later), Throws.TypeOf<DomainValidationException>());

        Assert.That(c.FirstName, Is.EqualTo("Ada"));
        Assert.That(c.DateOfBirth, Is.EqualTo(new DateOnly(1990, 1, 1)));
        Assert.That(c.UpdatedAt, Is.EqualTo(Now));
    }
}
