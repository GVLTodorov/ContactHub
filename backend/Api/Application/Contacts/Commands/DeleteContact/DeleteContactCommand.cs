using Api.Application.Abstractions;

namespace Api.Application.Contacts.Commands.DeleteContact;

public record DeleteContactCommand(Guid Id) : ICommand<Unit>;
