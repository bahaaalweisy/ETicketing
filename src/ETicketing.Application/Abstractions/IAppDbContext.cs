using ETicketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace ETicketing.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Ticket> Tickets { get; }

    DbSet<Order> Orders { get; }

    DbSet<Transaction> Transactions { get; }

    DbSet<Account> Accounts { get; }

    DbSet<LedgerEntry> LedgerEntries { get; }

    DbSet<AuditLog> AuditLogs { get; }

    ChangeTracker ChangeTracker { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}
