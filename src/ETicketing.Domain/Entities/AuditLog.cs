using ETicketing.Domain.Common;

namespace ETicketing.Domain.Entities;

/// <summary>
/// Records one status change of an Order or a Transaction. FromStatus is null on creation.
/// </summary>
public class AuditLog : AuditableEntity
{
    private AuditLog()
    {
    }

    public AuditLog(string entityName, string entityId, string? fromStatus, string toStatus)
    {
        EntityName = entityName;
        EntityId = entityId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
    }

    public long Id { get; private set; }

    public string EntityName { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string? FromStatus { get; private set; }

    public string ToStatus { get; private set; } = string.Empty;
}
