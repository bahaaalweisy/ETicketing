using ETicketing.Domain.Constants;
using ETicketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ETicketing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps Tickets. The CHECK constraints are the last line of defence against overselling.
/// </summary>
public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    private const string Aed = "AED";

    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets", table =>
        {
            table.HasCheckConstraint("CK_Tickets_Price", "[Price] > 0");
            table.HasCheckConstraint("CK_Tickets_Quota", "[Quota] >= 0");
            table.HasCheckConstraint("CK_Tickets_Reserved", "[Reserved] >= 0");
            table.HasCheckConstraint("CK_Tickets_Sold", "[Sold] >= 0");
            table.HasCheckConstraint("CK_Tickets_Stock", "[Sold] + [Reserved] <= [Quota]");
        });

        builder.HasKey(ticket => ticket.Id);
        builder.Property(ticket => ticket.Code).HasMaxLength(20).IsRequired();
        builder.Property(ticket => ticket.Name).HasMaxLength(100).IsRequired();
        builder.Property(ticket => ticket.Price).HasPrecision(18, 2);
        builder.Property(ticket => ticket.Currency).HasMaxLength(3).IsRequired();
        builder.Property(ticket => ticket.RowVersion).IsRowVersion();
        builder.Ignore(ticket => ticket.Available);
        builder.HasIndex(ticket => ticket.Code).IsUnique();

        builder.HasData(
            Seed(1, TicketCodes.Gold, "Gold", 100m, 100),
            Seed(2, TicketCodes.Premium, "Premium", 200m, 100),
            Seed(3, TicketCodes.Vip, "VIP", 500m, 50));
    }

    private static object Seed(int id, string code, string name, decimal price, int quota) => new
    {
        Id = id,
        Code = code,
        Name = name,
        Price = price,
        Currency = Aed,
        Quota = quota,
        Reserved = 0,
        Sold = 0,
        IsActive = true,
        CreatedAt = SeedData.Timestamp,
        UpdatedAt = SeedData.Timestamp
    };
}
