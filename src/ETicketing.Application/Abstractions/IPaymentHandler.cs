using ETicketing.Application.Payments;
using ETicketing.Domain.Enums;

namespace ETicketing.Application.Abstractions;

/// <summary>
/// Strategy for one payment method. Every handler honours the same contract: it returns a
/// <see cref="PaymentResult"/>, reports expected business failures as Failed instead of
/// throwing, and respects the cancellation token.
/// </summary>
public interface IPaymentHandler
{
    /// <summary>
    /// The payment method this handler serves.
    /// </summary>
    PaymentMethod Method { get; }

    /// <summary>
    /// Runs the payment for an order that has already passed the checkout rules.
    /// </summary>
    Task<PaymentResult> ProcessAsync(PaymentContext context, CancellationToken cancellationToken);
}
