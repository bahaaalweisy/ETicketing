using System.Net;
using System.Net.Http.Json;
using ETicketing.Application.Orders;
using ETicketing.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ETicketing.IntegrationTests;

[Collection(ApiCollection.Name)]
public class IdempotencyTests(ApiFixture api)
{
    [Fact]
    public async Task The_same_key_twice_creates_one_order()
    {
        await api.SetStockAsync(ApiFixture.PremiumTicketId);
        var key = Guid.NewGuid().ToString();

        var first = await api.PostBookingAsync(ApiFixture.PremiumTicketId, 2, key);
        var second = await api.PostBookingAsync(ApiFixture.PremiumTicketId, 2, key);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstOrder = await first.Content.ReadFromJsonAsync<OrderResponse>(ApiFixture.JsonOptions);
        var secondOrder = await second.Content.ReadFromJsonAsync<OrderResponse>(ApiFixture.JsonOptions);
        secondOrder!.Id.Should().Be(firstOrder!.Id);

        (await api.WithDbAsync(db => db.Orders.CountAsync(order => order.IdempotencyKey == key))).Should().Be(1);

        var ticket = await api.WithDbAsync(db => db.Tickets.AsNoTracking().SingleAsync(item => item.Id == ApiFixture.PremiumTicketId));
        ticket.Reserved.Should().Be(2);
    }

    [Fact]
    public async Task Parallel_retries_of_the_last_seat_all_receive_the_same_order()
    {
        await api.SetStockAsync(ApiFixture.GoldTicketId, sold: 99);
        var key = Guid.NewGuid().ToString();

        var responses = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => api.PostBookingAsync(ApiFixture.GoldTicketId, 1, key)));

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Created);
        (await api.WithDbAsync(db => db.Orders.CountAsync(order => order.IdempotencyKey == key))).Should().Be(1);
    }
}
