namespace ETicketing.Application.Bookings;

/// <summary>
/// Body of POST /api/bookings.
/// </summary>
public sealed record CreateBookingRequest(int TicketId, int Quantity);
