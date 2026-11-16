using ContactHub.Domain.ValueObjects;

namespace ContactHub.Application.Contacts.Commands;

/// <summary>
/// The editable fields of a contact. Create and update both implement it, so they share the
/// request rules in <see cref="ContactInputValidator{T}"/> without one command being the other.
/// </summary>
public interface IContactInput
{
    /// <summary>The contact's first name. Required, up to <c>Contact.MaxNameLength</c> characters.</summary>
    string? FirstName { get; }

    /// <summary>The contact's surname. Required, up to <c>Contact.MaxNameLength</c> characters.</summary>
    string? Surname { get; }

    /// <summary>
    /// The contact's date of birth. Required; it can't be in the future or more than
    /// <c>Contact.MaxAgeYears</c> years ago.
    /// </summary>
    DateOnly? DateOfBirth { get; }

    /// <summary>The contact's postal address. Required; every part of it must be filled in.</summary>
    AddressInput? Address { get; }

    /// <summary>The contact's phone number. Required and must be a valid phone number.</summary>
    string? PhoneNumber { get; }

    /// <summary>The contact's bank account number. Required and must be a valid IBAN.</summary>
    string? Iban { get; }
}

public record AddressInput(string? Street, string? PostalCode, string? City, string? Country)
{
    public Address ToAddress() => Address.Create(Street, PostalCode, City, Country);
}
