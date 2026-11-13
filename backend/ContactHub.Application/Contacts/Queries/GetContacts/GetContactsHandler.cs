using ContactHub.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ContactHub.Application.Contacts.Queries.GetContacts;

public class GetContactsHandler(IAppDbContext db) : IQueryHandler<GetContactsQuery, IReadOnlyList<ContactDto>>
{
    public async Task<IReadOnlyList<ContactDto>> HandleAsync(GetContactsQuery query, CancellationToken cancellationToken)
    {
        var rows = await db.Contacts
            .AsNoTracking()
            .OrderBy(c => c.Surname.ToLower())
            .ThenBy(c => c.FirstName.ToLower())
            .ThenBy(c => c.Id)
            .Select(c => new
            {
                c.Id,
                c.FirstName,
                c.Surname,
                c.DateOfBirth,
                c.Address,
                c.PhoneNumber,
                c.Iban,
                c.CreatedAt,
                c.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new ContactDto(
                r.Id,
                r.FirstName,
                r.Surname,
                r.DateOfBirth,
                new AddressDto(r.Address.Street, r.Address.PostalCode, r.Address.City, r.Address.Country),
                r.PhoneNumber.Value,
                r.Iban.Value,
                r.Iban.ToFormatted(),
                r.CreatedAt,
                r.UpdatedAt))
            .ToList();
    }
}
