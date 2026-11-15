using ContactHub.Application.Abstractions;

namespace ContactHub.Application.Contacts.Commands.DeleteContact;

public record DeleteContactCommand(Guid Id) : ICommand<Unit>;
