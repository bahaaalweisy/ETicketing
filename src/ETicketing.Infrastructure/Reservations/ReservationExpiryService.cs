using ETicketing.Application.Abstractions;
using ETicketing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ETicketing.Infrastructure.Reservations;

/// <summary>
/// Runs every minute: marks unpaid orders past their reservation as Expired and releases their stock.
/// An order with a recent Pending transaction is skipped, because that payment decides how the
/// order ends. A Pending transaction older than the configured timeout is stuck, so it is failed,
/// which releases the stock.
/// </summary>
public sealed class ReservationExpiryService(
    IServiceScopeFactory scopeFactory,
    IClock clock,
    IOptions<ReservationExpiryOptions> options,
    ILogger<ReservationExpiryService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Settles every order that is due, one database transaction per order.
    /// </summary>
    public async Task ExpireDueOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var inventory = scope.ServiceProvider.GetRequiredService<ITicketInventory>();
        var completionService = scope.ServiceProvider.GetRequiredService<IPaymentCompletionService>();
        var utcNow = clock.UtcNow;

        var dueOrderIds = await db.Orders
            .Where(order => order.Status == OrderStatus.PendingPayment && order.ReservationExpiresAt <= utcNow)
            .Select(order => order.Id)
            .ToListAsync(cancellationToken);

        foreach (var orderId in dueOrderIds)
        {
            var pendingPayment = await db.Transactions
                .Where(transaction => transaction.OrderId == orderId && transaction.Status == TransactionStatus.Pending)
                .Select(transaction => new { transaction.Id, transaction.CreatedAt })
                .SingleOrDefaultAsync(cancellationToken);

            if (pendingPayment is null)
            {
                await ExpireOrderAsync(db, inventory, orderId, cancellationToken);
            }
            else if (utcNow - pendingPayment.CreatedAt >= options.Value.PendingPaymentTimeout)
            {
                logger.LogWarning("Transaction {TransactionId} is stuck in Pending, failing it", pendingPayment.Id);
                await completionService.FailAsync(pendingPayment.Id, cancellationToken);
            }
            else
            {
                logger.LogInformation("Order {OrderId} has a payment in progress, skipping it", orderId);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ExpireDueOrdersAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Expiring reservations failed, retrying on the next tick");
            }
        }
    }

    private async Task ExpireOrderAsync(
        IAppDbContext db,
        ITicketInventory inventory,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var dbTransaction = await db.BeginTransactionAsync(cancellationToken);

            var order = await db.Orders.SingleAsync(order => order.Id == orderId, cancellationToken);
            if (order.Status != OrderStatus.PendingPayment)
            {
                return;
            }

            order.MarkExpired();
            await db.SaveChangesAsync(cancellationToken);
            await inventory.ReleaseAsync(order.TicketId, order.Quantity, cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);
            logger.LogInformation("Order {OrderId} expired and released its stock", orderId);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A payment moved the order first, so the order keeps its stock.
            db.ChangeTracker.Clear();
            logger.LogInformation("Order {OrderId} was paid while expiring, skipping it", orderId);
        }
    }
}
