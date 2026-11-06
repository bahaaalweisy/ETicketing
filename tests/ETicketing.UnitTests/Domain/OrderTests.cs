using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;
using ETicketing.Domain.Exceptions;
using FluentAssertions;

namespace ETicketing.UnitTests.Domain;

public class OrderTests
{
    private static readonly DateTime UtcNow = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(100, 1, 100)]
    [InlineData(200, 3, 600)]
    [InlineData(500, 10, 5000)]
    public void Create_calculates_the_total_from_price_and_quantity(decimal price, int quantity, decimal expectedTotal)
    {
        var order = CreateOrder(price, quantity);

        order.UnitPrice.Should().Be(price);
        order.TotalAmount.Should().Be(expectedTotal);
    }

    [Fact]
    public void Create_reserves_for_ten_minutes_and_awaits_payment()
    {
        var order = CreateOrder();

        order.Status.Should().Be(OrderStatus.PendingPayment);
        order.ReservationExpiresAt.Should().Be(UtcNow.AddMinutes(10));
    }

    [Fact]
    public void EnsureCanCheckout_accepts_an_unpaid_order_within_its_reservation()
    {
        var order = CreateOrder();

        var act = () => order.EnsureCanCheckout(UtcNow.AddMinutes(9));

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanCheckout_rejects_an_order_past_its_reservation()
    {
        var order = CreateOrder();

        var act = () => order.EnsureCanCheckout(UtcNow.AddMinutes(10));

        act.Should().Throw<InvalidOrderStateException>();
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Failed)]
    [InlineData(OrderStatus.Expired)]
    public void EnsureCanCheckout_rejects_a_finished_order(OrderStatus status)
    {
        var order = CreateOrder();
        MoveTo(order, status);

        var act = () => order.EnsureCanCheckout(UtcNow);

        act.Should().Throw<InvalidOrderStateException>();
    }

    [Fact]
    public void A_finished_order_cannot_change_status_again()
    {
        var order = CreateOrder();
        order.MarkExpired();

        var act = order.MarkPaid;

        act.Should().Throw<InvalidOrderStateException>();
    }

    private static Order CreateOrder(decimal price = 100m, int quantity = 1) =>
        Order.Create(new Ticket("GOLD", "Gold", price, "AED", 100), quantity, "key", UtcNow);

    private static void MoveTo(Order order, OrderStatus status)
    {
        switch (status)
        {
            case OrderStatus.Paid:
                order.MarkPaid();
                break;
            case OrderStatus.Failed:
                order.MarkFailed();
                break;
            default:
                order.MarkExpired();
                break;
        }
    }
}
