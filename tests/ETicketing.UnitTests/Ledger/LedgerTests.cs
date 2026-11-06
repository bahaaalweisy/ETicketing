using ETicketing.Application.Abstractions;
using ETicketing.Application.Ledger;
using ETicketing.Application.Payments;
using ETicketing.Domain.Constants;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;
using ETicketing.Domain.Exceptions;
using ETicketing.Infrastructure.Persistence;
using ETicketing.UnitTests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ETicketing.UnitTests.Ledger;

public class LedgerTests
{
    private readonly AppDbContext _db = TestDatabase.Create();
    private readonly Mock<ITicketInventory> _inventory = new();

    [Fact]
    public async Task A_completed_payment_creates_exactly_two_entries_of_equal_amount()
    {
        var transaction = await CreatePendingTransactionAsync(quantity: 3);

        await CreateCompletionService().CompleteAsync(transaction.Id, CancellationToken.None);

        var entries = await _db.LedgerEntries.Include(entry => entry.Account).ToListAsync();
        entries.Should().HaveCount(2);

        var debit = entries.Single(entry => entry.Debit > 0);
        var credit = entries.Single(entry => entry.Credit > 0);
        debit.Account.Code.Should().Be(AccountCodes.CashGateway);
        credit.Account.Code.Should().Be(AccountCodes.TicketSalesRevenue);
        debit.Debit.Should().Be(credit.Credit);
    }

    [Fact]
    public async Task The_amount_equals_the_order_total()
    {
        var transaction = await CreatePendingTransactionAsync(quantity: 3);
        var orderTotal = (await _db.Orders.SingleAsync()).TotalAmount;

        await CreateCompletionService().CompleteAsync(transaction.Id, CancellationToken.None);

        orderTotal.Should().Be(300m);
        (await _db.LedgerEntries.SumAsync(entry => entry.Debit)).Should().Be(orderTotal);
        (await _db.LedgerEntries.SumAsync(entry => entry.Credit)).Should().Be(orderTotal);
    }

    [Fact]
    public async Task Completing_marks_the_payment_and_order_and_confirms_the_stock()
    {
        var transaction = await CreatePendingTransactionAsync(quantity: 2);

        var status = await CreateCompletionService().CompleteAsync(transaction.Id, CancellationToken.None);

        status.Should().Be(TransactionStatus.Completed);
        (await _db.Orders.SingleAsync()).Status.Should().Be(OrderStatus.Paid);
        _inventory.Verify(inventory => inventory.ConfirmAsync(TestDatabase.GoldTicketId, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Completing_twice_changes_nothing_the_second_time()
    {
        var transaction = await CreatePendingTransactionAsync();
        var service = CreateCompletionService();

        await service.CompleteAsync(transaction.Id, CancellationToken.None);
        await service.CompleteAsync(transaction.Id, CancellationToken.None);

        (await _db.LedgerEntries.CountAsync()).Should().Be(2);
        _inventory.Verify(inventory => inventory.ConfirmAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_payment_for_an_expired_order_fails_without_selling_or_posting()
    {
        var transaction = await CreatePendingTransactionAsync();
        (await _db.Orders.SingleAsync()).MarkExpired();
        await _db.SaveChangesAsync();

        var status = await CreateCompletionService().CompleteAsync(transaction.Id, CancellationToken.None);

        status.Should().Be(TransactionStatus.Failed);
        (await _db.LedgerEntries.CountAsync()).Should().Be(0);
        _inventory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Failing_a_payment_fails_the_order_and_releases_the_stock_without_posting()
    {
        var transaction = await CreatePendingTransactionAsync(quantity: 2);

        await CreateCompletionService().FailAsync(transaction.Id, CancellationToken.None);

        (await _db.Orders.SingleAsync()).Status.Should().Be(OrderStatus.Failed);
        (await _db.LedgerEntries.CountAsync()).Should().Be(0);
        _inventory.Verify(inventory => inventory.ReleaseAsync(TestDatabase.GoldTicketId, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task An_unbalanced_transaction_is_rejected()
    {
        var transaction = await CreatePendingTransactionAsync();
        _db.LedgerEntries.Add(LedgerEntry.CreateDebit(transaction.Id, 1, 100m));
        await _db.SaveChangesAsync();

        var act = () => new LedgerService(_db).EnsureBalancedAsync(transaction.Id, CancellationToken.None);

        await act.Should().ThrowAsync<UnbalancedLedgerException>();
    }

    [Fact]
    public async Task The_balance_reports_equal_totals_and_a_balance_per_account()
    {
        var transaction = await CreatePendingTransactionAsync(quantity: 2);
        await CreateCompletionService().CompleteAsync(transaction.Id, CancellationToken.None);

        var balance = await new LedgerService(_db).GetBalanceAsync(CancellationToken.None);

        balance.TotalDebits.Should().Be(200m);
        balance.TotalCredits.Should().Be(200m);
        balance.Accounts.Should().OnlyContain(account => account.Balance == 200m);
    }

    private PaymentCompletionService CreateCompletionService() => new(
        _db,
        _inventory.Object,
        new LedgerService(_db),
        new FixedClock(),
        NullLogger<PaymentCompletionService>.Instance);

    private async Task<Transaction> CreatePendingTransactionAsync(int quantity = 1)
    {
        var ticket = await _db.Tickets.SingleAsync(item => item.Id == TestDatabase.GoldTicketId);
        var order = Order.Create(ticket, quantity, Guid.NewGuid().ToString(), new FixedClock().UtcNow);
        var transaction = Transaction.CreatePending(order.Id, PaymentMethod.CreditCard, order.TotalAmount);

        _db.Orders.Add(order);
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();

        return transaction;
    }
}
