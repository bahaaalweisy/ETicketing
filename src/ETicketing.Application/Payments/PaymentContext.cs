namespace ETicketing.Application.Payments;

public sealed record PaymentContext(Guid OrderId, decimal Amount);
