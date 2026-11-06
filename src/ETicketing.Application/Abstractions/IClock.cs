namespace ETicketing.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
