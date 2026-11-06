using ETicketing.Application.Abstractions;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ETicketing.Application.Transactions;

/// <summary>
/// Reads a single payment transaction.
/// </summary>
public sealed class TransactionQueryService(IAppDbContext db)
{
    public async Task<TransactionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await db.Transactions
            .AsNoTracking()
            .SingleOrDefaultAsync(transaction => transaction.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), id);

        return TransactionResponse.From(transaction);
    }
}
