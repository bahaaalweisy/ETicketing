using ETicketing.Domain.Enums;

namespace ETicketing.Application.Abstractions;

public interface IPaymentCompletionService
{
    Task<TransactionStatus> CompleteAsync(Guid transactionId, CancellationToken cancellationToken);

    Task FailAsync(Guid transactionId, CancellationToken cancellationToken);
}
