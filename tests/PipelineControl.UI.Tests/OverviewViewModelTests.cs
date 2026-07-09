using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Io.Mapping;
using PipelineControl.UI.Services.Line;
using PipelineControl.UI.ViewModels.Pages.Overview;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class OverviewViewModelTests
{
    [Fact]
    public void Constructor_initializes_simple_line_overview()
    {
        var viewModel = CreateViewModel(out _, out _, out _);

        Assert.False(viewModel.IsIoConnected);
        Assert.False(viewModel.IsRunning);
        Assert.Equal("待机", viewModel.LineStateText);
        Assert.Equal("离线", viewModel.CommunicationStatusText);
    }

    [Fact]
    public async Task Auto_start_without_connection_is_blocked()
    {
        var viewModel = CreateViewModel(out var driver, out var appLogs, out _);

        await viewModel.StartAutoCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsRunning);
        Assert.Equal("待机", viewModel.LineStateText);
        Assert.Contains("请先连接板卡", viewModel.OperationMessage, StringComparison.Ordinal);
        Assert.Contains(appLogs.Entries, entry => entry.Command == "AUTO_START_BLOCKED");
        Assert.Empty(driver.Writes);
        Assert.Empty(driver.ModuleWrites);
    }

    [Fact]
    public void Shortcut_command_navigates_to_output_test()
    {
        var viewModel = CreateViewModel(out _, out _, out var navigator);

        viewModel.OpenOutputTestCommand.Execute(null);
        Assert.Equal("OutputTest", navigator.LastPageKey);
    }

    private static OverviewViewModel CreateViewModel(
        out RecordingIoBoardDriver driver,
        out RecordingAppLogService appLogs,
        out RecordingNavigator navigator)
    {
        driver = new RecordingIoBoardDriver();
        appLogs = new RecordingAppLogService();
        navigator = new RecordingNavigator();
        var ioService = new IoBoardService(
            new RecordingDriverFactory(driver),
            new JsonIoPointMapProvider("missing-io-points.json"),
            new TestSettingsService());
        var lineControlService = new LineControlService(ioService, appLogs);
        return new OverviewViewModel(ioService, lineControlService, navigator);
    }
}
