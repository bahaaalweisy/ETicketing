using ETicketing.Domain.Enums;

namespace ETicketing.Application.Ledger;

/// <summary>
/// Totals of one account. Balance grows with debits on assets and with credits on revenue.
/// </summary>
public sealed record AccountBalanceResponse(
    string Code,
    string Name,
    AccountType Type,
    decimal Debits,
    decimal Credits,
    decimal Balance);
