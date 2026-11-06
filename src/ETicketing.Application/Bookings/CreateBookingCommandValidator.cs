using ETicketing.Application.Abstractions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ETicketing.Application.Bookings;

/// <summary>
/// Validates a booking: quantity range, a present key, and an existing active ticket.
/// </summary>
public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public const int MinQuantity = 1;
    public const int MaxQuantity = 10;
    public const int MaxIdempotencyKeyLength = 100;

    public CreateBookingCommandValidator(IAppDbContext db)
    {
        RuleFor(command => command.Quantity).InclusiveBetween(MinQuantity, MaxQuantity);

        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .WithMessage("The Idempotency-Key header is required.")
            .MaximumLength(MaxIdempotencyKeyLength);

        RuleFor(command => command.TicketId)
            .MustAsync((ticketId, cancellationToken) =>
                db.Tickets.AnyAsync(ticket => ticket.Id == ticketId && ticket.IsActive, cancellationToken))
            .WithMessage("The ticket does not exist or is not on sale.");
    }
}
