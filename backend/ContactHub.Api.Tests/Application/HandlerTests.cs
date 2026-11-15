using ContactHub.Api.Tests.Common;
using ContactHub.Application.Abstractions;
using ContactHub.Application.Contacts;
using ContactHub.Application.Contacts.Commands.CreateContact;
using ContactHub.Application.Contacts.Commands.DeleteContact;
using ContactHub.Application.Contacts.Commands.UpdateContact;
using ContactHub.Application.Contacts.Queries.GetContactById;
using ContactHub.Application.Contacts.Queries.GetContacts;
using ContactHub.Application.Exceptions;
using ContactHub.Domain.Entities;
using ContactHub.Domain.Exceptions;
using ContactHub.Domain.ValueObjects;
using ContactHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContactHub.Api.Tests.Application;

[TestFixture]
public class HandlerTests
{
    private TestDatabase _database = null!;
    private readonly TimeProvider _time = new FixedTimeProvider();

    [SetUp]
    public void SetUp() => _database = new TestDatabase();

    [TearDown]
    public void TearDown() => _database.Dispose();

    /// <summary>Simulates another writer saving between our read and our write.</summary>
    private sealed class ConcurrentlyChangedDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new DbUpdateConcurrencyException("Row was changed.");
    }

    private async Task<ContactDto> CreateAsync(CreateContactCommand command)
    {
        await using var db = _database.CreateContext();
        var handler = new CreateContactHandler(db, _time);
        var created = await handler.HandleAsync(command, CancellationToken.None);

        return created;
    }

    [Test]
    public async Task Create_PersistsAndReturnsDto()
    {
        var created = await CreateAsync(Commands.ValidCreate());

        Assert.That(created.FirstName, Is.EqualTo("Ada"));
        Assert.That(created.Iban, Is.EqualTo("DE89370400440532013000"));
        Assert.That(created.IbanFormatted, Is.EqualTo("DE89 3704 0044 0532 0130 00"));
        Assert.That(created.PhoneNumber, Is.EqualTo("+4930123456"));
        Assert.That(created.CreatedAt, Is.EqualTo(_time.GetUtcNow().UtcDateTime));

        await using var db = _database.CreateContext();
        var count = await db.Contacts.CountAsync();

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void Create_InvalidDomainValue_Throws()
    {
        var command = Commands.ValidCreate() with { Iban = "bad" };

        Assert.ThrowsAsync<DomainValidationException>(() => CreateAsync(command));
    }

    [Test]
    public async Task GetById_ReturnsStoredContact()
    {
        var created = await CreateAsync(Commands.ValidCreate());

        await using var db = _database.CreateContext();
        var handler = new GetContactByIdHandler(db);
        var found = await handler.HandleAsync(new GetContactByIdQuery(created.Id), CancellationToken.None);

        Assert.That(found, Is.EqualTo(created));
        Assert.That(found.UpdatedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public async Task GetById_Missing_ThrowsNotFound()
    {
        await using var db = _database.CreateContext();
        var handler = new GetContactByIdHandler(db);

        var ex = Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(new GetContactByIdQuery(Guid.NewGuid()), CancellationToken.None))!;

        Assert.That(ex.Resource, Is.EqualTo("Contact"));
        Assert.That(ex.Message, Does.Contain("was not found"));
    }

    [Test]
    public async Task List_OrdersBySurnameThenFirstNameCaseInsensitive()
    {
        await CreateAsync(Commands.ValidCreate("Zed", "adams"));
        await CreateAsync(Commands.ValidCreate("bob", "Adams"));
        await CreateAsync(Commands.ValidCreate("Alice", "Baker"));

        await using var db = _database.CreateContext();
        var handler = new GetContactsHandler(db);
        var list = await handler.HandleAsync(new GetContactsQuery(), CancellationToken.None);

        Assert.That(list.Select(c => c.FirstName), Is.EqualTo(new[] { "bob", "Zed", "Alice" }));
    }

    [Test]
    public async Task List_Empty_ReturnsEmpty()
    {
        await using var db = _database.CreateContext();
        var handler = new GetContactsHandler(db);
        var list = await handler.HandleAsync(new GetContactsQuery(), CancellationToken.None);

        Assert.That(list, Is.Empty);
    }

    [Test]
    public async Task Update_ChangesFieldsAndUpdatedAt()
    {
        var created = await CreateAsync(Commands.ValidCreate());
        var later = new FixedTimeProvider(FixedTimeProvider.DefaultNow.AddDays(1));

        await using var db = _database.CreateContext();
        var handler = new UpdateContactHandler(db, later);
        var updated = await handler.HandleAsync(Commands.ValidUpdate(created.Id), CancellationToken.None);

        Assert.That(updated.FirstName, Is.EqualTo("Grace"));
        Assert.That(updated.Address.City, Is.EqualTo("Hamburg"));
        Assert.That(updated.Iban, Is.EqualTo("GB82WEST12345698765432"));
        Assert.That(updated.CreatedAt, Is.EqualTo(created.CreatedAt));
        Assert.That(updated.UpdatedAt, Is.EqualTo(later.GetUtcNow().UtcDateTime));
    }

    [Test]
    public async Task Update_BumpsVersion()
    {
        var created = await CreateAsync(Commands.ValidCreate());

        await using var db = _database.CreateContext();
        var handler = new UpdateContactHandler(db, _time);
        var updated = await handler.HandleAsync(Commands.ValidUpdate(created.Id, version: created.Version), CancellationToken.None);

        Assert.That(created.Version, Is.EqualTo(1));
        Assert.That(updated.Version, Is.EqualTo(2));
    }

    [Test]
    public async Task Update_StaleVersion_ThrowsConflictAndKeepsStoredContact()
    {
        var created = await CreateAsync(Commands.ValidCreate());
        await using (var first = _database.CreateContext())
        {
            var firstHandler = new UpdateContactHandler(first, _time);
            await firstHandler.HandleAsync(Commands.ValidUpdate(created.Id), CancellationToken.None);
        }

        await using var db = _database.CreateContext();
        var handler = new UpdateContactHandler(db, _time);

        var ex = Assert.ThrowsAsync<ConflictException>(
            () => handler.HandleAsync(Commands.ValidUpdate(created.Id, "Other", "Writer", version: 1), CancellationToken.None))!;

        Assert.That(ex.Message, Does.Contain("changed by someone else"));

        await using var check = _database.CreateContext();
        var stored = await check.Contacts.SingleAsync();

        Assert.That(stored.FirstName, Is.EqualTo("Grace"));
        Assert.That(stored.Version, Is.EqualTo(2));
    }

    [Test]
    public async Task Update_ConcurrentWriteDuringSave_ThrowsConflict()
    {
        var created = await CreateAsync(Commands.ValidCreate());

        await using var db = new ConcurrentlyChangedDbContext(_database.Options);
        var handler = new UpdateContactHandler(db, _time);

        Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(Commands.ValidUpdate(created.Id), CancellationToken.None));
    }

    [Test]
    public async Task Reads_StoredValuesThatTodaysRulesReject_StillLoad()
    {
        var created = await CreateAsync(Commands.ValidCreate());
        await using (var raw = _database.CreateContext())
        {
            await raw.Database.ExecuteSqlRawAsync("UPDATE Contacts SET Iban = 'XX001234', PhoneNumber = '12'");
        }

        await using var db = _database.CreateContext();
        var one = await new GetContactByIdHandler(db).HandleAsync(new GetContactByIdQuery(created.Id), CancellationToken.None);
        var all = await new GetContactsHandler(db).HandleAsync(new GetContactsQuery(), CancellationToken.None);

        Assert.That(one.Iban, Is.EqualTo("XX001234"));
        Assert.That(one.PhoneNumber, Is.EqualTo("12"));
        Assert.That(all.Single().Iban, Is.EqualTo("XX001234"));
    }

    [Test]
    public async Task Update_Missing_ThrowsNotFound()
    {
        await using var db = _database.CreateContext();
        var handler = new UpdateContactHandler(db, _time);

        Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(Commands.ValidUpdate(Guid.NewGuid()), CancellationToken.None));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Delete_RemovesContact()
    {
        var created = await CreateAsync(Commands.ValidCreate());

        await using (var db = _database.CreateContext())
        {
            var handler = new DeleteContactHandler(db);
            var result = await handler.HandleAsync(new DeleteContactCommand(created.Id), CancellationToken.None);

            Assert.That(result, Is.EqualTo(default(Unit)));
        }

        await using var check = _database.CreateContext();
        var count = await check.Contacts.CountAsync();

        Assert.That(count, Is.Zero);
    }

    [Test]
    public async Task Delete_Missing_ThrowsNotFound()
    {
        await using var db = _database.CreateContext();
        var handler = new DeleteContactHandler(db);

        Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(new DeleteContactCommand(Guid.NewGuid()), CancellationToken.None));
        await Task.CompletedTask;
    }
}
