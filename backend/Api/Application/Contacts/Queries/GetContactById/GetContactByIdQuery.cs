using Api.Application.Abstractions;

namespace Api.Application.Contacts.Queries.GetContactById;

public record GetContactByIdQuery(Guid Id) : IQuery<ContactDto>;
