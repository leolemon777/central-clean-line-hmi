using Microsoft.Extensions.DependencyInjection;
using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Io.Mapping;
using PipelineControl.UI.Services.Line;
using PipelineControl.UI.Services.Logs;
using PipelineControl.UI.Services.Navigation;
using PipelineControl.UI.Services.Settings;
using PipelineControl.UI.Services.Servo;
using PipelineControl.UI.Services.Servo.Mapping;
using PipelineControl.UI.Services.Theme;
using PipelineControl.UI.ViewModels.Pages.IoMonitor;
using PipelineControl.UI.ViewModels.Pages.Overview;
using PipelineControl.UI.ViewModels.Pages.Servo;
using PipelineControl.UI.ViewModels.Pages.Settings;
using PipelineControl.UI.ViewModels.Shell;
using PipelineControl.UI.Views.Pages.IoMonitor;
using PipelineControl.UI.Views.Pages.Overview;
using PipelineControl.UI.Views.Pages.Servo;
using PipelineControl.UI.Views.Pages.Settings;
using PipelineControl.UI.Views.Shell;
using System.IO;
using System.Windows;

namespace PipelineControl.UI;

public partial class App : System.Windows.Application
{
    private ServiceProvider? serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (TryCreateIoPointImportRequest(e.Args, out var importRequest, out var importErrorMessage))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            if (importRequest is null)
            {
                await ShowIoPointImportErrorAndShutdownAsync(importErrorMessage ?? "IO 点位回填参数不完整。");
            }
            else
            {
                await ImportIoPointsAndShutdownAsync(importRequest);
            }

            return;
        }

        var services = new ServiceCollection();
        ConfigureServices(services);
        serviceProvider = services.BuildServiceProvider();
        _ = serviceProvider.GetRequiredService<IoBoardEventBridge>();
        await ApplyInitialThemesAsync(serviceProvider);

        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
        mainWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<PageNavigator>();
        services.AddSingleton<IPageNavigator>(provider => provider.GetRequiredService<PageNavigator>());
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<IAppLogService, JsonAppLogService>();
        services.AddSingleton<IIoPointMapProvider, JsonIoPointMapProvider>();
        services.AddSingleton<IIoBoardDriverFactory, IoBoardDriverFactory>();
        services.AddSingleton<IIoPointVerificationExporter, IoPointVerificationExporter>();
        services.AddSingleton<IIoPointVerificationImporter, IoPointVerificationImporter>();
        services.AddSingleton<IoBoardService>();
        services.AddSingleton<IoBoardEventBridge>();
        services.AddSingleton<LineControlService>();
        services.AddSingleton<IServoRegisterMapProvider, JsonServoRegisterMapProvider>();
        services.AddSingleton<IServoDriverFactory, ServoDriverFactory>();
        services.AddSingleton<ServoService>();
        services.AddSingleton<StatusBarViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddSingleton<OverviewViewModel>();
        services.AddSingleton<OutputTestViewModel>();
        services.AddSingleton<ServoViewModel>();
        services.AddSingleton<SettingsViewModel>();

        services.AddSingleton<PlaceholderPage>();
        services.AddSingleton(provider => CreatePage<OverviewPage, OverviewViewModel>(provider));
        services.AddSingleton(provider => CreatePage<OutputTestPage, OutputTestViewModel>(provider));
        services.AddSingleton(provider => CreatePage<ServoPage, ServoViewModel>(provider));
        services.AddSingleton(provider => CreatePage<SettingsPage, SettingsViewModel>(provider));

        services.AddTransient(provider =>
        {
            var window = new MainWindow
            {
                DataContext = provider.GetRequiredService<MainWindowViewModel>()
            };
            return window;
        });
    }

    private static TPage CreatePage<TPage, TViewModel>(IServiceProvider provider)
        where TPage : FrameworkElement, new()
        where TViewModel : class
    {
        return new TPage
        {
            DataContext = provider.GetRequiredService<TViewModel>()
        };
    }

    private static async Task ApplyInitialThemesAsync(IServiceProvider provider)
    {
        try
        {
            var settings = await provider.GetRequiredService<ISettingsService>().LoadAsync().ConfigureAwait(true);
            var themeService = provider.GetRequiredService<IThemeService>();
            themeService.ApplyAppTheme(ThemeService.ParseAppTheme(settings.Theme.ThemeMode));
        }
        catch
        {
            // Theme loading must not block startup; defaults from Colors.xaml remain usable.
        }
    }

    private static bool TryCreateIoPointImportRequest(
        IReadOnlyList<string> args,
        out IoPointImportRequest? request,
        out string? errorMessage)
    {
        request = null;
        errorMessage = null;
        var importIndex = IndexOfArg(args, "--import-io-points");
        if (importIndex < 0)
        {
            return false;
        }

        if (importIndex + 1 >= args.Count || args[importIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            errorMessage = "用法: PipelineControl.UI.exe --import-io-points <现场验证CSV> [--io-points-json <io-points.json路径>]";
            return true;
        }

        var csvFilePath = args[importIndex + 1];
        var jsonIndex = IndexOfArg(args, "--io-points-json");
        var ioPointsJsonPath = jsonIndex >= 0 && jsonIndex + 1 < args.Count
            ? args[jsonIndex + 1]
            : Path.Combine(AppContext.BaseDirectory, "Resources", "io-points.json");

        request = new IoPointImportRequest(csvFilePath, ioPointsJsonPath);
        return true;
    }

    private static int IndexOfArg(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private async Task ImportIoPointsAndShutdownAsync(IoPointImportRequest request)
    {
        try
        {
            var importer = new IoPointVerificationImporter();
            var result = await importer.ImportCsvAsync(request.CsvFilePath, request.IoPointsJsonPath).ConfigureAwait(true);
            MessageBox.Show(
                $"IO 点位已回填。\n更新: {result.UpdatedCount} 点，跳过: {result.SkippedCount} 点。\n配置: {result.OutputFilePath}\n备份: {result.BackupFilePath}",
                "IO 点位回填",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"IO 点位回填失败: {ex.Message}",
                "IO 点位回填",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            Shutdown();
        }
    }

    private Task ShowIoPointImportErrorAndShutdownAsync(string message)
    {
        MessageBox.Show(message, "IO 点位回填", MessageBoxButton.OK, MessageBoxImage.Warning);
        Shutdown();
        return Task.CompletedTask;
    }

    private sealed record IoPointImportRequest(string CsvFilePath, string IoPointsJsonPath);
}


