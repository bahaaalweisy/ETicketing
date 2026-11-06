using ETicketing.Domain.Common;
using ETicketing.Domain.Enums;

namespace ETicketing.Domain.Entities;

/// <summary>
/// A ledger account. Accounts are seeded reference data.
/// </summary>
public class Account : AuditableEntity
{
    private Account()
    {
    }

    public int Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public AccountType Type { get; private set; }
}
