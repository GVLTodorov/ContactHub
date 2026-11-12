using Api.Data;
using Api.Domain;
using FluentValidation;

namespace Api.Application.Contacts;

public record CreateContactCommand(
    string? FirstName,
    string? Surname,
    DateOnly? DateOfBirth,
    CreateAddress? Address,
    string? PhoneNumber,
    string? Iban) : ICommand<ContactDto>;

public record CreateAddress(string? Street, string? PostalCode, string? City, string? Country);

public class CreateAddressValidator : AbstractValidator<CreateAddress>
{
    public CreateAddressValidator()
    {
        RuleFor(a => a.Street).NotEmpty().MaximumLength(Address.MaxStreet);
        RuleFor(a => a.PostalCode).NotEmpty().MaximumLength(Address.MaxPostalCode);
        RuleFor(a => a.City).NotEmpty().MaximumLength(Address.MaxCity);
        RuleFor(a => a.Country).NotEmpty().MaximumLength(Address.MaxCountry);
    }
}

public class CreateContactCommandValidator : AbstractValidator<CreateContactCommand>
{
    public CreateContactCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(Contact.MaxNameLength);
        RuleFor(c => c.Surname).NotEmpty().MaximumLength(Contact.MaxNameLength);
        RuleFor(c => c.DateOfBirth)
            .NotNull()
            .Must(d => d is null || d <= Today(timeProvider))
            .WithMessage("Date of birth cannot be in the future.")
            .Must(d => d is null || d >= Today(timeProvider).AddYears(-Contact.MaxAgeYears))
            .WithMessage($"Date of birth cannot be more than {Contact.MaxAgeYears} years ago.");
        RuleFor(c => c.Address).NotNull();
        RuleFor(c => c.Address!).SetValidator(new CreateAddressValidator()).When(c => c.Address is not null);
        RuleFor(c => c.PhoneNumber).NotEmpty().Must(PhoneNumber.IsValid).WithMessage("Phone number is not valid.");
        RuleFor(c => c.Iban).NotEmpty().Must(Iban.IsValid).WithMessage("IBAN is not valid.");
    }

    private static DateOnly Today(TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}

public class CreateContactCommandHandler(AppDbContext db, TimeProvider timeProvider)
    : ICommandHandler<CreateContactCommand, ContactDto>
{
    public async Task<ContactDto> HandleAsync(CreateContactCommand command, CancellationToken cancellationToken)
    {
        var contact = Contact.Create(
            command.FirstName,
            command.Surname,
            command.DateOfBirth!.Value,
            Address.Create(command.Address!.Street, command.Address.PostalCode, command.Address.City, command.Address.Country),
            PhoneNumber.Create(command.PhoneNumber),
            Iban.Create(command.Iban),
            timeProvider.GetUtcNow().UtcDateTime);

        db.Contacts.Add(contact);
        await db.SaveChangesAsync(cancellationToken);
        return ContactDto.From(contact);
    }
}
