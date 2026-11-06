namespace ETicketing.Application.Abstractions;

/// <summary>
/// Single source of the current time, so business rules stay testable.
/// </summary>
public interface IClock
{
    /// <summary>
    /// The current time in UTC.
    /// </summary>
    DateTime UtcNow { get; }
}
