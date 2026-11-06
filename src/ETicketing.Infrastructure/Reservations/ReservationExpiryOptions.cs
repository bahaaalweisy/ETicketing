namespace ETicketing.Infrastructure.Reservations;

/// <summary>
/// Settings for <see cref="ReservationExpiryService"/>, bound from the ReservationExpiry section.
/// </summary>
public sealed class ReservationExpiryOptions
{
    public const string SectionName = "ReservationExpiry";

    /// <summary>
    /// How long a Pending transaction may protect an overdue order before it counts as stuck.
    /// It must stay well above the 8-second QR confirmation.
    /// </summary>
    public TimeSpan PendingPaymentTimeout { get; set; } = TimeSpan.FromMinutes(2);
}
