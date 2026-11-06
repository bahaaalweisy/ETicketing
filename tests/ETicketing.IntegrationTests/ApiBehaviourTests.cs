using System.Net;
using ETicketing.IntegrationTests.Support;
using FluentAssertions;

namespace ETicketing.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ApiBehaviourTests(ApiFixture api)
{
    private const string FrontendOrigin = "http://localhost:5173";

    [Fact]
    public async Task An_invalid_quantity_returns_400_as_problem_details()
    {
        var response = await api.PostBookingAsync(ApiFixture.GoldTicketId, 11, Guid.NewGuid().ToString());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        (await response.Content.ReadAsStringAsync()).Should().Contain("Quantity");
    }

    [Fact]
    public async Task A_missing_idempotency_key_returns_400()
    {
        var response = await api.PostBookingAsync(ApiFixture.GoldTicketId, 1, idempotencyKey: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_unknown_ticket_returns_400()
    {
        var response = await api.PostBookingAsync(999, 1, Guid.NewGuid().ToString());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_unknown_payment_method_returns_400()
    {
        var order = await BookPremiumAsync();

        var response = await api.PostCheckoutAsync(order, "Bitcoin");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_unknown_order_returns_404()
    {
        var response = await api.PostCheckoutAsync(Guid.NewGuid(), "QR");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().NotContain("   at ");
    }

    [Fact]
    public async Task Sold_out_errors_are_readable_by_the_frontend_origin()
    {
        await api.SetStockAsync(ApiFixture.GoldTicketId, sold: 100);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/bookings")
        {
            Content = new StringContent("""{"ticketId":1,"quantity":1}""", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        request.Headers.Add("Origin", FrontendOrigin);

        var response = await api.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle(FrontendOrigin);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/api/tickets")]
    [InlineData("/api/ledger/balance")]
    public async Task Public_endpoints_respond(string url)
    {
        var response = await api.Client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> BookPremiumAsync()
    {
        await api.SetStockAsync(ApiFixture.PremiumTicketId);

        return (await api.BookAsync(ApiFixture.PremiumTicketId)).Id;
    }
}
