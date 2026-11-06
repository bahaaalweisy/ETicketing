using ETicketing.Application.Abstractions;
using ETicketing.Application.Ledger;
using Microsoft.AspNetCore.Mvc;

namespace ETicketing.Api.Controllers;

/// <summary>
/// Exposes the ledger totals so anyone can verify that debits equal credits.
/// </summary>
[ApiController]
[Route("api/ledger")]
public sealed class LedgerController(ILedgerService ledgerService) : ControllerBase
{
    [HttpGet("balance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<LedgerBalanceResponse>> GetBalance(CancellationToken cancellationToken)
    {
        var balance = await ledgerService.GetBalanceAsync(cancellationToken);

        return Ok(balance);
    }
}
