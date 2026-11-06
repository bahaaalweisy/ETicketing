using System.Reflection;
using ETicketing.Application.Bookings;
using ETicketing.Application.Orders;
using ETicketing.Application.Tickets;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ETicketing.Application;

/// <summary>
/// Registers the Application layer's services with the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the use-case services and every FluentValidation validator in this assembly.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<TicketQueryService>();
        services.AddScoped<OrderQueryService>();
        services.AddScoped<BookingService>();

        return services;
    }
}
