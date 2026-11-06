using ETicketing.Domain.Enums;

namespace ETicketing.Domain.Exceptions;

/// <summary>
/// No payment handler is registered for the requested method.
/// </summary>
public sealed class UnsupportedPaymentMethodException(PaymentMethod method)
    : DomainException($"Payment method '{method}' is not supported.");
