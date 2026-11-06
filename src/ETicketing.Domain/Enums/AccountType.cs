namespace ETicketing.Domain.Enums;

/// <summary>
/// Accounting type of a ledger account. It decides on which side the balance grows.
/// </summary>
public enum AccountType
{
    Asset = 1,
    Revenue = 2
}
