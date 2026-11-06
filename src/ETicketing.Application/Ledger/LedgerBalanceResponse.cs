namespace ETicketing.Application.Ledger;

public sealed record LedgerBalanceResponse(
    decimal TotalDebits,
    decimal TotalCredits,
    IReadOnlyList<AccountBalanceResponse> Accounts);
