namespace ETicketing.Domain.Exceptions;

/// <summary>
/// A payment handler reported a business failure, such as a payment already in progress.
/// </summary>
public sealed class PaymentFailedException(string message) : DomainException(message);
