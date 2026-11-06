using ETicketing.Application.Bookings;
using ETicketing.UnitTests.Support;
using FluentAssertions;

namespace ETicketing.UnitTests.Validators;

public class CreateBookingCommandValidatorTests
{
    private readonly CreateBookingCommandValidator _validator = new(TestDatabase.Create());

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public async Task Accepts_quantities_within_the_limits(int quantity)
    {
        var result = await _validator.ValidateAsync(new CreateBookingCommand(TestDatabase.GoldTicketId, quantity, "key"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task Rejects_quantities_outside_the_limits(int quantity)
    {
        var result = await _validator.ValidateAsync(new CreateBookingCommand(TestDatabase.GoldTicketId, quantity, "key"));

        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CreateBookingCommand.Quantity));
    }

    [Fact]
    public async Task Rejects_a_ticket_that_does_not_exist()
    {
        var result = await _validator.ValidateAsync(new CreateBookingCommand(999, 1, "key"));

        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CreateBookingCommand.TicketId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Rejects_a_missing_idempotency_key(string idempotencyKey)
    {
        var result = await _validator.ValidateAsync(new CreateBookingCommand(TestDatabase.GoldTicketId, 1, idempotencyKey));

        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CreateBookingCommand.IdempotencyKey));
    }
}
