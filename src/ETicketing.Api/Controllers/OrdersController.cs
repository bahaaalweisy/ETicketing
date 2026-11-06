using ETicketing.Application.Orders;
using Microsoft.AspNetCore.Mvc;

namespace ETicketing.Api.Controllers;

/// <summary>
/// Reads a single order.
/// </summary>
[ApiController]
[Route("api/orders")]
public sealed class OrdersController(OrderQueryService orderQueryService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await orderQueryService.GetByIdAsync(id, cancellationToken);

        return Ok(order);
    }
}
