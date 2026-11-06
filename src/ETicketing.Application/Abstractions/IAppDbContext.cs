using ETicketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace ETicketing.Application.Abstractions;

/// <summary>
/// The persistence surface available to the use cases. It replaces a repository layer:
/// the Application layer queries the sets directly and never sees the concrete context.
/// </summary>
public interface IAppDbContext
{
    DbSet<Ticket> Tickets { get; }

    DbSet<Order> Orders { get; }

    DbSet<Transaction> Transactions { get; }

    DbSet<Account> Accounts { get; }

    DbSet<LedgerEntry> LedgerEntries { get; }

    DbSet<AuditLog> AuditLogs { get; }

    /// <summary>
    /// Tracks the loaded entities. Use cases clear it to re-read fresh state after a write conflict.
    /// </summary>
    ChangeTracker ChangeTracker { get; }

    /// <summary>
    /// Persists the tracked changes.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a database transaction so several writes commit or roll back together.
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}
