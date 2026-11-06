using ETicketing.Application.Ledger;

namespace ETicketing.Application.Abstractions;

/// <summary>
/// Double-entry bookkeeping.
/// </summary>
public interface ILedgerService
{
    /// <summary>
    /// Inserts exactly two entries for a sale: debit CASH_GATEWAY and credit TICKET_SALES_REVENUE.
    /// </summary>
    Task RecordTicketSaleAsync(Guid transactionId, decimal amount, CancellationToken cancellationToken);

    /// <summary>
    /// Verifies that the debits and credits of a transaction are equal.
    /// </summary>
    /// <exception cref="ETicketing.Domain.Exceptions.UnbalancedLedgerException">They differ.</exception>
    Task EnsureBalancedAsync(Guid transactionId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the ledger totals and the balance of each account.
    /// </summary>
    Task<LedgerBalanceResponse> GetBalanceAsync(CancellationToken cancellationToken);
}
