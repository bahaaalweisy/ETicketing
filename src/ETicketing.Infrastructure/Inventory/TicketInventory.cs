using ETicketing.Application.Abstractions;
using ETicketing.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ETicketing.Infrastructure.Inventory;

public sealed class TicketInventory(IAppDbContext db, IClock clock) : ITicketInventory
{
    public async Task ReserveAsync(int ticketId, int quantity, CancellationToken cancellationToken)
    {
        var utcNow = clock.UtcNow;

        var affectedRows = await db.Tickets
            .Where(ticket => ticket.Id == ticketId
                && ticket.IsActive
                && ticket.Sold + ticket.Reserved + quantity <= ticket.Quota)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(ticket => ticket.Reserved, ticket => ticket.Reserved + quantity)
                    .SetProperty(ticket => ticket.UpdatedAt, utcNow),
                cancellationToken);

        if (affectedRows == 0)
        {
            throw new TicketSoldOutException(ticketId, quantity);
        }
    }

    public async Task ConfirmAsync(int ticketId, int quantity, CancellationToken cancellationToken)
    {
        var utcNow = clock.UtcNow;

        var affectedRows = await db.Tickets
            .Where(ticket => ticket.Id == ticketId && ticket.Reserved >= quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(ticket => ticket.Reserved, ticket => ticket.Reserved - quantity)
                    .SetProperty(ticket => ticket.Sold, ticket => ticket.Sold + quantity)
                    .SetProperty(ticket => ticket.UpdatedAt, utcNow),
                cancellationToken);

        EnsureReservationExisted(affectedRows, ticketId, quantity);
    }

    public async Task ReleaseAsync(int ticketId, int quantity, CancellationToken cancellationToken)
    {
        var utcNow = clock.UtcNow;

        var affectedRows = await db.Tickets
            .Where(ticket => ticket.Id == ticketId && ticket.Reserved >= quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(ticket => ticket.Reserved, ticket => ticket.Reserved - quantity)
                    .SetProperty(ticket => ticket.UpdatedAt, utcNow),
                cancellationToken);

        EnsureReservationExisted(affectedRows, ticketId, quantity);
    }

    private static void EnsureReservationExisted(int affectedRows, int ticketId, int quantity)
    {
        if (affectedRows == 0)
        {
            throw new InvalidOperationException($"Ticket {ticketId} has no reservation of {quantity} to settle.");
        }
    }
}
