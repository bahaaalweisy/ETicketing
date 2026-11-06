using ETicketing.Domain.Common;

namespace ETicketing.Domain.Entities;

/// <summary>
/// A ticket type and its stock. Reserved and Sold only change through atomic SQL updates,
/// so they have no setters for business code to use.
/// </summary>
public class Ticket : AuditableEntity
{
    private Ticket()
    {
    }

    public Ticket(string code, string name, decimal price, string currency, int quota)
    {
        Code = code;
        Name = name;
        Price = price;
        Currency = currency;
        Quota = quota;
        IsActive = true;
    }

    public int Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public int Quota { get; private set; }

    public int Reserved { get; private set; }

    public int Sold { get; private set; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public int Available => Quota - Reserved - Sold;
}
