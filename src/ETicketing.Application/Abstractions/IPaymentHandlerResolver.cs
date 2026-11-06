using ETicketing.Domain.Enums;

namespace ETicketing.Application.Abstractions;

/// <summary>
/// Picks the payment strategy for a method.
/// </summary>
public interface IPaymentHandlerResolver
{
    /// <summary>
    /// Returns the handler registered for the method.
    /// </summary>
    /// <exception cref="ETicketing.Domain.Exceptions.UnsupportedPaymentMethodException">No handler is registered.</exception>
    IPaymentHandler Resolve(PaymentMethod method);
}
