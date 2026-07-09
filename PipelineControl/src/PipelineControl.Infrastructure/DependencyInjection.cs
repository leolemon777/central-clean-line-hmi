using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PipelineControl.Drivers.Abstractions.Selection;
using PipelineControl.Shared.Configuration;

namespace PipelineControl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration cfg)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(cfg);

        return services;
    }

    public static IServiceCollection AddDrivers(this IServiceCollection services, IConfiguration cfg)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(cfg);

        services.AddSingleton<IDriverSelection>(serviceProvider =>
        {
            BopaiCardOptions options = serviceProvider.GetRequiredService<IOptions<BopaiCardOptions>>().Value;
            DriverKind driverKind = options.UseSimulator ? DriverKind.Simulator : DriverKind.Bopai;
            return new DriverSelection(driverKind);
        });

        return services;
    }
}
