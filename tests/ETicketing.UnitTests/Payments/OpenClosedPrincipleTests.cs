using ETicketing.Application;
using ETicketing.Application.Abstractions;
using ETicketing.Application.Payments;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;
using ETicketing.Infrastructure.Persistence;
using ETicketing.UnitTests.Support;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ETicketing.UnitTests.Payments;

/// <summary>
/// Proves the Open/Closed claim: a new payment method plugs in through one DI registration,
/// with no change to CheckoutService, the resolver or the existing handlers.
/// </summary>
public class OpenClosedPrincipleTests
{
    private const PaymentMethod ApplePay = (PaymentMethod)99;

    private readonly AppDbContext _db = TestDatabase.Create();

    [Fact]
    public void A_new_handler_is_resolved_from_a_single_registration()
    {
        using var provider = BuildProvider();

        var resolver = provider.GetRequiredService<IPaymentHandlerResolver>();

        resolver.Resolve(ApplePay).Should().BeOfType<FakeApplePayHandler>();
    }

    [Fact]
    public void Existing_handlers_keep_working_next_to_the_new_one()
    {
        using var provider = BuildProvider();

        var resolver = provider.GetRequiredService<IPaymentHandlerResolver>();

        resolver.Resolve(PaymentMethod.CreditCard).Method.Should().Be(PaymentMethod.CreditCard);
        resolver.Resolve(PaymentMethod.QR).Method.Should().Be(PaymentMethod.QR);
    }

    [Fact]
    public async Task CheckoutService_runs_the_new_handler_unchanged()
    {
        using var provider = BuildProvider();
        var ticket = new Ticket("GOLD2", "Gold", 100m, "AED", 10);
        var clock = new FixedClock();
        var order = Order.Create(ticket, 1, "key", clock.UtcNow);
        _db.Tickets.Add(ticket);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // The real validator would accept a real enum value; the fake value here needs a pass-through.
        var checkoutService = new CheckoutService(
            _db,
            provider.GetRequiredService<IPaymentHandlerResolver>(),
            clock,
            new InlineValidator<CheckoutRequest>());

        var response = await checkoutService.CheckoutAsync(new CheckoutRequest(order.Id, ApplePay), CancellationToken.None);

        response.Status.Should().Be(TransactionStatus.Completed);
        response.Reference.Should().Be(FakeApplePayHandler.Reference);
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IAppDbContext>(_db);
        services.AddSingleton<IClock>(new FixedClock());
        services.AddSingleton(Mock.Of<ITicketInventory>());
        services.AddSingleton(Mock.Of<IPaymentQueue>());

        services.AddScoped<IPaymentHandler, FakeApplePayHandler>();

        return services.BuildServiceProvider();
    }

    private sealed class FakeApplePayHandler : IPaymentHandler
    {
        public const string Reference = "APPLE-PAY-1";

        public PaymentMethod Method => ApplePay;

        public Task<PaymentResult> ProcessAsync(PaymentContext context, CancellationToken cancellationToken) =>
            Task.FromResult(new PaymentResult(TransactionStatus.Completed, Guid.NewGuid(), Reference, "Paid."));
    }
}
