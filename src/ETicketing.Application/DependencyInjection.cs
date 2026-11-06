using System.Reflection;
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

        return services;
    }
}
