using Api.Application.Abstractions;

namespace Api.Application.Contacts.Commands.CreateContact;

public record CreateContactCommand(
    string? FirstName,
    string? Surname,
    DateOnly? DateOfBirth,
    CreateAddress? Address,
    string? PhoneNumber,
    string? Iban) : ICommand<ContactDto>;

public record CreateAddress(string? Street, string? PostalCode, string? City, string? Country);
