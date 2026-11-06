using ETicketing.Domain.Common;
using ETicketing.Domain.Enums;

namespace ETicketing.Domain.Entities;

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
