using ETicketing.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ETicketing.Application.Tickets;

public sealed class TicketQueryService(IAppDbContext db)
{
    public async Task<IReadOnlyList<TicketResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var tickets = await db.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.IsActive)
            .OrderBy(ticket => ticket.Price)
            .ToListAsync(cancellationToken);

        return tickets.Select(TicketResponse.From).ToList();
    }
}
