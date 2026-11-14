using ContactHub.Application.Contacts;
using ContactHub.Application.Contacts.Commands;
using ContactHub.Application.Contacts.Commands.CreateContact;
using ContactHub.Application.Contacts.Commands.DeleteContact;
using ContactHub.Application.Contacts.Commands.UpdateContact;
using ContactHub.Application.Contacts.Queries.GetContactById;
using ContactHub.Application.Contacts.Queries.GetContacts;
using ContactHub.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ContactHub.Api.Tests.Common;

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    public FixedTimeProvider()
        : this(DefaultNow)
    {
    }

    public override DateTimeOffset GetUtcNow() => now;
}

/// <summary>In-memory SQLite database kept alive by an open connection.</summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<AppDbContext> _options;

    public TestDatabase(bool migrate = true)
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        if (migrate)
        {
            using var context = CreateContext();
            context.Database.Migrate();
        }
    }

    public DbContextOptions<AppDbContext> Options => _options;

    public AppDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}

public static class Commands
{
    public static AddressInput ValidAddress() => new("Main St 1", "10115", "Berlin", "Germany");

    public static CreateContactCommand ValidCreate(string firstName = "Ada", string surname = "Lovelace") =>
        new(firstName, surname, new DateOnly(1990, 1, 1), ValidAddress(), "+49 30 123456", "DE89 3704 0044 0532 0130 00");

    public static UpdateContactCommand ValidUpdate(Guid id, string firstName = "Grace", string surname = "Hopper", int? version = 1) =>
        new(id, version, firstName, surname, new DateOnly(1985, 5, 5), new AddressInput("Other St 2", "20095", "Hamburg", "Germany"), "+49 40 999999", "GB82 WEST 1234 5698 7654 32");
}
