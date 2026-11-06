using ETicketing.Application.Tickets;
using Microsoft.AspNetCore.Mvc;

namespace ETicketing.Api.Controllers;

/// <summary>
/// Lists the ticket types and their stock.
/// </summary>
[ApiController]
[Route("api/tickets")]
public sealed class TicketsController(TicketQueryService ticketQueryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TicketResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var tickets = await ticketQueryService.GetAllAsync(cancellationToken);

        return Ok(tickets);
    }
}
