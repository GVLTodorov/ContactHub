using Api.Application;
using Api.Application.Contacts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ContactDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        CreateContactCommand command,
        [FromServices] ICommandHandler<CreateContactCommand, ContactDto> handler,
        CancellationToken cancellationToken)
    {
        var contact = await handler.HandleAsync(command, cancellationToken);

        return Created($"/api/contacts/{contact.Id}", contact);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ContactDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ContactDto> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetContactByIdQuery, ContactDto> handler,
        CancellationToken cancellationToken)
    {
        var contact = await handler.HandleAsync(new GetContactByIdQuery(id), cancellationToken);

        return contact;
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ContactDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ContactDto> Update(
        Guid id,
        CreateContactCommand body,
        [FromServices] ICommandHandler<UpdateContactCommand, ContactDto> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateContactCommand(id, body.FirstName, body.Surname, body.DateOfBirth, body.Address, body.PhoneNumber, body.Iban);
        var contact = await handler.HandleAsync(command, cancellationToken);

        return contact;
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICommandHandler<DeleteContactCommand, Unit> handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteContactCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ContactDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<ContactDto>> List(
        [FromServices] IQueryHandler<ListContactsQuery, IReadOnlyList<ContactDto>> handler,
        CancellationToken cancellationToken)
    {
        var contacts = await handler.HandleAsync(new ListContactsQuery(), cancellationToken);

        return contacts;
    }
}
