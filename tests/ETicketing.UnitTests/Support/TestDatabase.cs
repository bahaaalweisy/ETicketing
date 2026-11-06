using ETicketing.Application.Abstractions;
using ETicketing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ETicketing.UnitTests.Support;

public static class TestDatabase
{
    public const int GoldTicketId = 1;

    public static AppDbContext Create(IClock? clock = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), inMemory => inMemory.EnableNullChecks(false))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AppDbContext(options, clock ?? new FixedClock());
        db.Database.EnsureCreated();

        return db;
    }
}
