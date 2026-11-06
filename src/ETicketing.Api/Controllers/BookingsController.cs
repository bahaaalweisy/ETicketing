using ETicketing.Application.Bookings;
using ETicketing.Application.Orders;
using Microsoft.AspNetCore.Mvc;

namespace ETicketing.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(BookingService bookingService) : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Create(
        [FromBody] CreateBookingRequest request,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new CreateBookingCommand(request.TicketId, request.Quantity, idempotencyKey ?? string.Empty);
        var order = await bookingService.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(OrdersController.GetById), "Orders", new { id = order.Id }, order);
    }
}
