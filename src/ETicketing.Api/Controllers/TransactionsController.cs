using ETicketing.Application.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace ETicketing.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController(TransactionQueryService transactionQueryService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await transactionQueryService.GetByIdAsync(id, cancellationToken);

        return Ok(transaction);
    }
}
