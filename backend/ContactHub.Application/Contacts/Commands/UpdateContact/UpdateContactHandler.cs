using ContactHub.Application.Abstractions;
using ContactHub.Application.Exceptions;
using ContactHub.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ContactHub.Application.Contacts.Commands.UpdateContact;

public class UpdateContactHandler(IAppDbContext db, TimeProvider timeProvider)
    : ICommandHandler<UpdateContactCommand, ContactDto>
{
    public async Task<ContactDto> HandleAsync(UpdateContactCommand command, CancellationToken cancellationToken)
    {
        var found = await db.Contacts.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        var contact = found ?? throw new NotFoundException("Contact", command.Id);
        if (contact.Version != command.Version)
        {
            throw ConflictException.ChangedByOthers("Contact", command.Id);
        }

        contact.Update(
            command.FirstName,
            command.Surname,
            command.DateOfBirth!.Value,
            command.Address!.ToAddress(),
            PhoneNumber.Create(command.PhoneNumber),
            Iban.Create(command.Iban),
            timeProvider.GetUtcNow().UtcDateTime);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone saved between our read and our write; Version is the concurrency token.
            throw ConflictException.ChangedByOthers("Contact", command.Id);
        }

        return ContactDto.From(contact);
    }
}
