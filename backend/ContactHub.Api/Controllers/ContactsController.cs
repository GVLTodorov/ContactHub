using ContactHub.Api.Contracts;
using ContactHub.Application.Abstractions;
using ContactHub.Application.Contacts;
using ContactHub.Application.Contacts.Commands.CreateContact;
using ContactHub.Application.Contacts.Commands.DeleteContact;
using ContactHub.Application.Contacts.Commands.UpdateContact;
using ContactHub.Application.Contacts.Queries.GetContactById;
using ContactHub.Application.Contacts.Queries.GetContacts;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace ContactHub.Api.Controllers;

[ApiController]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ContactDto>(Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(Status400BadRequest)]
    public async Task<IActionResult> Create(
        CreateContactRequest body,
        [FromServices]
        ICommandHandler<CreateContactCommand, ContactDto> handler,
        CancellationToken cancellationToken)
    {
        var command = body.ToCommand();
        var contact = await handler.HandleAsync(command, cancellationToken);

        return Created($"/api/contacts/{contact.Id}", contact);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ContactDto>(Status200OK)]
    [ProducesResponseType<ProblemDetails>(Status404NotFound)]
    public async Task<ContactDto> GetById(
        Guid id,
        [FromServices]
        IQueryHandler<GetContactByIdQuery, ContactDto> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetContactByIdQuery(id);
        var contact = await handler.HandleAsync(query, cancellationToken);

        return contact;
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ContactDto>(Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(Status409Conflict)]
    public async Task<ContactDto> Update(
        Guid id,
        UpdateContactRequest body,
        [FromServices]
        ICommandHandler<UpdateContactCommand, ContactDto> handler,
        CancellationToken cancellationToken)
    {
        var command = body.ToCommand(id);
        var contact = await handler.HandleAsync(command, cancellationToken);

        return contact;
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices]
        ICommandHandler<DeleteContactCommand, Unit> handler,
        CancellationToken cancellationToken)
    {
        var command = new DeleteContactCommand(id);
        await handler.HandleAsync(command, cancellationToken);

        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ContactDto>>(Status200OK)]
    public async Task<IReadOnlyList<ContactDto>> List(
        [FromServices]
        IQueryHandler<GetContactsQuery, IReadOnlyList<ContactDto>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetContactsQuery();
        var contacts = await handler.HandleAsync(query, cancellationToken);

        return contacts;
    }
}
