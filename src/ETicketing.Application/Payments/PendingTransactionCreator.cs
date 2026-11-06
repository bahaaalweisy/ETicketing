using ETicketing.Application.Abstractions;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;
using ETicketing.Domain.Exceptions;

namespace ETicketing.Application.Payments;

public sealed class PendingTransactionCreator(IAppDbContext db)
{
    public async Task<Transaction?> CreateAsync(
        PaymentContext context,
        PaymentMethod method,
        CancellationToken cancellationToken)
    {
        var transaction = Transaction.CreatePending(context.OrderId, method, context.Amount);
        db.Transactions.Add(transaction);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return transaction;
        }
        catch (DuplicateEntryException)
        {
            db.ChangeTracker.Clear();
            return null;
        }
    }
}
