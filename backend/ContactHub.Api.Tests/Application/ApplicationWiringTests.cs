using ContactHub.Api.Tests.Common;
using ContactHub.Application;
using ContactHub.Application.Abstractions;
using ContactHub.Application.Behaviors;
using ContactHub.Application.Contacts;
using ContactHub.Application.Contacts.Commands.CreateContact;
using ContactHub.Application.Contacts.Commands.DeleteContact;
using ContactHub.Application.Contacts.Commands.UpdateContact;
using ContactHub.Application.Contacts.Queries.GetContactById;
using ContactHub.Application.Contacts.Queries.GetContacts;
using ContactHub.Application.Exceptions;
using ContactHub.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ContactHub.Api.Tests.Application;

[TestFixture]
public class ApplicationWiringTests
{
    private TestDatabase _database = null!;
    private ServiceProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _database = new TestDatabase();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider());
        services.AddScoped(_ => _database.CreateContext());
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        var returned = services.AddApplication();
        Assert.That(returned, Is.SameAs(services));
        _provider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        _provider.Dispose();
        _database.Dispose();
    }

    [Test]
    public void CommandHandlers_AreResolvedThroughValidationDecorator()
    {
        using var scope = _provider.CreateScope();

        var create = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateContactCommand, ContactDto>>();
        var update = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateContactCommand, ContactDto>>();
        var delete = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeleteContactCommand, Unit>>();

        Assert.That(create, Is.TypeOf<ValidationCommandHandlerDecorator<CreateContactCommand, ContactDto>>());
        Assert.That(update, Is.TypeOf<ValidationCommandHandlerDecorator<UpdateContactCommand, ContactDto>>());
        Assert.That(delete, Is.TypeOf<ValidationCommandHandlerDecorator<DeleteContactCommand, Unit>>());
    }

    [Test]
    public void QueryHandlers_AreRegisteredWithoutDecorator()
    {
        using var scope = _provider.CreateScope();

        var list = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetContactsQuery, IReadOnlyList<ContactDto>>>();
        var get = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetContactByIdQuery, ContactDto>>();

        Assert.That(list, Is.TypeOf<GetContactsHandler>());
        Assert.That(get, Is.TypeOf<GetContactByIdHandler>());
    }

    [Test]
    public void Validators_AreRegisteredOnlyFromApplicationNamespace()
    {
        using var scope = _provider.CreateScope();

        var validators = scope.ServiceProvider.GetServices<IValidator<CreateContactCommand>>().ToList();

        Assert.That(validators, Has.Count.EqualTo(1));
        Assert.That(validators[0], Is.TypeOf<CreateContactValidator>());
    }

    [Test]
    public async Task Decorator_InvalidCommand_ThrowsValidationExceptionAndSkipsHandler()
    {
        using var scope = _provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateContactCommand, ContactDto>>();

        var ex = Assert.ThrowsAsync<ValidationException>(
            () => handler.HandleAsync(new CreateContactCommand(null, null, null, null, null, null), CancellationToken.None))!;

        Assert.That(ex.Errors, Is.Not.Empty);

        await using var db = _database.CreateContext();
        var count = await db.Contacts.CountAsync();

        Assert.That(count, Is.Zero);
    }

    [Test]
    public async Task Decorator_ValidCommand_RunsInnerHandler()
    {
        using var scope = _provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateContactCommand, ContactDto>>();

        var created = await handler.HandleAsync(Commands.ValidCreate(), CancellationToken.None);

        Assert.That(created.FirstName, Is.EqualTo("Ada"));
    }

    [Test]
    public async Task Decorator_WithoutValidators_RunsInnerHandler()
    {
        using var scope = _provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeleteContactCommand, Unit>>();

        Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(new DeleteContactCommand(Guid.NewGuid()), CancellationToken.None));
        await Task.CompletedTask;
    }

    [Test]
    public void DbContextRegistration_Resolves()
    {
        using var scope = _provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.That(db.Database.IsSqlite(), Is.True);
    }
}
