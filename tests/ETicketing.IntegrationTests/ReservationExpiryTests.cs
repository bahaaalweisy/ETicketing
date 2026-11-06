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
        var pending = Transaction.CreatePending(order.Id, PaymentMethod.QR, order.TotalAmount);
        await api.WithDbAsync(db =>
        {
            db.Transactions.Add(pending);
            return db.SaveChangesAsync();
        });

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

    private Task<Ticket> GetPremiumTicketAsync() =>
        api.WithDbAsync(db => db.Tickets.AsNoTracking().SingleAsync(item => item.Id == ApiFixture.PremiumTicketId));

    private async Task ExpireAsync(Guid orderId)
    {
        await api.WithDbAsync(db => db.Orders
            .Where(order => order.Id == orderId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(order => order.ReservationExpiresAt, DateTime.UtcNow.AddMinutes(-1))));

        await api.Factory.Services.GetRequiredService<ReservationExpiryService>().ExpireDueOrdersAsync(CancellationToken.None);
    }
}
