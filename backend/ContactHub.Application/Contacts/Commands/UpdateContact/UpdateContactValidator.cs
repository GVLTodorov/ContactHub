using FluentValidation;

namespace ContactHub.Application.Contacts.Commands.UpdateContact;

public class UpdateContactValidator : ContactInputValidator<UpdateContactCommand>
{
    public UpdateContactValidator(TimeProvider timeProvider)
        : base(timeProvider)
    {
        RuleFor(c => c.Version).NotNull().GreaterThan(0);
    }
}
