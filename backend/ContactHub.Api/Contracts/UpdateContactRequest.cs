using ContactHub.Application.Contacts.Commands.CreateContact;

namespace ContactHub.Api.Contracts;

public record UpdateContactRequest(
    string? FirstName,
    string? Surname,
    DateOnly? DateOfBirth,
    CreateAddress? Address,
    string? PhoneNumber,
    string? Iban);
