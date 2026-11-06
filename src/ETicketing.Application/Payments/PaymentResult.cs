using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;

namespace ETicketing.Application.Payments;

/// <summary>
/// Outcome of a payment handler. It covers instant (Completed) and deferred (Pending) payments.
/// TransactionId is null only when a Failed attempt never created a transaction.
/// </summary>
public sealed record PaymentResult(TransactionStatus Status, Guid? TransactionId, string? Reference, string Message)
{
    public static PaymentResult Completed(Transaction transaction) =>
        new(TransactionStatus.Completed, transaction.Id, transaction.Reference, "Payment completed.");

    public static PaymentResult Pending(Transaction transaction) =>
        new(TransactionStatus.Pending, transaction.Id, transaction.Reference, "Payment is being processed.");

    public static PaymentResult Failed(string message, Transaction? transaction = null) =>
        new(TransactionStatus.Failed, transaction?.Id, transaction?.Reference, message);
}
