using ETicketing.Domain.Common;
using ETicketing.Domain.Enums;
using ETicketing.Domain.Exceptions;

namespace ETicketing.Domain.Entities;

/// <summary>
/// A customer booking. It owns the price calculation and the order state machine.
/// </summary>
public class Order : AuditableEntity
{
    public static readonly TimeSpan ReservationWindow = TimeSpan.FromMinutes(10);

    private Order()
    {
    }

    public Guid Id { get; private set; }

    public int TicketId { get; private set; }

    public Ticket Ticket { get; private set; } = null!;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal TotalAmount { get; private set; }

    public OrderStatus Status { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public DateTime ReservationExpiresAt { get; private set; }

    /// <summary>
    /// Creates an unpaid order at the current ticket price, reserved for <see cref="ReservationWindow"/>.
    /// </summary>
    public static Order Create(Ticket ticket, int quantity, string idempotencyKey, DateTime utcNow) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticket.Id,
        Quantity = quantity,
        UnitPrice = ticket.Price,
        TotalAmount = ticket.Price * quantity,
        Status = OrderStatus.PendingPayment,
        IdempotencyKey = idempotencyKey,
        ReservationExpiresAt = utcNow.Add(ReservationWindow)
    };

    /// <summary>
    /// Rejects the checkout unless the order is unpaid and its reservation is still valid.
    /// </summary>
    public void EnsureCanCheckout(DateTime utcNow)
    {
        EnsurePendingPayment();

        if (utcNow >= ReservationExpiresAt)
        {
            throw new InvalidOrderStateException($"The reservation of order {Id} has expired.");
        }
    }

    public void MarkPaid() => MoveTo(OrderStatus.Paid);

    public void MarkFailed() => MoveTo(OrderStatus.Failed);

    public void MarkExpired() => MoveTo(OrderStatus.Expired);

    private void MoveTo(OrderStatus status)
    {
        EnsurePendingPayment();
        Status = status;
    }

    private void EnsurePendingPayment()
    {
        if (Status != OrderStatus.PendingPayment)
        {
            throw new InvalidOrderStateException($"Order {Id} is {Status} and can no longer be changed.");
        }
    }
}
