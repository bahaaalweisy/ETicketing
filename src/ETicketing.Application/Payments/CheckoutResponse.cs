using ETicketing.Domain.Enums;

namespace ETicketing.Application.Payments;

public sealed record CheckoutResponse(
    Guid OrderId,
    Guid TransactionId,
    TransactionStatus Status,
    string Reference,
    string Message);
