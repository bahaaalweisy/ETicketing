using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;
using ETicketing.Infrastructure.Persistence;
using ETicketing.UnitTests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ETicketing.UnitTests.Persistence;

public class AppDbContextRulesTests
{
    private readonly FixedClock _clock = new();
    private readonly AppDbContext _db;

    public AppDbContextRulesTests() => _db = TestDatabase.Create(_clock);

    [Fact]
    public async Task Creating_and_changing_an_order_writes_an_audit_row_for_each_status()
    {
        var order = await AddOrderAsync();

        order.MarkPaid();
        await _db.SaveChangesAsync();

        var logs = await _db.AuditLogs.OrderBy(log => log.Id).ToListAsync();
        logs.Should().HaveCount(2);
        logs[0].FromStatus.Should().BeNull();
        logs[0].ToStatus.Should().Be(nameof(OrderStatus.PendingPayment));
        logs[1].EntityName.Should().Be(nameof(Order));
        logs[1].EntityId.Should().Be(order.Id.ToString());
        logs[1].FromStatus.Should().Be(nameof(OrderStatus.PendingPayment));
        logs[1].ToStatus.Should().Be(nameof(OrderStatus.Paid));
    }

    [Fact]
    public async Task Saving_without_a_status_change_writes_no_audit_row()
    {
        await AddOrderAsync();

        await _db.SaveChangesAsync();

        (await _db.AuditLogs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Timestamps_are_stamped_from_the_clock()
    {
        var order = await AddOrderAsync();
        var createdAt = _clock.UtcNow;

        _clock.UtcNow = createdAt.AddMinutes(1);
        order.MarkPaid();
        await _db.SaveChangesAsync();

        order.CreatedAt.Should().Be(createdAt);
        order.UpdatedAt.Should().Be(_clock.UtcNow);
    }

    [Fact]
    public async Task Hard_deletes_are_rejected()
    {
        var order = await AddOrderAsync();
        _db.Orders.Remove(order);

        var act = () => _db.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Ledger_entries_cannot_be_updated()
    {
        var order = await AddOrderAsync();
        var transaction = Transaction.CreatePending(order.Id, PaymentMethod.QR, order.TotalAmount);
        var entry = LedgerEntry.CreateDebit(transaction.Id, 1, order.TotalAmount);
        _db.Transactions.Add(transaction);
        _db.LedgerEntries.Add(entry);
        await _db.SaveChangesAsync();

        _db.Entry(entry).State = EntityState.Modified;
        var act = () => _db.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private async Task<Order> AddOrderAsync()
    {
        var ticket = await _db.Tickets.SingleAsync(item => item.Id == TestDatabase.GoldTicketId);
        var order = Order.Create(ticket, 1, Guid.NewGuid().ToString(), _clock.UtcNow);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return order;
    }
}
