using ContactHub.Application.Abstractions;
using ContactHub.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ContactHub.Application.Contacts.Queries.GetContactById;

public class GetContactByIdHandler(IAppDbContext db) : IQueryHandler<GetContactByIdQuery, ContactDto>
{
    public async Task<ContactDto> HandleAsync(GetContactByIdQuery query, CancellationToken cancellationToken)
    {
        var found = await db.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken);

        var contact = found ?? throw new NotFoundException("Contact", query.Id);

        return ContactDto.From(contact);
    }
}
