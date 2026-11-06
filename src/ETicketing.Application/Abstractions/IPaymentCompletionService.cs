using ETicketing.Domain.Enums;

namespace ETicketing.Application.Abstractions;

/// <summary>
/// Finalises a payment atomically: payment status, stock and ledger change together or not at all.
/// </summary>
public interface IPaymentCompletionService
{
    /// <summary>
    /// Completes a pending transaction. Safe to call repeatedly: it returns the final status,
    /// which is Failed when the order expired before the payment could complete.
    /// </summary>
    Task<TransactionStatus> CompleteAsync(Guid transactionId, CancellationToken cancellationToken);

    /// <summary>
    /// Fails a pending transaction and releases the reserved stock of its order.
    /// </summary>
    Task FailAsync(Guid transactionId, CancellationToken cancellationToken);
}
