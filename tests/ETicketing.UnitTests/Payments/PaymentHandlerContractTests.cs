using ETicketing.Application.Abstractions;
using ETicketing.Application.Payments;
using ETicketing.Application.Payments.Handlers;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;
using ETicketing.UnitTests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ETicketing.UnitTests.Payments;

public class PaymentHandlerContractTests
{
    private readonly Mock<IPaymentCompletionService> _completionService = new();
    private readonly Mock<IPaymentQueue> _paymentQueue = new();

    [Fact]
    public async Task CreditCard_completes_through_the_completion_service()
    {
        using var db = TestDatabase.Create();
        var context = await CreateContextAsync(db);
        _completionService
            .Setup(service => service.CompleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionStatus.Completed);
        var handler = new CreditCardHandler(new PendingTransactionCreator(db), _completionService.Object);

        var result = await handler.ProcessAsync(context, CancellationToken.None);

        result.Status.Should().Be(TransactionStatus.Completed);
        _completionService.Verify(service => service.CompleteAsync(result.TransactionId!.Value, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditCard_reports_a_lost_order_as_Failed_instead_of_throwing()
    {
        using var db = TestDatabase.Create();
        var context = await CreateContextAsync(db);
        _completionService
            .Setup(service => service.CompleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionStatus.Failed);
        var handler = new CreditCardHandler(new PendingTransactionCreator(db), _completionService.Object);

        var result = await handler.ProcessAsync(context, CancellationToken.None);

        result.Status.Should().Be(TransactionStatus.Failed);
    }

    [Fact]
    public async Task QR_creates_a_pending_transaction_queues_it_and_returns_immediately()
    {
        using var db = TestDatabase.Create();
        var context = await CreateContextAsync(db);
        var handler = new QRHandler(new PendingTransactionCreator(db), _paymentQueue.Object);

        var result = await handler.ProcessAsync(context, CancellationToken.None);

        result.Status.Should().Be(TransactionStatus.Pending);
        var transaction = await db.Transactions.SingleAsync();
        transaction.Status.Should().Be(TransactionStatus.Pending);
        transaction.Amount.Should().Be(context.Amount);
        _paymentQueue.Verify(queue => queue.EnqueueAsync(transaction.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async Task<PaymentContext> CreateContextAsync(ETicketing.Infrastructure.Persistence.AppDbContext db)
    {
        var ticket = await db.Tickets.SingleAsync(item => item.Id == TestDatabase.GoldTicketId);
        var order = Order.Create(ticket, 1, Guid.NewGuid().ToString(), new FixedClock().UtcNow);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return new PaymentContext(order.Id, order.TotalAmount);
    }
}
