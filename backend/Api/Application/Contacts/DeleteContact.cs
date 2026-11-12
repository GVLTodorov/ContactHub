using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Contacts;

public readonly record struct Unit;

public record DeleteContactCommand(Guid Id) : ICommand<Unit>;

public class DeleteContactCommandHandler(AppDbContext db) : ICommandHandler<DeleteContactCommand, Unit>
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
