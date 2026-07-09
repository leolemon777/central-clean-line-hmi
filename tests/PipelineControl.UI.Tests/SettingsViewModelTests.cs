using PipelineControl.UI.Services.Settings;
using PipelineControl.UI.ViewModels.Pages.Settings;
using System.IO;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class SettingsViewModelTests : IDisposable
{
    private readonly string tempRoot = Path.Combine(Path.GetTempPath(), $"PipelineSettingsVm-{Guid.NewGuid():N}");

    public SettingsViewModelTests()
    {
        Directory.CreateDirectory(tempRoot);
    }

    [Fact]
    public async Task Constructor_loads_card_communication_and_servo_fields()
    {
        var viewModel = CreateViewModel();
        await viewModel.InitialLoadTask;

        Assert.Equal(16, viewModel.AllFields.Count);
        Assert.Equal(new[]
        {
            "CardComm.PcIp",
            "CardComm.MainCardIp",
            "CardComm.ExtensionCardCount",
            "CardComm.ScanCycleMs",
            "CardComm.HeartbeatMs",
            "Advanced.SimulationMode",
            "ServoComm.GatewayIp",
            "ServoComm.GatewayPort",
            "ServoComm.Axis1Station",
            "ServoComm.Axis2Station",
            "ServoComm.Axis3Station",
            "ServoComm.Axis4Station",
            "ServoComm.ScanCycleMs",
            "ServoComm.HeartbeatCycleMs",
            "ServoComm.DefaultSpeedRpm",
            "ServoComm.MaxSpeedRpm"
        }, viewModel.AllFields.Select(field => field.Key));
        Assert.Equal(0, viewModel.DirtyCount);
    }

    [Fact]
    public async Task Invalid_ip_sets_validation_error_and_blocks_save()
    {
        var viewModel = CreateViewModel();
        await viewModel.InitialLoadTask;
        var pcIp = viewModel.AllFields.Single(field => field.Key == "CardComm.PcIp");

        pcIp.Value = "999.1.1.1";

        Assert.True(pcIp.HasError);
        Assert.True(viewModel.HasValidationErrors);
        Assert.False(viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task Saving_card_field_requires_confirmation_then_writes_local_settings_file()
    {
        var service = new JsonSettingsService(tempRoot);
        var viewModel = new SettingsViewModel(service);
        await viewModel.InitialLoadTask;
        var scanCycle = viewModel.AllFields.Single(field => field.Key == "CardComm.ScanCycleMs");

        scanCycle.Value = "35";
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsConfirmationOpen);
        await viewModel.ConfirmPendingCommand.ExecuteAsync(null);
        var saved = await service.LoadAsync();

        Assert.Equal(35, saved.CardComm.ScanCycleMs);
        Assert.True(File.Exists(service.LocalSettingsPath));
        Assert.Equal(0, viewModel.DirtyCount);
        Assert.Contains("重启", viewModel.RestartNoticeText, StringComparison.Ordinal);
    }

    private SettingsViewModel CreateViewModel()
    {
        return new SettingsViewModel(new JsonSettingsService(tempRoot));
    }

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
