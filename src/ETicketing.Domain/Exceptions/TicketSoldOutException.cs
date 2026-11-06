namespace ETicketing.Domain.Exceptions;

/// <summary>
/// The atomic reservation affected no rows: there is not enough stock left.
/// </summary>
public sealed class TicketSoldOutException(int ticketId, int quantity)
    : DomainException($"Ticket {ticketId} does not have {quantity} seat(s) available.");
