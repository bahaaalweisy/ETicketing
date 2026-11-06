using ETicketing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ETicketing.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
            table.HasCheckConstraint("CK_Orders_Quantity", "[Quantity] > 0"));

        builder.HasKey(order => order.Id);
        builder.Property(order => order.UnitPrice).HasPrecision(18, 2);
        builder.Property(order => order.TotalAmount).HasPrecision(18, 2);
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(20).IsConcurrencyToken();
        builder.Property(order => order.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.HasIndex(order => order.IdempotencyKey).IsUnique();
        builder.HasIndex(order => new { order.Status, order.ReservationExpiresAt });

        builder.HasOne(order => order.Ticket)
            .WithMany()
            .HasForeignKey(order => order.TicketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
