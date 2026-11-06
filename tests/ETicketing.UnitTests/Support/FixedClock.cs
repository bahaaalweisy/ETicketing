using ETicketing.Application.Abstractions;

namespace ETicketing.UnitTests.Support;

public sealed class FixedClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
}
