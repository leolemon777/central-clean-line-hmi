using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PipelineControl.UI.Bootstrap;
using PipelineControl.UI.Views;
using Serilog;
using System.Windows;

namespace PipelineControl.UI;

public partial class App : System.Windows.Application
{
    private static readonly Action<Microsoft.Extensions.Logging.ILogger, Exception?> LogHostStarted =
        LoggerMessage.Define(LogLevel.Information, new EventId(1000, nameof(LogHostStarted)), "Central Softening Line Control Console host started.");

    public static IHost AppHost { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.SetBasePath(AppContext.BaseDirectory);
                configuration.AddJsonFile("appsettings.json", optional: false);
                configuration.AddJsonFile("appsettings.local.json", optional: true);
            })
            .UseSerilog((context, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext())
            .ConfigureServices((context, services) =>
            {
                ServiceRegistration.RegisterAll(services, context.Configuration);
            })
            .Build();

        await AppHost.StartAsync();

        ILogger<App> logger = AppHost.Services.GetRequiredService<ILogger<App>>();
        LogHostStarted(logger, null);

        MainWindow mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost.StopAsync(TimeSpan.FromSeconds(3));
        AppHost.Dispose();
        base.OnExit(e);
    }
}
