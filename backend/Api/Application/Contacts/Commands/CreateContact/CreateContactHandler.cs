using Api.Application.Abstractions;
using Api.Domain.Entities;
using Api.Domain.ValueObjects;
using Api.Infrastructure.Persistence;

namespace Api.Application.Contacts.Commands.CreateContact;

public class CreateContactHandler(AppDbContext db, TimeProvider timeProvider)
    : ICommandHandler<CreateContactCommand, ContactDto>
{
    public async Task<ContactDto> HandleAsync(CreateContactCommand command, CancellationToken cancellationToken)
    {
        var contact = Contact.Create(
            command.FirstName,
            command.Surname,
            command.DateOfBirth!.Value,
            Address.Create(command.Address!.Street, command.Address.PostalCode, command.Address.City, command.Address.Country),
            PhoneNumber.Create(command.PhoneNumber),
            Iban.Create(command.Iban),
            timeProvider.GetUtcNow().UtcDateTime);

        db.Contacts.Add(contact);
        await db.SaveChangesAsync(cancellationToken);
        return ContactDto.From(contact);
    }
}
