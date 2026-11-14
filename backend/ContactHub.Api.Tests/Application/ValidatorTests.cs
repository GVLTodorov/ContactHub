using ContactHub.Api.Tests.Common;
using ContactHub.Application.Contacts;
using ContactHub.Application.Contacts.Commands;
using ContactHub.Application.Contacts.Commands.CreateContact;
using ContactHub.Application.Contacts.Commands.DeleteContact;
using ContactHub.Application.Contacts.Commands.UpdateContact;
using ContactHub.Application.Contacts.Queries.GetContactById;
using ContactHub.Application.Contacts.Queries.GetContacts;
using FluentValidation.Results;

namespace ContactHub.Api.Tests.Application;

[TestFixture]
public class ValidatorTests
{
    private readonly CreateContactValidator _create = new(new FixedTimeProvider());

    private static string[] Keys(ValidationResult result) => result.Errors.Select(e => e.PropertyName).Distinct().ToArray();

    [Test]
    public void ValidCommand_Passes()
    {
        var result = _create.Validate(Commands.ValidCreate());

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void UpdateValidator_ValidCommand_Passes()
    {
        var validator = new UpdateContactValidator(new FixedTimeProvider());

        var result = validator.Validate(Commands.ValidUpdate(Guid.NewGuid()));

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(null)]
    [TestCase(0)]
    public void UpdateValidator_MissingOrInvalidVersion_IsRejected(int? version)
    {
        var validator = new UpdateContactValidator(new FixedTimeProvider());

        var result = validator.Validate(Commands.ValidUpdate(Guid.NewGuid(), version: version));

        Assert.That(Keys(result), Is.EqualTo(new[] { "Version" }));
    }

    [Test]
    public void EverythingNull_ReportsEachRequiredField()
    {
        var result = _create.Validate(new CreateContactCommand(null, null, null, null, null, null));

        Assert.That(Keys(result), Is.EquivalentTo(new[] { "FirstName", "Surname", "DateOfBirth", "Address", "PhoneNumber", "Iban" }));
    }

    [Test]
    public void FutureBirthDate_IsRejected()
    {
        var result = _create.Validate(Commands.ValidCreate() with { DateOfBirth = new DateOnly(2026, 6, 16) });

        Assert.That(result.Errors.Single().ErrorMessage, Does.Contain("future"));
    }

    [Test]
    public void TodayBirthDate_IsAccepted()
    {
        var result = _create.Validate(Commands.ValidCreate() with { DateOfBirth = new DateOnly(2026, 6, 15) });

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void TooOldBirthDate_IsRejected()
    {
        var result = _create.Validate(Commands.ValidCreate() with { DateOfBirth = new DateOnly(1876, 6, 14) });

        Assert.That(result.Errors.Single().ErrorMessage, Does.Contain("150 years"));
    }

    [Test]
    public void OldestAllowedBirthDate_IsAccepted()
    {
        var result = _create.Validate(Commands.ValidCreate() with { DateOfBirth = new DateOnly(1876, 6, 15) });

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void InvalidPhoneAndIban_AreRejected()
    {
        var result = _create.Validate(Commands.ValidCreate() with { PhoneNumber = "abc", Iban = "DE00" });

        Assert.That(Keys(result), Is.EquivalentTo(new[] { "PhoneNumber", "Iban" }));
    }

    [Test]
    public void TooLongValues_AreRejected()
    {
        var command = Commands.ValidCreate() with
        {
            FirstName = new string('a', 101),
            Surname = new string('b', 101),
            Address = new AddressInput(new string('s', 201), new string('p', 21), new string('c', 101), new string('k', 101)),
        };

        var result = _create.Validate(command);

        Assert.That(
            Keys(result),
            Is.EquivalentTo(new[] { "FirstName", "Surname", "Address.Street", "Address.PostalCode", "Address.City", "Address.Country" }));
    }

    [Test]
    public void EmptyAddressFields_AreRejected()
    {
        var result = _create.Validate(Commands.ValidCreate() with { Address = new AddressInput("", " ", null, "") });

        Assert.That(Keys(result), Is.EquivalentTo(new[] { "Address.Street", "Address.PostalCode", "Address.City", "Address.Country" }));
    }
}
