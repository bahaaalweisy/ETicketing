using System.Net;
using System.Net.Http.Json;
using ETicketing.Application.Ledger;
using ETicketing.Application.Orders;
using ETicketing.Application.Payments;
using ETicketing.Application.Transactions;
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
    public async Task A_QR_payment_that_loses_the_race_to_expiry_fails_without_selling_or_posting()
    {
        await api.SetStockAsync(ApiFixture.PremiumTicketId);
        var ledgerBefore = await api.GetAsync<LedgerBalanceResponse>("/api/ledger/balance");
        var order = await api.BookAsync(ApiFixture.PremiumTicketId);

        var response = await api.PostCheckoutAsync(order.Id, "QR");
        var checkout = await response.Content.ReadFromJsonAsync<CheckoutResponse>(ApiFixture.JsonOptions);
        await ExpireAsync(order.Id);

        await Task.Delay(TimeSpan.FromSeconds(10));

        var transaction = await api.GetAsync<TransactionResponse>($"/api/transactions/{checkout!.TransactionId}");
        transaction.Status.Should().Be(TransactionStatus.Failed);

        var ticket = await api.WithDbAsync(db => db.Tickets.AsNoTracking().SingleAsync(item => item.Id == ApiFixture.PremiumTicketId));
        ticket.Sold.Should().Be(0);
        ticket.Reserved.Should().Be(0);

        var ledger = await api.GetAsync<LedgerBalanceResponse>("/api/ledger/balance");
        ledger.TotalDebits.Should().Be(ledgerBefore.TotalDebits);
    }

    private async Task ExpireAsync(Guid orderId)
    {
        await api.WithDbAsync(db => db.Orders
            .Where(order => order.Id == orderId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(order => order.ReservationExpiresAt, DateTime.UtcNow.AddMinutes(-1))));

        await api.Factory.Services.GetRequiredService<ReservationExpiryService>().ExpireDueOrdersAsync(CancellationToken.None);
    }
}
