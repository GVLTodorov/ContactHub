using ContactHub.Application.Abstractions;

namespace ContactHub.Application.Contacts.Queries.GetContactById;

public record GetContactByIdQuery(Guid Id) : IQuery<ContactDto>;
