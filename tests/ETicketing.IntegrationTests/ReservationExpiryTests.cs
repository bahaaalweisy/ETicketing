using System.Net;
using System.Net.Http.Json;
using ETicketing.Application.Abstractions;
using ETicketing.Application.Ledger;
using ETicketing.Application.Orders;
using ETicketing.Application.Payments;
using ETicketing.Application.Transactions;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;
using ETicketing.Infrastructure.Reservations;
using ETicketing.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ETicketing.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ReservationExpiryTests(ApiFixture api)
{
    [Fact]
    public async Task An_unpaid_order_past_its_reservation_expires_and_returns_its_stock()
    {
        await api.SetStockAsync(ApiFixture.PremiumTicketId);
        var order = await api.BookAsync(ApiFixture.PremiumTicketId, 3);

        await ExpireAsync(order.Id);

        (await api.GetAsync<OrderResponse>($"/api/orders/{order.Id}")).Status.Should().Be(OrderStatus.Expired);

        var ticket = await api.WithDbAsync(db => db.Tickets.AsNoTracking().SingleAsync(item => item.Id == ApiFixture.PremiumTicketId));
        ticket.Reserved.Should().Be(0);

        var response = await api.PostCheckoutAsync(order.Id, "CreditCard");
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task A_QR_payment_in_progress_when_the_reservation_ends_is_not_expired_and_completes()
    {
        await api.SetStockAsync(ApiFixture.PremiumTicketId);
        var ledgerBefore = await api.GetAsync<LedgerBalanceResponse>("/api/ledger/balance");
        var order = await api.BookAsync(ApiFixture.PremiumTicketId);

        var response = await api.PostCheckoutAsync(order.Id, "QR");
        var checkout = await response.Content.ReadFromJsonAsync<CheckoutResponse>(ApiFixture.JsonOptions);
        await ExpireAsync(order.Id);

        (await api.GetAsync<OrderResponse>($"/api/orders/{order.Id}")).Status.Should().Be(OrderStatus.PendingPayment);
        (await GetPremiumTicketAsync()).Reserved.Should().Be(1);

        await Task.Delay(TimeSpan.FromSeconds(10));

        var transaction = await api.GetAsync<TransactionResponse>($"/api/transactions/{checkout!.TransactionId}");
        transaction.Status.Should().Be(TransactionStatus.Completed);
        (await api.GetAsync<OrderResponse>($"/api/orders/{order.Id}")).Status.Should().Be(OrderStatus.Paid);

        var ticket = await GetPremiumTicketAsync();
        ticket.Sold.Should().Be(1);
        ticket.Reserved.Should().Be(0);

        var ledger = await api.GetAsync<LedgerBalanceResponse>("/api/ledger/balance");
        ledger.TotalDebits.Should().Be(ledgerBefore.TotalDebits + order.TotalAmount);
        ledger.TotalCredits.Should().Be(ledger.TotalDebits);
    }

    [Fact]
    public async Task A_payment_recorded_just_after_its_order_expired_fails_without_selling_or_posting()
    {
        await api.SetStockAsync(ApiFixture.PremiumTicketId);
        var ledgerBefore = await api.GetAsync<LedgerBalanceResponse>("/api/ledger/balance");
        var order = await api.BookAsync(ApiFixture.PremiumTicketId);
        await ExpireAsync(order.Id);

        // The state a checkout leaves if it passed its checks just before expiry committed.
        var pending = await AddPendingAsync(order, PaymentMethod.QR);

        using var scope = api.Factory.Services.CreateScope();
        var status = await scope.ServiceProvider.GetRequiredService<IPaymentCompletionService>()
            .CompleteAsync(pending.Id, CancellationToken.None);

        status.Should().Be(TransactionStatus.Failed);

        var ticket = await GetPremiumTicketAsync();
        ticket.Sold.Should().Be(0);
        ticket.Reserved.Should().Be(0);

        var ledger = await api.GetAsync<LedgerBalanceResponse>("/api/ledger/balance");
        ledger.TotalDebits.Should().Be(ledgerBefore.TotalDebits);
    }

    [Fact]
    public async Task A_stuck_pending_payment_older_than_the_threshold_fails_and_releases_its_stock()
    {
        await api.SetStockAsync(ApiFixture.PremiumTicketId);
        var ledgerBefore = await api.GetAsync<LedgerBalanceResponse>("/api/ledger/balance");
        var order = await api.BookAsync(ApiFixture.PremiumTicketId, 2);

        // The state an aborted credit card request leaves: Pending, and in no queue.
        var stuck = await AddPendingAsync(order, PaymentMethod.CreditCard);
        await AgeAsync(stuck.Id, TimeSpan.FromMinutes(3));
        await ExpireAsync(order.Id);

        var transaction = await api.GetAsync<TransactionResponse>($"/api/transactions/{stuck.Id}");
        transaction.Status.Should().Be(TransactionStatus.Failed);
        (await api.GetAsync<OrderResponse>($"/api/orders/{order.Id}")).Status.Should().Be(OrderStatus.Failed);

        var ticket = await GetPremiumTicketAsync();
        ticket.Sold.Should().Be(0);
        ticket.Reserved.Should().Be(0);

        var ledger = await api.GetAsync<LedgerBalanceResponse>("/api/ledger/balance");
        ledger.TotalDebits.Should().Be(ledgerBefore.TotalDebits);
    }

    [Fact]
    public async Task The_stuck_payment_threshold_comes_from_configuration()
    {
        await using var host = api.Factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ReservationExpiry:PendingPaymentTimeout", "00:00:30"));
        _ = host.Server;

        await api.SetStockAsync(ApiFixture.PremiumTicketId);
        var order = await api.BookAsync(ApiFixture.PremiumTicketId);
        var pending = await AddPendingAsync(order, PaymentMethod.CreditCard);

        // One minute is under the two-minute default, so only the configured value can fail it.
        await AgeAsync(pending.Id, TimeSpan.FromMinutes(1));
        await MoveReservationDeadlineToThePastAsync(order.Id);
        await host.Services.GetRequiredService<ReservationExpiryService>().ExpireDueOrdersAsync(CancellationToken.None);

        var transaction = await api.GetAsync<TransactionResponse>($"/api/transactions/{pending.Id}");
        transaction.Status.Should().Be(TransactionStatus.Failed);
    }

    private async Task<Transaction> AddPendingAsync(OrderResponse order, PaymentMethod method)
    {
        var pending = Transaction.CreatePending(order.Id, method, order.TotalAmount);
        await api.WithDbAsync(db =>
        {
            db.Transactions.Add(pending);
            return db.SaveChangesAsync();
        });

        return pending;
    }

    private Task AgeAsync(Guid transactionId, TimeSpan age)
    {
        var createdAt = DateTime.UtcNow - age;

        return api.WithDbAsync(db => db.Transactions
            .Where(transaction => transaction.Id == transactionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(transaction => transaction.CreatedAt, createdAt)));
    }

    private Task<Ticket> GetPremiumTicketAsync() =>
        api.WithDbAsync(db => db.Tickets.AsNoTracking().SingleAsync(item => item.Id == ApiFixture.PremiumTicketId));

    private Task MoveReservationDeadlineToThePastAsync(Guid orderId) =>
        api.WithDbAsync(db => db.Orders
            .Where(order => order.Id == orderId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(order => order.ReservationExpiresAt, DateTime.UtcNow.AddMinutes(-1))));

    private async Task ExpireAsync(Guid orderId)
    {
        await MoveReservationDeadlineToThePastAsync(orderId);

        await api.Factory.Services.GetRequiredService<ReservationExpiryService>().ExpireDueOrdersAsync(CancellationToken.None);
    }
}
