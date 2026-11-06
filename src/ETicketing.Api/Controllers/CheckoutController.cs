using ETicketing.Application.Payments;
using ETicketing.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ETicketing.Api.Controllers;

/// <summary>
/// Starts a payment: 200 when it completed instantly, 202 when it continues in the background.
/// </summary>
[ApiController]
[Route("api/checkout")]
public sealed class CheckoutController(CheckoutService checkoutService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CheckoutResponse>> Checkout(
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var response = await checkoutService.CheckoutAsync(request, cancellationToken);

        return response.Status == TransactionStatus.Pending
            ? AcceptedAtAction(
                nameof(TransactionsController.GetById),
                "Transactions",
                new { id = response.TransactionId },
                response)
            : Ok(response);
    }
}
