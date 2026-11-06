using ETicketing.Application.Abstractions;
using ETicketing.Domain.Common;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace ETicketing.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IClock clock)
    : DbContext(options), IAppDbContext
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        Database.BeginTransactionAsync(cancellationToken);

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        RejectForbiddenChanges();
        WriteAuditLogs();
        StampTimestamps();

        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException
        {
            Number: UniqueIndexViolation or UniqueConstraintViolation
        })
        {
            throw new DuplicateEntryException(exception);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();

    private void RejectForbiddenChanges()
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                throw new InvalidOperationException($"{entry.Metadata.ClrType.Name} rows are never deleted.");
            }

            if (entry.State == EntityState.Modified && entry.Entity is LedgerEntry or AuditLog)
            {
                throw new InvalidOperationException($"{entry.Metadata.ClrType.Name} rows are insert-only.");
            }
        }
    }

    private void WriteAuditLogs()
    {
        var statusChanges = ChangeTracker.Entries()
            .Where(entry => entry.Entity is Order or Transaction)
            .Select(ToAuditLog)
            .OfType<AuditLog>()
            .ToList();

        AuditLogs.AddRange(statusChanges);
    }

    private static AuditLog? ToAuditLog(EntityEntry entry)
    {
        var status = entry.Property(nameof(Order.Status));
        var isNew = entry.State == EntityState.Added;

        if (!isNew && !status.IsModified)
        {
            return null;
        }

        return new AuditLog(
            entry.Metadata.ClrType.Name,
            entry.Property(nameof(Order.Id)).CurrentValue!.ToString()!,
            isNew ? null : status.OriginalValue!.ToString(),
            status.CurrentValue!.ToString()!);
    }

    private void StampTimestamps()
    {
        var utcNow = clock.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(entity => entity.CreatedAt).CurrentValue = utcNow;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Property(entity => entity.UpdatedAt).CurrentValue = utcNow;
            }
        }
    }
}
