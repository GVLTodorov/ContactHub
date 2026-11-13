using ContactHub.Domain.Entities;
using ContactHub.Domain.ValueObjects;
using FluentValidation;

namespace ContactHub.Application.Contacts.Commands.CreateContact;

public class CreateContactValidator(TimeProvider timeProvider)
    : ContactCommandValidator<CreateContactCommand>(timeProvider);

/// <summary>Request rules shared by create and update.</summary>
public abstract class ContactCommandValidator<T> : AbstractValidator<T> where T : CreateContactCommand
{
    protected ContactCommandValidator(TimeProvider timeProvider)
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
