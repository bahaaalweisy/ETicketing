using System.Net;
using System.Net.Http.Json;
using ETicketing.Application.Ledger;
using ETicketing.Application.Orders;
using ETicketing.Application.Payments;
using ETicketing.Application.Tickets;
using ETicketing.Application.Transactions;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;
using ETicketing.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ETicketing.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CreditCardCheckoutTests(ApiFixture api)
{
    [Fact]
    public async Task Booking_then_checkout_pays_the_order_sells_the_stock_and_balances_the_ledger()
    {
        await api.SetStockAsync(ApiFixture.PremiumTicketId);
        var ledgerBefore = await api.GetAsync<LedgerBalanceResponse>("/api/ledger/balance");
        var order = await api.BookAsync(ApiFixture.PremiumTicketId, 2);

        var response = await api.PostCheckoutAsync(order.Id, "CreditCard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkout = await response.Content.ReadFromJsonAsync<CheckoutResponse>(ApiFixture.JsonOptions);
        checkout!.Status.Should().Be(TransactionStatus.Completed);

        (await api.GetAsync<OrderResponse>($"/api/orders/{order.Id}")).Status.Should().Be(OrderStatus.Paid);

        var tickets = await api.GetAsync<List<TicketResponse>>("/api/tickets");
        var ticket = tickets.Single(item => item.Id == ApiFixture.PremiumTicketId);
        ticket.Sold.Should().Be(2);
        ticket.Reserved.Should().Be(0);
        ticket.Available.Should().Be(98);

        var transaction = await api.GetAsync<TransactionResponse>($"/api/transactions/{checkout.TransactionId}");
        transaction.Status.Should().Be(TransactionStatus.Completed);
        transaction.Amount.Should().Be(400m);
        transaction.CompletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);

        var ledger = await api.GetAsync<LedgerBalanceResponse>("/api/ledger/balance");
        ledger.TotalDebits.Should().Be(ledger.TotalCredits);
        (ledger.TotalDebits - ledgerBefore.TotalDebits).Should().Be(400m);

        var entries = await api.WithDbAsync(db => db.LedgerEntries.CountAsync(entry => entry.TransactionId == checkout.TransactionId));
        entries.Should().Be(2);
    }

    [Fact]
    public async Task Every_status_change_is_recorded_in_the_audit_log()
    {
        await api.SetStockAsync(ApiFixture.PremiumTicketId);
        var order = await api.BookAsync(ApiFixture.PremiumTicketId);

        await api.PostCheckoutAsync(order.Id, "CreditCard");

        var orderId = order.Id.ToString();
        var logs = await api.WithDbAsync(db => db.AuditLogs
            .Where(log => log.EntityName == nameof(Order) && log.EntityId == orderId)
            .OrderBy(log => log.Id)
            .Select(log => log.ToStatus)
            .ToListAsync());

        logs.Should().Equal(nameof(OrderStatus.PendingPayment), nameof(OrderStatus.Paid));
    }

    [Fact]
    public async Task Parallel_checkouts_of_one_order_charge_it_exactly_once()
    {
        await api.SetStockAsync(ApiFixture.PremiumTicketId);
        var order = await api.BookAsync(ApiFixture.PremiumTicketId);

        var responses = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => api.PostCheckoutAsync(order.Id, "CreditCard")));

        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);

        var charges = await api.WithDbAsync(db => db.Transactions
            .CountAsync(transaction => transaction.OrderId == order.Id && transaction.Status == TransactionStatus.Completed));
        charges.Should().Be(1);

        var ticket = await api.WithDbAsync(db => db.Tickets.AsNoTracking().SingleAsync(item => item.Id == ApiFixture.PremiumTicketId));
        ticket.Sold.Should().Be(1);
    }

    [Fact]
    public async Task Paying_a_paid_order_again_returns_422()
    {
        await api.SetStockAsync(ApiFixture.PremiumTicketId);
        var order = await api.BookAsync(ApiFixture.PremiumTicketId);
        await api.PostCheckoutAsync(order.Id, "CreditCard");

        var response = await api.PostCheckoutAsync(order.Id, "CreditCard");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
