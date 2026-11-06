namespace ETicketing.Application.Abstractions;

public interface ITicketInventory
{
    Task ReserveAsync(int ticketId, int quantity, CancellationToken cancellationToken);

    Task ConfirmAsync(int ticketId, int quantity, CancellationToken cancellationToken);

    Task ReleaseAsync(int ticketId, int quantity, CancellationToken cancellationToken);
}
