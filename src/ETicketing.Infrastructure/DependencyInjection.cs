using ETicketing.Application.Abstractions;
using ETicketing.Infrastructure.Inventory;
using ETicketing.Infrastructure.Payments;
using ETicketing.Infrastructure.Persistence;
using ETicketing.Infrastructure.Reservations;
using ETicketing.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ETicketing.Infrastructure;

/// <summary>
/// Registers the Infrastructure layer's implementations with the DI container.
/// </summary>
public static class DependencyInjection
{
    private const string ConnectionStringName = "DefaultConnection";

    /// <summary>
    /// Adds the implementations of the Application abstractions (persistence, queue, background services).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is missing.");

        services.AddSingleton<IClock, SystemClock>();
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<ITicketInventory, TicketInventory>();
        services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

        services.AddSingleton<IPaymentQueue, PaymentQueue>();
        services.AddHostedService<QrPaymentProcessor>();

        services.Configure<ReservationExpiryOptions>(configuration.GetSection(ReservationExpiryOptions.SectionName));
        services.AddSingleton<ReservationExpiryService>();
        services.AddHostedService(provider => provider.GetRequiredService<ReservationExpiryService>());

        return services;
    }
}
