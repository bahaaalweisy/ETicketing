namespace ETicketing.Domain.Exceptions;

public sealed class PaymentFailedException(string message) : DomainException(message);
