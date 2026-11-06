using ETicketing.Application.Abstractions;

namespace ETicketing.UnitTests.Support;

/// <summary>
/// A clock that returns a set time, so time-based rules are deterministic.
/// </summary>
public sealed class FixedClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
}
