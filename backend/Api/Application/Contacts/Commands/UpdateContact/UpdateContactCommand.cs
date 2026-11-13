using Api.Application.Contacts.Commands.CreateContact;

namespace Api.Application.Contacts.Commands.UpdateContact;

public record UpdateContactCommand(
    Guid Id,
    string? FirstName,
    string? Surname,
    DateOnly? DateOfBirth,
    CreateAddress? Address,
    string? PhoneNumber,
    string? Iban) : CreateContactCommand(FirstName, Surname, DateOfBirth, Address, PhoneNumber, Iban);
