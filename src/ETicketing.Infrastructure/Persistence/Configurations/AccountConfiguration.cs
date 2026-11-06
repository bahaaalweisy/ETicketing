using ETicketing.Domain.Constants;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ETicketing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps Accounts and seeds the two accounts of the double-entry ledger.
/// </summary>
public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(account => account.Id);
        builder.Property(account => account.Code).HasMaxLength(50).IsRequired();
        builder.Property(account => account.Name).HasMaxLength(100).IsRequired();
        builder.Property(account => account.Type).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(account => account.Code).IsUnique();

        builder.HasData(
            Seed(1, AccountCodes.CashGateway, "Cash Gateway", AccountType.Asset),
            Seed(2, AccountCodes.TicketSalesRevenue, "Ticket Sales Revenue", AccountType.Revenue));
    }

    private static object Seed(int id, string code, string name, AccountType type) => new
    {
        Id = id,
        Code = code,
        Name = name,
        Type = type,
        CreatedAt = SeedData.Timestamp,
        UpdatedAt = SeedData.Timestamp
    };
}
