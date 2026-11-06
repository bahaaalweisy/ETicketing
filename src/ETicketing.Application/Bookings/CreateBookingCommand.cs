namespace ETicketing.Application.Bookings;

public sealed record CreateBookingCommand(int TicketId, int Quantity, string IdempotencyKey);
