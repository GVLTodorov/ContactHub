using ContactHub.Domain.ValueObjects;

namespace ContactHub.Application.Contacts.Commands;

/// <summary>
/// The editable fields of a contact. Create and update both implement it, so they share the
/// request rules in <see cref="ContactInputValidator{T}"/> without one command being the other.
/// </summary>
public interface IContactInput
{
    string? FirstName { get; }
    string? Surname { get; }
    DateOnly? DateOfBirth { get; }
    AddressInput? Address { get; }
    string? PhoneNumber { get; }
    string? Iban { get; }
}

public record AddressInput(string? Street, string? PostalCode, string? City, string? Country)
{
    public Address ToAddress() => Address.Create(Street, PostalCode, City, Country);
}
