namespace ETicketing.Application.Abstractions;

public interface IPaymentQueue
{
    ValueTask EnqueueAsync(Guid transactionId, CancellationToken cancellationToken);

    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
}
