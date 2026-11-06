using ETicketing.Application.Abstractions;
using ETicketing.Domain.Enums;
using ETicketing.Domain.Exceptions;

namespace ETicketing.Application.Payments;

public sealed class PaymentHandlerResolver(IEnumerable<IPaymentHandler> handlers) : IPaymentHandlerResolver
{
    public IPaymentHandler Resolve(PaymentMethod method) =>
        handlers.FirstOrDefault(handler => handler.Method == method)
        ?? throw new UnsupportedPaymentMethodException(method);
}
