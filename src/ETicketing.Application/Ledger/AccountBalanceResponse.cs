using ETicketing.Domain.Enums;

namespace ETicketing.Application.Ledger;

public sealed record AccountBalanceResponse(
    string Code,
    string Name,
    AccountType Type,
    decimal Debits,
    decimal Credits,
    decimal Balance);
