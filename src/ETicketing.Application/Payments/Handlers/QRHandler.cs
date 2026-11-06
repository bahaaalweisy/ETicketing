using ETicketing.Application.Abstractions;
using ETicketing.Domain.Enums;

namespace ETicketing.Application.Payments.Handlers;

/// <summary>
/// Deferred payment: it only records and queues the transaction, so the HTTP request never
/// waits for the customer to scan. A background service finishes the payment.
/// </summary>
public sealed class QRHandler(PendingTransactionCreator transactionCreator, IPaymentQueue paymentQueue) : IPaymentHandler
{
    public PaymentMethod Method => PaymentMethod.QR;

    public async Task<PaymentResult> ProcessAsync(PaymentContext context, CancellationToken cancellationToken)
    {
        var transaction = await transactionCreator.CreateAsync(context, Method, cancellationToken);
        if (transaction is null)
        {
            return PaymentResult.Failed(PaymentMessages.AlreadyInProgress);
        }

        await paymentQueue.EnqueueAsync(transaction.Id, cancellationToken);

        return PaymentResult.Pending(transaction);
    }
}
