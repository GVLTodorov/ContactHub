using ContactHub.Application.Abstractions;
using ContactHub.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ContactHub.Application.Contacts.Commands.DeleteContact;

public class DeleteContactHandler(IAppDbContext db) : ICommandHandler<DeleteContactCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteContactCommand command, CancellationToken cancellationToken)
    {
        var found = await db.Contacts.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        var contact = found ?? throw new NotFoundException("Contact", command.Id);

        db.Contacts.Remove(contact);
        await db.SaveChangesAsync(cancellationToken);

        return default;
    }
}
