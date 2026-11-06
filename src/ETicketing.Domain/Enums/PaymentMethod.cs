namespace ETicketing.Domain.Enums;

/// <summary>
/// Supported payment methods. Values start at 1 so that a request omitting the field
/// fails validation instead of silently defaulting to the first method.
/// </summary>
public enum PaymentMethod
{
    CreditCard = 1,
    QR = 2
}
