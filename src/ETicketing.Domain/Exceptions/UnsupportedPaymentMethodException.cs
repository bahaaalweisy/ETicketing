using ETicketing.Domain.Enums;

namespace ETicketing.Domain.Exceptions;

public sealed class UnsupportedPaymentMethodException(PaymentMethod method)
    : DomainException($"Payment method '{method}' is not supported.");
