using ContactHub.Application.Abstractions;

namespace ContactHub.Application.Contacts.Commands.UpdateContact;

/// <param name="Version">The contact version the edit started from; a stale one is rejected with a conflict.</param>
public record UpdateContactCommand(
    Guid Id,
    int? Version,
    string? FirstName,
    string? Surname,
    DateOnly? DateOfBirth,
    AddressInput? Address,
    string? PhoneNumber,
    string? Iban) : ICommand<ContactDto>, IContactInput;
