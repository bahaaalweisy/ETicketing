using ETicketing.Application.Abstractions;
using ETicketing.Application.Bookings;
using ETicketing.Domain.Enums;
using ETicketing.Domain.Exceptions;
using ETicketing.Infrastructure.Persistence;
using ETicketing.UnitTests.Support;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ETicketing.UnitTests.Bookings;

public class BookingServiceTests
{
    private readonly AppDbContext _db = TestDatabase.Create();
    private readonly Mock<ITicketInventory> _inventory = new();

    [Fact]
    public async Task Booking_calculates_the_total_and_reserves_the_quantity()
    {
        var order = await CreateService().CreateAsync(Command(quantity: 3), CancellationToken.None);

        order.TotalAmount.Should().Be(300m);
        order.Status.Should().Be(OrderStatus.PendingPayment);
        _inventory.Verify(inventory => inventory.ReserveAsync(TestDatabase.GoldTicketId, 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task The_same_key_returns_the_same_order_and_reserves_once()
    {
        var service = CreateService();

        var first = await service.CreateAsync(Command(), CancellationToken.None);
        var second = await service.CreateAsync(Command(), CancellationToken.None);

        second.Id.Should().Be(first.Id);
        (await _db.Orders.CountAsync()).Should().Be(1);
        _inventory.Verify(inventory => inventory.ReserveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sold_out_stock_surfaces_as_a_sold_out_failure()
    {
        _inventory
            .Setup(inventory => inventory.ReserveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TicketSoldOutException(TestDatabase.GoldTicketId, 1));

        var act = () => CreateService().CreateAsync(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<TicketSoldOutException>();
    }

    [Fact]
    public async Task An_invalid_booking_never_touches_the_stock()
    {
        var act = () => CreateService().CreateAsync(Command(quantity: 11), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _inventory.VerifyNoOtherCalls();
    }

    private BookingService CreateService() =>
        new(_db, _inventory.Object, new FixedClock(), new CreateBookingCommandValidator(_db));

    private static CreateBookingCommand Command(int quantity = 1) =>
        new(TestDatabase.GoldTicketId, quantity, "key-1");
}
