using ContactHub.Application.Abstractions;

namespace ContactHub.Application.Contacts.Commands.CreateContact;

public record CreateContactCommand(
    string? FirstName,
    string? Surname,
    DateOnly? DateOfBirth,
    AddressInput? Address,
    string? PhoneNumber,
    string? Iban) : ICommand<ContactDto>, IContactInput;
