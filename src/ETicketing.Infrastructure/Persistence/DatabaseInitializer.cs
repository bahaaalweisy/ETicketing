using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ETicketing.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    private const int MaxAttempts = 12;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    public static async Task InitialiseDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Database is ready");
                return;
            }
            catch (SqlException exception) when (attempt < MaxAttempts)
            {
                logger.LogWarning(exception, "Database not ready, attempt {Attempt} of {MaxAttempts}", attempt, MaxAttempts);
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }
}
