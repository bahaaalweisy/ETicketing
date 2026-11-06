using ETicketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ETicketing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps Transactions. The filtered unique index lets the database guarantee that an order
/// has at most one live payment, so two checkouts can never charge the same order twice.
/// </summary>
public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Method).HasConversion<string>().HasMaxLength(20);
        builder.Property(transaction => transaction.Status).HasConversion<string>().HasMaxLength(20).IsConcurrencyToken();
        builder.Property(transaction => transaction.Amount).HasPrecision(18, 2);
        builder.Property(transaction => transaction.Reference).HasMaxLength(100).IsRequired();
        builder.HasIndex(transaction => transaction.Status);
        builder.HasIndex(transaction => transaction.OrderId)
            .IsUnique()
            .HasFilter("[Status] IN ('Pending', 'Completed')")
            .HasDatabaseName("UX_Transactions_OrderId_Live");

        builder.HasOne(transaction => transaction.Order)
            .WithMany()
            .HasForeignKey(transaction => transaction.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
