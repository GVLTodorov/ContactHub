namespace ContactHub.Application.Contacts.Commands.CreateContact;

public class CreateContactValidator(TimeProvider timeProvider)
    : ContactInputValidator<CreateContactCommand>(timeProvider);
