using ETicketing.Application.Payments;
using ETicketing.Domain.Enums;

namespace ETicketing.Application.Abstractions;

public interface IPaymentHandler
{
    PaymentMethod Method { get; }

    Task<PaymentResult> ProcessAsync(PaymentContext context, CancellationToken cancellationToken);
}
