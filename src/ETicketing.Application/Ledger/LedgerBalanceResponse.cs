namespace ETicketing.Application.Ledger;

/// <summary>
/// Whole-ledger totals. TotalDebits and TotalCredits must always be equal.
/// </summary>
public sealed record LedgerBalanceResponse(
    decimal TotalDebits,
    decimal TotalCredits,
    IReadOnlyList<AccountBalanceResponse> Accounts);
