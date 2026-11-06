using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;

namespace ETicketing.Application.Orders;

public sealed record OrderResponse(
    Guid Id,
    int TicketId,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    OrderStatus Status,
    DateTime ReservationExpiresAt,
    DateTime CreatedAt)
{
    public static OrderResponse From(Order order) => new(
        order.Id,
        order.TicketId,
        order.Quantity,
        order.UnitPrice,
        order.TotalAmount,
        order.Status,
        order.ReservationExpiresAt,
        order.CreatedAt);
}
