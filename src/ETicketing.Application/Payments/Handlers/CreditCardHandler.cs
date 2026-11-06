using ETicketing.Application.Abstractions;
using ETicketing.Domain.Enums;

namespace ETicketing.Application.Payments.Handlers;

/// <summary>
/// Instant payment: the simulated gateway always approves, so it completes straight away.
/// </summary>
public sealed class CreditCardHandler(
    PendingTransactionCreator transactionCreator,
    IPaymentCompletionService completionService) : IPaymentHandler
{
    public PaymentMethod Method => PaymentMethod.CreditCard;

    public async Task<PaymentResult> ProcessAsync(PaymentContext context, CancellationToken cancellationToken)
    {
        var transaction = await transactionCreator.CreateAsync(context, Method, cancellationToken);
        if (transaction is null)
        {
            return PaymentResult.Failed(PaymentMessages.AlreadyInProgress);
        }

        var status = await completionService.CompleteAsync(transaction.Id, cancellationToken);

        return status == TransactionStatus.Completed
            ? PaymentResult.Completed(transaction)
            : PaymentResult.Failed(PaymentMessages.OrderNoLongerPayable, transaction);
    }
}
