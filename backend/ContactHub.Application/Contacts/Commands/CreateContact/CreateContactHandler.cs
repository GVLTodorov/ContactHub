using ContactHub.Application.Abstractions;
using ContactHub.Domain.Entities;
using ContactHub.Domain.ValueObjects;

namespace ContactHub.Application.Contacts.Commands.CreateContact;

public class CreateContactHandler(IAppDbContext db, TimeProvider timeProvider)
    : ICommandHandler<CreateContactCommand, ContactDto>
{
    public async Task<ContactDto> HandleAsync(CreateContactCommand command, CancellationToken cancellationToken)
    {
        var contact = Contact.Create(
            command.FirstName,
            command.Surname,
            command.DateOfBirth!.Value,
            command.Address!.ToAddress(),
            PhoneNumber.Create(command.PhoneNumber),
            Iban.Create(command.Iban),
            timeProvider.GetUtcNow().UtcDateTime);

        db.Contacts.Add(contact);
        await db.SaveChangesAsync(cancellationToken);

        return ContactDto.From(contact);
    }
}
