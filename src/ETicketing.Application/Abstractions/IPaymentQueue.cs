namespace ETicketing.Application.Abstractions;

/// <summary>
/// Hands deferred payments from the HTTP request over to background processing.
/// </summary>
public interface IPaymentQueue
{
    /// <summary>
    /// Adds a pending transaction to the queue.
    /// </summary>
    ValueTask EnqueueAsync(Guid transactionId, CancellationToken cancellationToken);

    /// <summary>
    /// Streams queued transactions as they arrive.
    /// </summary>
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
}
