using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ETicketing.Infrastructure;

/// <summary>
/// Registers the Infrastructure layer's implementations with the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the implementations of the Application abstractions (persistence, queue, background services).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
