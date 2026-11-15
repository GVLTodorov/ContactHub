using ContactHub.Application.Contacts.Commands;
using ContactHub.Application.Contacts.Commands.CreateContact;
using ContactHub.Application.Contacts.Commands.UpdateContact;

namespace ContactHub.Api.Contracts;

// HTTP request bodies. The controllers map them to Application commands, so the wire format can
// change without touching the commands, and route values (the id) never come from the body.

public record AddressRequest(string? Street, string? PostalCode, string? City, string? Country)
{
    public AddressInput ToInput() => new(Street, PostalCode, City, Country);
}

public record CreateContactRequest(
    string? FirstName,
    string? Surname,
    DateOnly? DateOfBirth,
    AddressRequest? Address,
    string? PhoneNumber,
    string? Iban)
{
    public CreateContactCommand ToCommand() =>
        new(FirstName, Surname, DateOfBirth, Address?.ToInput(), PhoneNumber, Iban);
}

/// <param name="Version">The <c>version</c> of the contact as it was read; a stale one gets 409 Conflict.</param>
public record UpdateContactRequest(
    int? Version,
    string? FirstName,
    string? Surname,
    DateOnly? DateOfBirth,
    AddressRequest? Address,
    string? PhoneNumber,
    string? Iban)
{
    public UpdateContactCommand ToCommand(Guid id) =>
        new(id, Version, FirstName, Surname, DateOfBirth, Address?.ToInput(), PhoneNumber, Iban);
}
