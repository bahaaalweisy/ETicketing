namespace ETicketing.Domain.Exceptions;

public sealed class TicketSoldOutException(int ticketId, int quantity)
    : DomainException($"Ticket {ticketId} does not have {quantity} seat(s) available.");
