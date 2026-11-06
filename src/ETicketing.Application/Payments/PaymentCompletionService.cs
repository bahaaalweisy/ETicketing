using ETicketing.Application.Abstractions;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;
using ETicketing.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ETicketing.Application.Payments;

public sealed class PaymentCompletionService(
    IAppDbContext db,
    ITicketInventory inventory,
    ILedgerService ledger,
    IClock clock,
    ILogger<PaymentCompletionService> logger) : IPaymentCompletionService
{
    public Task<TransactionStatus> CompleteAsync(Guid transactionId, CancellationToken cancellationToken) =>
        RetryAfterConflictAsync(transactionId, () => CompleteInDbTransactionAsync(transactionId, cancellationToken));

    public Task FailAsync(Guid transactionId, CancellationToken cancellationToken) =>
        RetryAfterConflictAsync(transactionId, () => FailInDbTransactionAsync(transactionId, cancellationToken));

    private async Task<TransactionStatus> CompleteInDbTransactionAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        await using var dbTransaction = await db.BeginTransactionAsync(cancellationToken);

        var transaction = await LoadAsync(transactionId, cancellationToken);
        if (transaction.Status != TransactionStatus.Pending)
        {
            return transaction.Status;
        }

        var order = transaction.Order;
        if (order.Status != OrderStatus.PendingPayment)
        {
            // The reservation expired first and its stock is already released: nothing to sell.
            transaction.Fail();
            await db.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);
            return transaction.Status;
        }

        transaction.Complete(clock.UtcNow);
        order.MarkPaid();
        await db.SaveChangesAsync(cancellationToken);

        await inventory.ConfirmAsync(order.TicketId, order.Quantity, cancellationToken);
        await ledger.RecordTicketSaleAsync(transaction.Id, transaction.Amount, cancellationToken);
        await ledger.EnsureBalancedAsync(transaction.Id, cancellationToken);

        await dbTransaction.CommitAsync(cancellationToken);
        return transaction.Status;
    }

    private async Task<TransactionStatus> FailInDbTransactionAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        await using var dbTransaction = await db.BeginTransactionAsync(cancellationToken);

        var transaction = await LoadAsync(transactionId, cancellationToken);
        if (transaction.Status != TransactionStatus.Pending)
        {
            return transaction.Status;
        }

        var order = transaction.Order;
        var holdsReservation = order.Status == OrderStatus.PendingPayment;

        transaction.Fail();
        if (holdsReservation)
        {
            order.MarkFailed();
        }

        await db.SaveChangesAsync(cancellationToken);

        if (holdsReservation)
        {
            await inventory.ReleaseAsync(order.TicketId, order.Quantity, cancellationToken);
        }

        await dbTransaction.CommitAsync(cancellationToken);
        return transaction.Status;
    }

    private async Task<Transaction> LoadAsync(Guid transactionId, CancellationToken cancellationToken) =>
        await db.Transactions
            .Include(transaction => transaction.Order)
            .SingleOrDefaultAsync(transaction => transaction.Id == transactionId, cancellationToken)
        ?? throw new NotFoundException(nameof(Transaction), transactionId);

    private async Task<TransactionStatus> RetryAfterConflictAsync(Guid transactionId, Func<Task<TransactionStatus>> action)
    {
        try
        {
            return await action();
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogInformation("Transaction {TransactionId} lost a status race, re-reading it", transactionId);
            db.ChangeTracker.Clear();

            return await action();
        }
    }
}
