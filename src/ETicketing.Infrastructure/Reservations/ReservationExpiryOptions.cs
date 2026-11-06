namespace ETicketing.Infrastructure.Reservations;

public sealed class ReservationExpiryOptions
{
    public const string SectionName = "ReservationExpiry";

    public TimeSpan PendingPaymentTimeout { get; set; } = TimeSpan.FromMinutes(2);
}
