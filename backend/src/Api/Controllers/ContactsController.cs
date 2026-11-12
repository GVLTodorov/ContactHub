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

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ContactDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<ContactDto>> List(
        [FromServices] IQueryHandler<ListContactsQuery, IReadOnlyList<ContactDto>> handler,
        CancellationToken cancellationToken) =>
        await handler.HandleAsync(new ListContactsQuery(), cancellationToken);
}
