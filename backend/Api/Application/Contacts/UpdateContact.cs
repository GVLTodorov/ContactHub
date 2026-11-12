using Api.Data;
using Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Contacts;

public record UpdateContactCommand(
    Guid Id,
    string? FirstName,
    string? Surname,
    DateOnly? DateOfBirth,
    CreateAddress? Address,
    string? PhoneNumber,
    string? Iban) : CreateContactCommand(FirstName, Surname, DateOfBirth, Address, PhoneNumber, Iban);

public class UpdateContactCommandValidator(TimeProvider timeProvider) : ContactCommandValidator<UpdateContactCommand>(timeProvider);

public class UpdateContactCommandHandler(AppDbContext db, TimeProvider timeProvider)
    : ICommandHandler<UpdateContactCommand, ContactDto>
{
    public async Task<ContactDto> HandleAsync(UpdateContactCommand command, CancellationToken cancellationToken)
    {
        var contact = await db.Contacts.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException("Contact", command.Id);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var address = command.Address!;
        contact.Rename(command.FirstName, command.Surname, now);
        contact.ChangeDateOfBirth(command.DateOfBirth!.Value, now);
        contact.ChangeAddress(Address.Create(address.Street, address.PostalCode, address.City, address.Country), now);
        contact.ChangePhoneNumber(PhoneNumber.Create(command.PhoneNumber), now);
        contact.ChangeIban(Iban.Create(command.Iban), now);

        await db.SaveChangesAsync(cancellationToken);
        return ContactDto.From(contact);
    }
}
