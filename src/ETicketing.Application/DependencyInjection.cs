using System.Reflection;
using ETicketing.Application.Abstractions;
using ETicketing.Application.Bookings;
using ETicketing.Application.Orders;
using ETicketing.Application.Payments;
using ETicketing.Application.Tickets;
using ETicketing.Application.Transactions;
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
        services.AddScoped<TransactionQueryService>();
        services.AddScoped<BookingService>();
        services.AddScoped<CheckoutService>();
        services.AddScoped<PendingTransactionCreator>();
        services.AddScoped<IPaymentHandlerResolver, PaymentHandlerResolver>();

        return services;
    }
}
