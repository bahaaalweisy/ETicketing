namespace ETicketing.Application.Payments;

/// <summary>
/// What a payment handler needs to know about the order being paid.
/// </summary>
public sealed record PaymentContext(Guid OrderId, decimal Amount);
