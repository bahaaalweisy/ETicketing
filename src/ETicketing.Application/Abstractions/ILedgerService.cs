using ETicketing.Application.Ledger;

namespace ETicketing.Application.Abstractions;

public interface ILedgerService
{
    Task RecordTicketSaleAsync(Guid transactionId, decimal amount, CancellationToken cancellationToken);

    Task EnsureBalancedAsync(Guid transactionId, CancellationToken cancellationToken);

    Task<LedgerBalanceResponse> GetBalanceAsync(CancellationToken cancellationToken);
}
