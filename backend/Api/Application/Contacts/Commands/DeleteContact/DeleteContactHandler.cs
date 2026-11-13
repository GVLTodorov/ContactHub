using Api.Application.Abstractions;
using Api.Application.Exceptions;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Contacts.Commands.DeleteContact;

public class DeleteContactHandler(AppDbContext db) : ICommandHandler<DeleteContactCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteContactCommand command, CancellationToken cancellationToken)
    {
        var contact = await db.Contacts.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException("Contact", command.Id);

        db.Contacts.Remove(contact);
        await db.SaveChangesAsync(cancellationToken);
        return default;
    }
}
