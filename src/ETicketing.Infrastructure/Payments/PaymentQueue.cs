using System.Threading.Channels;
using ETicketing.Application.Abstractions;

namespace ETicketing.Infrastructure.Payments;

public sealed class PaymentQueue : IPaymentQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(Guid transactionId, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(transactionId, cancellationToken);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
