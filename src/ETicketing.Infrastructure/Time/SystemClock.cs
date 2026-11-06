using ETicketing.Application.Abstractions;

namespace ETicketing.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
