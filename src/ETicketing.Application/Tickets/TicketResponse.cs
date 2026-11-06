using ETicketing.Domain.Entities;

namespace ETicketing.Application.Tickets;

/// <summary>
/// A ticket type with its current stock numbers.
/// </summary>
public sealed record TicketResponse(
    int Id,
    string Code,
    string Name,
    decimal Price,
    string Currency,
    int Quota,
    int Sold,
    int Reserved,
    int Available)
{
    public static TicketResponse From(Ticket ticket) => new(
        ticket.Id,
        ticket.Code,
        ticket.Name,
        ticket.Price,
        ticket.Currency,
        ticket.Quota,
        ticket.Sold,
        ticket.Reserved,
        ticket.Available);
}
