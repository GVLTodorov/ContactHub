using ContactHub.Domain.Entities;

namespace ContactHub.Application.Contacts;

public record AddressDto(string Street, string PostalCode, string City, string Country);

public record ContactDto(
    Guid Id,
    string FirstName,
    string Surname,
    DateOnly DateOfBirth,
    AddressDto Address,
    string PhoneNumber,
    string Iban,
    string IbanFormatted,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ContactDto From(Contact c) => new(
        c.Id,
        c.FirstName,
        c.Surname,
        c.DateOfBirth,
        new AddressDto(c.Address.Street, c.Address.PostalCode, c.Address.City, c.Address.Country),
        c.PhoneNumber.Value,
        c.Iban.Value,
        c.Iban.ToFormatted(),
        c.CreatedAt,
        c.UpdatedAt);
}
