using FluentValidation;

namespace Api.Domain;

public sealed record Address
{
    public const int MaxStreet = 200;
    public const int MaxPostalCode = 20;
    public const int MaxCity = 100;
    public const int MaxCountry = 100;

    private Address()
    {
    }

    public string Street { get; private init; } = null!;
    public string PostalCode { get; private init; } = null!;
    public string City { get; private init; } = null!;
    public string Country { get; private init; } = null!;

    public static Address Create(string? street, string? postalCode, string? city, string? country)
    {
        var address = new Address
        {
            Street = street?.Trim() ?? string.Empty,
            PostalCode = postalCode?.Trim() ?? string.Empty,
            City = city?.Trim() ?? string.Empty,
            Country = country?.Trim() ?? string.Empty,
        };

        var result = new AddressValidator().Validate(address);
        if (!result.IsValid)
        {
            throw DomainValidationException.From(result, "address");
        }

        return address;
    }
}

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(a => a.Street).NotEmpty().MaximumLength(Address.MaxStreet);
        RuleFor(a => a.PostalCode).NotEmpty().MaximumLength(Address.MaxPostalCode);
        RuleFor(a => a.City).NotEmpty().MaximumLength(Address.MaxCity);
        RuleFor(a => a.Country).NotEmpty().MaximumLength(Address.MaxCountry);
    }
}
