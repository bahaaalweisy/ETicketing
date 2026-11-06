using ETicketing.Application.Abstractions;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ETicketing.Application.Orders;

/// <summary>
/// Reads a single order.
/// </summary>
public sealed class OrderQueryService(IAppDbContext db)
{
    public async Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(order => order.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), id);

        return OrderResponse.From(order);
    }
}
