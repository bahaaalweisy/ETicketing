using ETicketing.Application.Abstractions;
using ETicketing.Domain.Enums;
using ETicketing.Domain.Exceptions;

namespace ETicketing.Application.Payments;

/// <summary>
/// Factory over every handler in the DI container. It knows no concrete handler, so a new
/// payment method needs no change here.
/// </summary>
public sealed class PaymentHandlerResolver(IEnumerable<IPaymentHandler> handlers) : IPaymentHandlerResolver
{
    public IPaymentHandler Resolve(PaymentMethod method) =>
        handlers.FirstOrDefault(handler => handler.Method == method)
        ?? throw new UnsupportedPaymentMethodException(method);
}
