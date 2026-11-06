namespace ETicketing.Application.Bookings;

public sealed record CreateBookingRequest(int TicketId, int Quantity);
