using Api.Application.Contacts.Commands.CreateContact;

namespace Api.Application.Contacts.Commands.UpdateContact;

public class UpdateContactValidator(TimeProvider timeProvider) : ContactCommandValidator<UpdateContactCommand>(timeProvider);
