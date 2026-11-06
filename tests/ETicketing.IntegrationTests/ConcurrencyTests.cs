using System.Net;
using ETicketing.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ETicketing.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ConcurrencyTests(ApiFixture api)
{
    [Fact]
    public async Task Twenty_parallel_bookings_for_the_last_ticket_sell_it_exactly_once()
    {
        await api.SetStockAsync(ApiFixture.GoldTicketId, sold: 99);

        var responses = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => api.PostBookingAsync(ApiFixture.GoldTicketId, 1, Guid.NewGuid().ToString())));

        responses.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(19);

        var ticket = await api.WithDbAsync(db => db.Tickets.AsNoTracking().SingleAsync(item => item.Id == ApiFixture.GoldTicketId));
        (ticket.Sold + ticket.Reserved).Should().Be(ticket.Quota);
    }

    [Fact]
    public async Task Parallel_bookings_for_more_than_the_stock_stop_exactly_at_the_quota()
    {
        await api.SetStockAsync(ApiFixture.GoldTicketId, sold: 90);

        var responses = await Task.WhenAll(Enumerable.Range(0, 30)
            .Select(_ => api.PostBookingAsync(ApiFixture.GoldTicketId, 1, Guid.NewGuid().ToString())));

        responses.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(10);

        var ticket = await api.WithDbAsync(db => db.Tickets.AsNoTracking().SingleAsync(item => item.Id == ApiFixture.GoldTicketId));
        ticket.Available.Should().Be(0);
    }

    [Fact]
    public async Task The_database_itself_refuses_to_oversell()
    {
        var act = () => api.SetStockAsync(ApiFixture.GoldTicketId, sold: 100, reserved: 1);

        await act.Should().ThrowAsync<Microsoft.Data.SqlClient.SqlException>().WithMessage("*CK_Tickets_Stock*");
    }
}
