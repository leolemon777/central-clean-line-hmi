using PipelineControl.UI.Services.Settings;
using PipelineControl.UI.ViewModels.Pages.Settings;
using PipelineControl.UI.Views.Pages.Settings;
using System.IO;
using System.Windows;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class SettingsPageSmokeTests : IDisposable
{
    private readonly string tempRoot = Path.Combine(Path.GetTempPath(), $"PipelineSettingsPage-{Guid.NewGuid():N}");

    public SettingsPageSmokeTests()
    {
        Directory.CreateDirectory(tempRoot);
    }

    [Fact]
    public void Settings_page_loads_with_real_resources()
    {
        WpfTestPump.RunOnWpfThread(() =>
        {
            var viewModel = new SettingsViewModel(new JsonSettingsService(tempRoot));
            WpfTestPump.Run(viewModel.InitialLoadTask);
            var page = new SettingsPage
            {
                DataContext = viewModel
            };

            page.Measure(new Size(1500, 1000));
            page.Arrange(new Rect(0, 0, 1500, 1000));
            page.UpdateLayout();

            Assert.True(page.DesiredSize.Width > 0);
            Assert.True(page.DesiredSize.Height > 0);
        });
    }

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
