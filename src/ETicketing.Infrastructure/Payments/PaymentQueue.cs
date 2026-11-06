using System.Threading.Channels;
using ETicketing.Application.Abstractions;

namespace ETicketing.Infrastructure.Payments;

/// <summary>
/// In-process queue on System.Threading.Channels. It holds only transaction ids: the database
/// stays the source of truth, so losing the queue in a crash loses no payment.
/// </summary>
public sealed class PaymentQueue : IPaymentQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(Guid transactionId, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(transactionId, cancellationToken);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
