using PipelineControl.UI.ViewModels.Shell;
using PipelineControl.UI.Services.Settings;
using PipelineControl.UI.Services.Theme;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_registers_runtime_entries_and_defaults_to_overview()
    {
        var navigator = new RecordingNavigator();
        var viewModel = new MainWindowViewModel(navigator, new StatusBarViewModel());
        var entries = viewModel.CurrentSideNavGroups.SelectMany(group => group.Items).ToList();

        Assert.Equal(new[] { "总控", "IO 点位", "伺服", "通讯" }, entries.Select(item => item.Title));
        Assert.Equal(new[] { "Overview", "OutputTest", "Servo", "CommConfig" }, entries.Select(item => item.PageKey));
        Assert.Equal("Overview", navigator.LastPageKey);
        Assert.Equal("总控", viewModel.SelectedSideNav?.Title);
    }

    [Fact]
    public async Task Toggle_theme_switches_palette_text_and_saves_local_setting()
    {
        var navigator = new RecordingNavigator();
        var themeService = new ThemeService();
        var settingsService = new TestSettingsService(settings => settings.Theme.ThemeMode = "亮色");
        var viewModel = new MainWindowViewModel(navigator, new StatusBarViewModel(), themeService, settingsService);

        Assert.Equal("暗色", viewModel.ThemeToggleText);

        await viewModel.ToggleThemeCommand.ExecuteAsync(null);

        Assert.Equal(AppTheme.Dark, themeService.CurrentAppTheme);
        Assert.Equal("亮色", viewModel.ThemeToggleText);
        Assert.Equal("暗色", settingsService.SavedSettings.Theme.ThemeMode);
    }
}
