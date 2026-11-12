using Api.Application;
using Api.Application.Contacts;
using Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests;

[TestFixture]
public class HandlerTests
{
    private TestDatabase _database = null!;
    private readonly TimeProvider _time = new FixedTimeProvider();

    [SetUp]
    public void SetUp() => _database = new TestDatabase();

    [TearDown]
    public void TearDown() => _database.Dispose();

    private async Task<ContactDto> CreateAsync(CreateContactCommand command)
    {
        await using var db = _database.CreateContext();
        var handler = new CreateContactCommandHandler(db, _time);
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
        var handler = new GetContactByIdQueryHandler(db);
        var found = await handler.HandleAsync(new GetContactByIdQuery(created.Id), CancellationToken.None);

        Assert.That(found, Is.EqualTo(created));
        Assert.That(found.UpdatedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public async Task GetById_Missing_ThrowsNotFound()
    {
        await using var db = _database.CreateContext();
        var handler = new GetContactByIdQueryHandler(db);

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
        var handler = new ListContactsQueryHandler(db);
        var list = await handler.HandleAsync(new ListContactsQuery(), CancellationToken.None);

        Assert.That(list.Select(c => c.FirstName), Is.EqualTo(new[] { "bob", "Zed", "Alice" }));
    }

    [Test]
    public async Task List_Empty_ReturnsEmpty()
    {
        await using var db = _database.CreateContext();
        var handler = new ListContactsQueryHandler(db);
        var list = await handler.HandleAsync(new ListContactsQuery(), CancellationToken.None);

        Assert.That(list, Is.Empty);
    }

    [Test]
    public async Task Update_ChangesFieldsAndUpdatedAt()
    {
        var created = await CreateAsync(Commands.ValidCreate());
        var later = new FixedTimeProvider(FixedTimeProvider.DefaultNow.AddDays(1));

        await using var db = _database.CreateContext();
        var handler = new UpdateContactCommandHandler(db, later);
        var updated = await handler.HandleAsync(Commands.ValidUpdate(created.Id), CancellationToken.None);

        Assert.That(updated.FirstName, Is.EqualTo("Grace"));
        Assert.That(updated.Address.City, Is.EqualTo("Hamburg"));
        Assert.That(updated.Iban, Is.EqualTo("GB82WEST12345698765432"));
        Assert.That(updated.CreatedAt, Is.EqualTo(created.CreatedAt));
        Assert.That(updated.UpdatedAt, Is.EqualTo(later.GetUtcNow().UtcDateTime));
    }

    [Test]
    public async Task Update_Missing_ThrowsNotFound()
    {
        await using var db = _database.CreateContext();
        var handler = new UpdateContactCommandHandler(db, _time);

        Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(Commands.ValidUpdate(Guid.NewGuid()), CancellationToken.None));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Delete_RemovesContact()
    {
        var created = await CreateAsync(Commands.ValidCreate());

        await using (var db = _database.CreateContext())
        {
            var handler = new DeleteContactCommandHandler(db);
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
        var handler = new DeleteContactCommandHandler(db);

        Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(new DeleteContactCommand(Guid.NewGuid()), CancellationToken.None));
        await Task.CompletedTask;
    }
}
