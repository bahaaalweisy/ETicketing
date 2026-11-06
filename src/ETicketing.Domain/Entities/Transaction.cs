using ETicketing.Domain.Common;
using ETicketing.Domain.Enums;
using ETicketing.Domain.Exceptions;

namespace ETicketing.Domain.Entities;

/// <summary>
/// One payment attempt for an order, with its own Pending to Completed or Failed state machine.
/// </summary>
public class Transaction : AuditableEntity
{
    private Transaction()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Order Order { get; private set; } = null!;

    public PaymentMethod Method { get; private set; }

    public TransactionStatus Status { get; private set; }

    public decimal Amount { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public DateTime? CompletedAt { get; private set; }

    public static Transaction CreatePending(Guid orderId, PaymentMethod method, decimal amount)
    {
        var id = Guid.NewGuid();

        return new Transaction
        {
            Id = id,
            OrderId = orderId,
            Method = method,
            Status = TransactionStatus.Pending,
            Amount = amount,
            Reference = $"{method}-{id:N}".ToUpperInvariant()
        };
    }

    public void Complete(DateTime utcNow)
    {
        EnsurePending();
        Status = TransactionStatus.Completed;
        CompletedAt = utcNow;
    }

    public void Fail()
    {
        EnsurePending();
        Status = TransactionStatus.Failed;
    }

    private void EnsurePending()
    {
        if (Status != TransactionStatus.Pending)
        {
            throw new InvalidTransactionStateException($"Transaction {Id} is {Status} and can no longer be changed.");
        }
    }
}
