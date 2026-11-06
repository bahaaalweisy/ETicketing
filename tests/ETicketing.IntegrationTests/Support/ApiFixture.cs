using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ETicketing.Application.Bookings;
using ETicketing.Application.Orders;
using ETicketing.Domain.Constants;
using ETicketing.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace ETicketing.IntegrationTests.Support;

/// <summary>
/// One real SQL Server container and one API host, shared by the whole test collection.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    public const int GoldTicketId = 1;
    public const int PremiumTicketId = 2;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public ApiFactory Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();
        Factory = CreateFactory();
        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _sqlServer.DisposeAsync();
    }

    /// <summary>
    /// A second host on the same database, which behaves like the API after a restart.
    /// </summary>
    public ApiFactory CreateFactory() => new(_sqlServer.GetConnectionString());

    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await action(db);
    }

    /// <summary>
    /// Puts a ticket into a known state, so every test is independent of the others.
    /// </summary>
    public Task SetStockAsync(int ticketId, int sold = 0, int reserved = 0) =>
        WithDbAsync(db => db.Tickets
            .Where(ticket => ticket.Id == ticketId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ticket => ticket.Sold, sold)
                .SetProperty(ticket => ticket.Reserved, reserved)));

    public async Task<OrderResponse> BookAsync(int ticketId, int quantity = 1)
    {
        var response = await PostBookingAsync(ticketId, quantity, Guid.NewGuid().ToString());
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions))!;
    }

    public Task<HttpResponseMessage> PostBookingAsync(int ticketId, int quantity, string? idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/bookings")
        {
            Content = JsonContent.Create(new CreateBookingRequest(ticketId, quantity))
        };

        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return Client.SendAsync(request);
    }

    public Task<HttpResponseMessage> PostCheckoutAsync(Guid orderId, string paymentMethod) =>
        Client.PostAsJsonAsync("/api/checkout", new { orderId, paymentMethod });

    public async Task<T> GetAsync<T>(string url) =>
        (await Client.GetFromJsonAsync<T>(url, JsonOptions))!;
}

/// <summary>
/// Tests in this collection run one at a time and share the fixture.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}
