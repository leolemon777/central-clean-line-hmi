using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Io.Mapping;
using PipelineControl.UI.Services.Line;
using PipelineControl.UI.Services.Settings;
using PipelineControl.UI.Services.Theme;
using PipelineControl.UI.ViewModels.Pages.IoMonitor;
using PipelineControl.UI.ViewModels.Pages.Overview;
using PipelineControl.UI.ViewModels.Pages.Settings;
using PipelineControl.UI.Views.Pages.IoMonitor;
using PipelineControl.UI.Views.Pages.Overview;
using PipelineControl.UI.Views.Pages.Settings;
using System.IO;
using System.Windows;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class AppThemePageSmokeTests : IDisposable
{
    private readonly string tempRoot = Path.Combine(Path.GetTempPath(), $"PipelineThemePages-{Guid.NewGuid():N}");

    public AppThemePageSmokeTests()
    {
        Directory.CreateDirectory(tempRoot);
    }

    [Theory]
    [InlineData(1500, 950)]
    [InlineData(980, 720)]
    public void Runtime_pages_load_with_light_and_dark_app_resources(double width, double height)
    {
        WpfTestPump.RunOnWpfThread(() =>
        {
            var themeService = new ThemeService();
            themeService.ApplyAppTheme(AppTheme.Light);
            MeasureRuntimePages(width, height);
            themeService.ApplyAppTheme(AppTheme.Dark);
            MeasureRuntimePages(width, height);
        });
    }

    private void MeasureRuntimePages(double width, double height)
    {
        var ioService = new IoBoardService(
            new RecordingDriverFactory(new RecordingIoBoardDriver()),
            new JsonIoPointMapProvider("missing-io-points.json"),
            new TestSettingsService());
        var lineControlService = new LineControlService(ioService, new RecordingAppLogService());
        var settingsViewModel = new SettingsViewModel(new JsonSettingsService(tempRoot));
        WpfTestPump.Run(settingsViewModel.InitialLoadTask);

        var pages = new FrameworkElement[]
        {
            new OverviewPage
            {
                DataContext = new OverviewViewModel(
                    ioService,
                    lineControlService,
                    new RecordingNavigator())
            },
            new OutputTestPage
            {
                DataContext = new OutputTestViewModel(ioService, lineControlService)
            },
            new SettingsPage
            {
                DataContext = settingsViewModel
            }
        };

        foreach (var page in pages)
        {
            page.Measure(new Size(width, height));
            page.Arrange(new Rect(0, 0, width, height));
            page.UpdateLayout();
            Assert.True(page.DesiredSize.Width > 0);
            Assert.True(page.DesiredSize.Height > 0);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
