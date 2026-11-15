using ContactHub.Application.Abstractions;

namespace ContactHub.Application.Contacts.Queries.GetContacts;

public record GetContactsQuery : IQuery<IReadOnlyList<ContactDto>>;
