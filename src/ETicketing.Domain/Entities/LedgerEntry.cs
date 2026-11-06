using ETicketing.Domain.Common;

namespace ETicketing.Domain.Entities;

public class LedgerEntry : AuditableEntity
{
    private LedgerEntry()
    {
    }

    public long Id { get; private set; }

    public Guid TransactionId { get; private set; }

    public Transaction Transaction { get; private set; } = null!;

    public int AccountId { get; private set; }

    public Account Account { get; private set; } = null!;

    public decimal Debit { get; private set; }

    public decimal Credit { get; private set; }

    public static LedgerEntry CreateDebit(Guid transactionId, int accountId, decimal amount) => new()
    {
        TransactionId = transactionId,
        AccountId = accountId,
        Debit = amount
    };

    public static LedgerEntry CreateCredit(Guid transactionId, int accountId, decimal amount) => new()
    {
        TransactionId = transactionId,
        AccountId = accountId,
        Credit = amount
    };
}
