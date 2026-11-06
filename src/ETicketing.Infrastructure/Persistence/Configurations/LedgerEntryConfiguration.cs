using ETicketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ETicketing.Infrastructure.Persistence.Configurations;

public sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("LedgerEntries", table =>
            table.HasCheckConstraint(
                "CK_LedgerEntries_OneSide",
                "([Debit] > 0 AND [Credit] = 0) OR ([Credit] > 0 AND [Debit] = 0)"));

        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Debit).HasPrecision(18, 2);
        builder.Property(entry => entry.Credit).HasPrecision(18, 2);

        builder.HasOne(entry => entry.Transaction)
            .WithMany()
            .HasForeignKey(entry => entry.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entry => entry.Account)
            .WithMany()
            .HasForeignKey(entry => entry.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
