using ContactHub.Domain.Exceptions;
using ContactHub.Domain.ValueObjects;
using FluentValidation;

namespace ContactHub.Domain.Entities;

public class Contact
{
    public const int MaxNameLength = 100;
    public const int MaxAgeYears = 150;

    private Contact()
    {
    }

    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string Surname { get; private set; } = null!;
    public DateOnly DateOfBirth { get; private set; }
    public Address Address { get; private set; } = null!;
    public PhoneNumber PhoneNumber { get; private set; } = null!;
    public Iban Iban { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Contact Create(
        string? firstName,
        string? surname,
        DateOnly dateOfBirth,
        Address address,
        PhoneNumber phoneNumber,
        Iban iban,
        DateTime utcNow)
    {
        var contact = new Contact
        {
            Id = Guid.CreateVersion7(new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc))),
            FirstName = firstName?.Trim() ?? string.Empty,
            Surname = surname?.Trim() ?? string.Empty,
            DateOfBirth = dateOfBirth,
            Address = address,
            PhoneNumber = phoneNumber,
            Iban = iban,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        contact.EnsureValid(utcNow);
        return contact;
    }

    public void Rename(string? firstName, string? surname, DateTime utcNow) =>
        Change(utcNow, () =>
        {
            FirstName = firstName?.Trim() ?? string.Empty;
            Surname = surname?.Trim() ?? string.Empty;
        });

    public void ChangeDateOfBirth(DateOnly dateOfBirth, DateTime utcNow) =>
        Change(utcNow, () => DateOfBirth = dateOfBirth);

    public void ChangeAddress(Address address, DateTime utcNow) =>
        Change(utcNow, () => Address = address);

    public void ChangePhoneNumber(PhoneNumber phoneNumber, DateTime utcNow) =>
        Change(utcNow, () => PhoneNumber = phoneNumber);

    public void ChangeIban(Iban iban, DateTime utcNow) =>
        Change(utcNow, () => Iban = iban);

    private void Change(DateTime utcNow, Action apply)
    {
        var before = (FirstName, Surname, DateOfBirth, Address, PhoneNumber, Iban);
        apply();
        var after = (FirstName, Surname, DateOfBirth, Address, PhoneNumber, Iban);
        if (before.Equals(after))
        {
            return;
        }

        try
        {
            EnsureValid(utcNow);
        }
        catch
        {
            (FirstName, Surname, DateOfBirth, Address, PhoneNumber, Iban) = before;
            throw;
        }

        UpdatedAt = utcNow;
    }

    private void EnsureValid(DateTime utcNow)
    {
        var result = new ContactValidator(DateOnly.FromDateTime(utcNow)).Validate(this);
        if (!result.IsValid)
        {
            throw DomainValidationException.From(result);
        }
    }
}

public class ContactValidator : AbstractValidator<Contact>
{
    public ContactValidator(DateOnly today)
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(Contact.MaxNameLength);
        RuleFor(c => c.Surname).NotEmpty().MaximumLength(Contact.MaxNameLength);
        RuleFor(c => c.DateOfBirth)
            .LessThanOrEqualTo(today).WithMessage("Date of birth cannot be in the future.")
            .GreaterThanOrEqualTo(today.AddYears(-Contact.MaxAgeYears))
            .WithMessage($"Date of birth cannot be more than {Contact.MaxAgeYears} years ago.");
        RuleFor(c => c.Address).NotNull();
        RuleFor(c => c.PhoneNumber).NotNull();
        RuleFor(c => c.Iban).NotNull();
    }
}
