using ETicketing.Application.Abstractions;
using ETicketing.Application.Orders;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ETicketing.Application.Bookings;

/// <summary>
/// Creates an order and reserves its stock in one database transaction.
/// </summary>
public sealed class BookingService(
    IAppDbContext db,
    ITicketInventory inventory,
    IClock clock,
    IValidator<CreateBookingCommand> validator)
{
    /// <summary>
    /// Books tickets. The same idempotency key always returns the same order. The order is
    /// inserted before the stock is reserved so that a concurrent duplicate waits on the unique
    /// index and receives the original order, instead of a false sold-out on the last seat.
    /// </summary>
    public async Task<OrderResponse> CreateAsync(CreateBookingCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var existingOrder = await FindByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
        if (existingOrder is not null)
        {
            return OrderResponse.From(existingOrder);
        }

        var ticket = await db.Tickets.AsNoTracking().SingleAsync(ticket => ticket.Id == command.TicketId, cancellationToken);
        var order = Order.Create(ticket, command.Quantity, command.IdempotencyKey, clock.UtcNow);

        try
        {
            await using var dbTransaction = await db.BeginTransactionAsync(cancellationToken);

            db.Orders.Add(order);
            await db.SaveChangesAsync(cancellationToken);
            await inventory.ReserveAsync(order.TicketId, order.Quantity, cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);
        }
        catch (DuplicateEntryException)
        {
            db.ChangeTracker.Clear();

            var winningOrder = await FindByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
            return OrderResponse.From(winningOrder!);
        }

        return OrderResponse.From(order);
    }

    private Task<Order?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        db.Orders.AsNoTracking().SingleOrDefaultAsync(order => order.IdempotencyKey == idempotencyKey, cancellationToken);
}
