using ETicketing.Domain.Enums;

namespace ETicketing.Application.Payments;

/// <summary>
/// Result of a checkout. Status is Completed for instant payments and Pending for deferred ones.
/// </summary>
public sealed record CheckoutResponse(
    Guid OrderId,
    Guid TransactionId,
    TransactionStatus Status,
    string Reference,
    string Message);
