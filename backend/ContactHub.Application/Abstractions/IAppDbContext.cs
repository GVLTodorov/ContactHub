using ContactHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContactHub.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Contact> Contacts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
