using Api.Application.Abstractions;

namespace Api.Application.Contacts.Queries.GetContacts;

public record GetContactsQuery : IQuery<IReadOnlyList<ContactDto>>;
