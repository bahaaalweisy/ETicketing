namespace ETicketing.Domain.Enums;

/// <summary>
/// Lifecycle of an order. PendingPayment is the only state that can still change.
/// </summary>
public enum OrderStatus
{
    PendingPayment = 1,
    Paid = 2,
    Failed = 3,
    Expired = 4
}
