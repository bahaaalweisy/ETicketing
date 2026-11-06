using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using ETicketing.Application.Ledger;
using ETicketing.Application.Orders;
using ETicketing.Application.Payments;
using ETicketing.Application.Transactions;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;
using ETicketing.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ETicketing.IntegrationTests;

[Collection(ApiCollection.Name)]
public class QrCheckoutTests(ApiFixture api)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

    [Fact]
    public async Task Checkout_returns_Pending_at_once_then_Completed_after_about_eight_seconds()
    {
        await api.SetStockAsync(ApiFixture.PremiumTicketId);
        var order = await api.BookAsync(ApiFixture.PremiumTicketId);
        var stopwatch = Stopwatch.StartNew();

        var response = await api.PostCheckoutAsync(order.Id, "QR");

        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var checkout = await response.Content.ReadFromJsonAsync<CheckoutResponse>(ApiFixture.JsonOptions);
        checkout!.Status.Should().Be(TransactionStatus.Pending);

        var transaction = await WaitUntilFinishedAsync(api, checkout.TransactionId);

        transaction.Status.Should().Be(TransactionStatus.Completed);
        stopwatch.Elapsed.Should().BeGreaterThan(TimeSpan.FromSeconds(7.5)).And.BeLessThan(TimeSpan.FromSeconds(12));

        (await api.GetAsync<OrderResponse>($"/api/orders/{order.Id}")).Status.Should().Be(OrderStatus.Paid);

        var ledger = await api.GetAsync<LedgerBalanceResponse>("/api/ledger/balance");
        ledger.TotalDebits.Should().Be(ledger.TotalCredits);

        var entries = await api.WithDbAsync(db => db.LedgerEntries.CountAsync(entry => entry.TransactionId == checkout.TransactionId));
        entries.Should().Be(2);
    }

    [Fact]
    public async Task Several_payments_wait_concurrently_instead_of_one_after_another()
    {
        await api.SetStockAsync(ApiFixture.PremiumTicketId);
        var orders = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => api.BookAsync(ApiFixture.PremiumTicketId)));
        var stopwatch = Stopwatch.StartNew();

        var responses = await Task.WhenAll(orders.Select(order => api.PostCheckoutAsync(order.Id, "QR")));
        var checkouts = await Task.WhenAll(responses.Select(response =>
            response.Content.ReadFromJsonAsync<CheckoutResponse>(ApiFixture.JsonOptions)));
        var transactions = await Task.WhenAll(checkouts.Select(checkout => WaitUntilFinishedAsync(api, checkout!.TransactionId)));

        transactions.Should().OnlyContain(transaction => transaction.Status == TransactionStatus.Completed);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(14));
    }

    [Fact]
    public async Task A_second_checkout_while_one_is_pending_returns_409()
    {
        await api.SetStockAsync(ApiFixture.PremiumTicketId);
        var order = await api.BookAsync(ApiFixture.PremiumTicketId);

        var first = await api.PostCheckoutAsync(order.Id, "QR");
        var second = await api.PostCheckoutAsync(order.Id, "CreditCard");

        first.StatusCode.Should().Be(HttpStatusCode.Accepted);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Let the payment finish, so it cannot settle against the stock of the next test.
        var checkout = await first.Content.ReadFromJsonAsync<CheckoutResponse>(ApiFixture.JsonOptions);
        var transaction = await WaitUntilFinishedAsync(api, checkout!.TransactionId);
        transaction.Status.Should().Be(TransactionStatus.Completed);
    }

    [Fact]
    public async Task A_pending_transaction_survives_a_restart_and_completes()
    {
        await api.SetStockAsync(ApiFixture.PremiumTicketId);
        var order = await api.BookAsync(ApiFixture.PremiumTicketId);

        // The state a crash leaves behind: Pending in the database, nothing in the in-memory queue.
        var pending = Transaction.CreatePending(order.Id, PaymentMethod.QR, order.TotalAmount);
        await api.WithDbAsync(db =>
        {
            db.Transactions.Add(pending);
            return db.SaveChangesAsync();
        });

        await using var restarted = api.CreateFactory();
        _ = restarted.Server;

        var transaction = await WaitUntilFinishedAsync(api, pending.Id);

        transaction.Status.Should().Be(TransactionStatus.Completed);
        (await api.GetAsync<OrderResponse>($"/api/orders/{order.Id}")).Status.Should().Be(OrderStatus.Paid);
    }

    private static async Task<TransactionResponse> WaitUntilFinishedAsync(ApiFixture api, Guid transactionId)
    {
        var deadline = Stopwatch.StartNew();

        while (true)
        {
            var transaction = await api.GetAsync<TransactionResponse>($"/api/transactions/{transactionId}");
            if (transaction.Status != TransactionStatus.Pending || deadline.Elapsed > Timeout)
            {
                return transaction;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }
    }
}
