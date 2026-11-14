using ContactHub.Api.Contracts;
using ContactHub.Application.Contacts.Commands;

namespace ContactHub.Api.Tests.Api;

[TestFixture]
public class ContactRequestsTests
{
    private static readonly AddressRequest Address = new("Main St 1", "10115", "Berlin", "Germany");

    [Test]
    public void CreateRequest_MapsEveryFieldToCommand()
    {
        var request = new CreateContactRequest("Ada", "Lovelace", new DateOnly(1990, 1, 1), Address, "+4930123456", "DE89370400440532013000");

        var command = request.ToCommand();

        Assert.That(command.FirstName, Is.EqualTo("Ada"));
        Assert.That(command.Surname, Is.EqualTo("Lovelace"));
        Assert.That(command.DateOfBirth, Is.EqualTo(new DateOnly(1990, 1, 1)));
        Assert.That(command.Address, Is.EqualTo(new AddressInput("Main St 1", "10115", "Berlin", "Germany")));
        Assert.That(command.PhoneNumber, Is.EqualTo("+4930123456"));
        Assert.That(command.Iban, Is.EqualTo("DE89370400440532013000"));
    }

    [Test]
    public void UpdateRequest_TakesIdFromRouteAndVersionFromBody()
    {
        var id = Guid.NewGuid();
        var request = new UpdateContactRequest(3, "Ada", "Lovelace", new DateOnly(1990, 1, 1), Address, "+4930123456", "DE89370400440532013000");

        var command = request.ToCommand(id);

        Assert.That(command.Id, Is.EqualTo(id));
        Assert.That(command.Version, Is.EqualTo(3));
        Assert.That(command.Address!.City, Is.EqualTo("Berlin"));
    }

    [Test]
    public void MissingAddress_StaysNullSoValidationReportsIt()
    {
        var create = new CreateContactRequest(null, null, null, null, null, null).ToCommand();
        var update = new UpdateContactRequest(null, null, null, null, null, null, null).ToCommand(Guid.NewGuid());

        Assert.That(create.Address, Is.Null);
        Assert.That(update.Address, Is.Null);
    }
}
