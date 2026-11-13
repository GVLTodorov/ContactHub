using ContactHub.Application.Contacts.Commands.CreateContact;

namespace ContactHub.Application.Contacts.Commands.UpdateContact;

public class UpdateContactValidator(TimeProvider timeProvider) : ContactCommandValidator<UpdateContactCommand>(timeProvider);
