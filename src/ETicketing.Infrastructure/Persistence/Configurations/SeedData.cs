namespace ETicketing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fixed values for seeded rows. A constant timestamp keeps migrations deterministic.
/// </summary>
internal static class SeedData
{
    public static readonly DateTime Timestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
