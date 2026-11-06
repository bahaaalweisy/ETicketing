namespace ETicketing.Application.Abstractions;

/// <summary>
/// Moves ticket stock between Available, Reserved and Sold. Every method is one atomic
/// conditional SQL UPDATE, so the database itself prevents overselling.
/// </summary>
public interface ITicketInventory
{
    /// <summary>
    /// Holds stock for an unpaid order.
    /// </summary>
    /// <exception cref="ETicketing.Domain.Exceptions.TicketSoldOutException">Not enough stock is available.</exception>
    Task ReserveAsync(int ticketId, int quantity, CancellationToken cancellationToken);

    /// <summary>
    /// Turns reserved stock into sold stock when a payment completes.
    /// </summary>
    Task ConfirmAsync(int ticketId, int quantity, CancellationToken cancellationToken);

    /// <summary>
    /// Returns reserved stock to the pool when a payment fails or a reservation expires.
    /// </summary>
    Task ReleaseAsync(int ticketId, int quantity, CancellationToken cancellationToken);
}
