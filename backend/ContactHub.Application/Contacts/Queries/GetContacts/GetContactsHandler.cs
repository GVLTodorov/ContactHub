using ContactHub.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ContactHub.Application.Contacts.Queries.GetContacts;

public class GetContactsHandler(IAppDbContext db) : IQueryHandler<GetContactsQuery, IReadOnlyList<ContactDto>>
{
    public async Task<IReadOnlyList<ContactDto>> HandleAsync(GetContactsQuery query, CancellationToken cancellationToken)
    {
        var contacts = await db.Contacts
            .AsNoTracking()
            .OrderBy(c => c.Surname.ToLower())
            .ThenBy(c => c.FirstName.ToLower())
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

        return contacts.Select(ContactDto.From).ToList();
    }
}
