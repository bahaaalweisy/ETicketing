namespace ETicketing.Domain.Enums;

/// <summary>
/// Lifecycle of a payment transaction. Pending is the only state that can still change.
/// </summary>
public enum TransactionStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}
