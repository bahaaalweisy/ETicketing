namespace ETicketing.Application.Bookings;

/// <summary>
/// The booking use-case input: the request body plus the Idempotency-Key header.
/// </summary>
public sealed record CreateBookingCommand(int TicketId, int Quantity, string IdempotencyKey);
