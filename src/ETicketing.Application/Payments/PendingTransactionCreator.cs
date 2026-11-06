using ETicketing.Application.Abstractions;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;
using ETicketing.Domain.Exceptions;

namespace ETicketing.Application.Payments;

/// <summary>
/// Creates the Pending transaction every handler starts from, so a new handler does not
/// repeat the duplicate-payment handling.
/// </summary>
public sealed class PendingTransactionCreator(IAppDbContext db)
{
    /// <summary>
    /// Returns the new transaction, or null when the unique index reports that the order
    /// already has a live payment.
    /// </summary>
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
