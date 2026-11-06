using ETicketing.Application.Abstractions;
using ETicketing.Domain.Constants;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;
using ETicketing.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ETicketing.Application.Ledger;

public sealed class LedgerService(IAppDbContext db) : ILedgerService
{
    public async Task RecordTicketSaleAsync(Guid transactionId, decimal amount, CancellationToken cancellationToken)
    {
        var cashAccountId = await GetAccountIdAsync(AccountCodes.CashGateway, cancellationToken);
        var revenueAccountId = await GetAccountIdAsync(AccountCodes.TicketSalesRevenue, cancellationToken);

        db.LedgerEntries.AddRange(
            LedgerEntry.CreateDebit(transactionId, cashAccountId, amount),
            LedgerEntry.CreateCredit(transactionId, revenueAccountId, amount));

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureBalancedAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        var entries = db.LedgerEntries.Where(entry => entry.TransactionId == transactionId);
        var debits = await entries.SumAsync(entry => entry.Debit, cancellationToken);
        var credits = await entries.SumAsync(entry => entry.Credit, cancellationToken);

        if (debits != credits)
        {
            throw new UnbalancedLedgerException(transactionId, debits, credits);
        }
    }

    public async Task<LedgerBalanceResponse> GetBalanceAsync(CancellationToken cancellationToken)
    {
        var accounts = await db.Accounts.AsNoTracking().OrderBy(account => account.Id).ToListAsync(cancellationToken);

        var totals = await db.LedgerEntries
            .GroupBy(entry => entry.AccountId)
            .Select(group => new
            {
                AccountId = group.Key,
                Debits = group.Sum(entry => entry.Debit),
                Credits = group.Sum(entry => entry.Credit)
            })
            .ToDictionaryAsync(total => total.AccountId, cancellationToken);

        var balances = accounts
            .Select(account =>
            {
                totals.TryGetValue(account.Id, out var total);
                var debits = total?.Debits ?? 0m;
                var credits = total?.Credits ?? 0m;
                var balance = account.Type == AccountType.Asset ? debits - credits : credits - debits;

                return new AccountBalanceResponse(account.Code, account.Name, account.Type, debits, credits, balance);
            })
            .ToList();

        return new LedgerBalanceResponse(balances.Sum(item => item.Debits), balances.Sum(item => item.Credits), balances);
    }

    private Task<int> GetAccountIdAsync(string code, CancellationToken cancellationToken) =>
        db.Accounts.Where(account => account.Code == code).Select(account => account.Id).SingleAsync(cancellationToken);
}
