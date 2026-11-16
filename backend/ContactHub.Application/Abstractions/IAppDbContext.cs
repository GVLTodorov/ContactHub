using ContactHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContactHub.Application.Abstractions;

/// <summary>
/// The application's view of the database. Handlers depend on this instead of the
/// Infrastructure <c>AppDbContext</c>, so the Application layer never references Infrastructure.
/// </summary>
public interface IAppDbContext
{
    /// <summary>
    /// The stored contacts, for querying and for adding or removing entries.
    /// </summary>
    DbSet<Contact> Contacts { get; }

    /// <summary>
    /// Writes all pending changes to the database in one transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>The number of rows written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
