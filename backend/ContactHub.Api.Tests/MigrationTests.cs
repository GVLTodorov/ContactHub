using ContactHub.Domain.Entities;
using ContactHub.Domain.Exceptions;
using ContactHub.Domain.ValueObjects;
using ContactHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ContactHub.Api.Tests;

[TestFixture]
public class MigrationTests
{
    private const string SqliteProvider = "Microsoft.EntityFrameworkCore.Sqlite";

    private static bool TableExists(AppDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Contacts'";
        var count = Convert.ToInt32(command.ExecuteScalar());

        return count == 1;
    }

    [Test]
    public void MigrateUp_ThenDownToZero_CreatesAndDropsSchema()
    {
        using var database = new TestDatabase(migrate: false);
        using var context = database.CreateContext();

        context.Database.Migrate();

        Assert.That(context.Database.GetAppliedMigrations().Count(), Is.EqualTo(2));
        Assert.That(TableExists(context), Is.True);

        var migrator = context.GetService<IMigrator>();
        migrator.Migrate("0");

        Assert.That(context.Database.GetAppliedMigrations(), Is.Empty);
        Assert.That(TableExists(context), Is.False);
    }

    [Test]
    public void EveryMigration_HasTargetModelAndOperations()
    {
        using var database = new TestDatabase(migrate: false);
        using var context = database.CreateContext();
        var assembly = context.GetService<IMigrationsAssembly>();

        Assert.That(assembly.Migrations, Has.Count.EqualTo(2));
        foreach (var typeInfo in assembly.Migrations.Values)
        {
            var migration = assembly.CreateMigration(typeInfo, SqliteProvider);

            Assert.That(migration.TargetModel, Is.Not.Null);
            Assert.That(migration.UpOperations, Is.Not.Null);
            Assert.That(migration.DownOperations, Is.Not.Null);
        }
    }

    [Test]
    public void Snapshot_DescribesContactEntity()
    {
        using var database = new TestDatabase(migrate: false);
        using var context = database.CreateContext();
        var assembly = context.GetService<IMigrationsAssembly>();

        var model = assembly.ModelSnapshot!.Model;

        Assert.That(model.FindEntityType(typeof(Contact)), Is.Not.Null);
    }

    [Test]
    public void ModelHasNoPendingChanges()
    {
        using var database = new TestDatabase(migrate: false);
        using var context = database.CreateContext();

        Assert.That(context.Database.HasPendingModelChanges(), Is.False);
    }
}
