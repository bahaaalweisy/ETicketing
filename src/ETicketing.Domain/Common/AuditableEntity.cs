namespace ETicketing.Domain.Common;

/// <summary>
/// Base class for every table. The timestamps are stamped by the persistence layer in UTC.
/// </summary>
public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }
}
