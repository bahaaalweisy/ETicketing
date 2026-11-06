using ETicketing.Application.Abstractions;

namespace ETicketing.Infrastructure.Time;

/// <summary>
/// Production clock backed by the system time.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
