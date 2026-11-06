using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;

namespace ETicketing.Application.Transactions;

/// <summary>
/// Current state of a payment, polled by the frontend during a QR checkout.
/// </summary>
public sealed record TransactionResponse(
    TransactionStatus Status,
    Guid TransactionId,
    Guid OrderId,
    decimal Amount,
    PaymentMethod Method,
    DateTime CreatedAt,
    DateTime? CompletedAt)
{
    public static TransactionResponse From(Transaction transaction) => new(
        transaction.Status,
        transaction.Id,
        transaction.OrderId,
        transaction.Amount,
        transaction.Method,
        transaction.CreatedAt,
        transaction.CompletedAt);
}
