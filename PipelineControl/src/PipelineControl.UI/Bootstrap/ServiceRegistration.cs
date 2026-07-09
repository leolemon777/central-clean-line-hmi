using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PipelineControl.Application;
using PipelineControl.Infrastructure;
using PipelineControl.Shared.Configuration;
using PipelineControl.UI.ViewModels;
using PipelineControl.UI.Views;

namespace PipelineControl.UI.Bootstrap;

public static class ServiceRegistration
{
    public static void RegisterAll(IServiceCollection services, IConfiguration cfg)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(cfg);

        services.AddSingleton<MainWindow>();
        services.AddTransient<MainWindowViewModel>();

        services.Configure<AppOptions>(cfg.GetSection(AppOptions.SectionName));
        services.Configure<BopaiCardOptions>(cfg.GetSection(BopaiCardOptions.SectionName));

        services.AddApplicationServices();
        services.AddInfrastructureServices(cfg);
        services.AddDrivers(cfg);
    }
}
