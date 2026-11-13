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
            .Where(c => c.Id == query.Id)
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
            .FirstOrDefaultAsync(cancellationToken);

        var r = found ?? throw new NotFoundException("Contact", query.Id);

        return new ContactDto(
            r.Id,
            r.FirstName,
            r.Surname,
            r.DateOfBirth,
            new AddressDto(r.Address.Street, r.Address.PostalCode, r.Address.City, r.Address.Country),
            r.PhoneNumber.Value,
            r.Iban.Value,
            r.Iban.ToFormatted(),
            r.CreatedAt,
            r.UpdatedAt);
    }
}
