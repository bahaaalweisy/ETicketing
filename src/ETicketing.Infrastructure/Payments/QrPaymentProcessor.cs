using ETicketing.Application.Abstractions;
using ETicketing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ETicketing.Infrastructure.Payments;

/// <summary>
/// Simulates the 8-second QR confirmation outside any HTTP request. Each payment waits in its
/// own task, so one QR never delays another. On startup it re-enqueues every Pending
/// transaction, which is what makes a crash mid-wait harmless.
/// </summary>
public sealed class QrPaymentProcessor(
    IPaymentQueue paymentQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<QrPaymentProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan ConfirmationDelay = TimeSpan.FromSeconds(8);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverPendingTransactionsAsync(stoppingToken);

        await foreach (var transactionId in paymentQueue.ReadAllAsync(stoppingToken))
        {
            _ = ProcessAsync(transactionId, stoppingToken);
        }
    }

    private async Task RecoverPendingTransactionsAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var pendingIds = await db.Transactions
            .Where(transaction => transaction.Status == TransactionStatus.Pending)
            .Select(transaction => transaction.Id)
            .ToListAsync(stoppingToken);

        foreach (var transactionId in pendingIds)
        {
            await paymentQueue.EnqueueAsync(transactionId, stoppingToken);
        }

        logger.LogInformation("Recovered {PendingCount} pending transaction(s) on startup", pendingIds.Count);
    }

    private async Task ProcessAsync(Guid transactionId, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(ConfirmationDelay, stoppingToken);

            using var scope = scopeFactory.CreateScope();
            var completionService = scope.ServiceProvider.GetRequiredService<IPaymentCompletionService>();
            var status = await completionService.CompleteAsync(transactionId, stoppingToken);

            logger.LogInformation("Transaction {TransactionId} finished as {Status}", transactionId, status);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Shutting down: transaction {TransactionId} stays Pending for recovery", transactionId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Completing transaction {TransactionId} failed", transactionId);
            await TryFailAsync(transactionId, stoppingToken);
        }
    }

    private async Task TryFailAsync(Guid transactionId, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var completionService = scope.ServiceProvider.GetRequiredService<IPaymentCompletionService>();
            await completionService.FailAsync(transactionId, stoppingToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Transaction {TransactionId} stays Pending for recovery", transactionId);
        }
    }
}
