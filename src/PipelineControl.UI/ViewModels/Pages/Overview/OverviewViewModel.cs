using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Io.Mapping;
using PipelineControl.UI.Services.Line;
using PipelineControl.UI.Services.Logs;
using PipelineControl.UI.Services.Navigation;
using PipelineControl.UI.Services.Settings;
using System.Windows.Threading;

namespace PipelineControl.UI.ViewModels.Pages.Overview;

public partial class OverviewViewModel : ObservableObject
{
    private readonly IoBoardService ioBoardService;
    private readonly LineControlService lineControlService;
    private readonly IPageNavigator? pageNavigator;
    private readonly Dispatcher dispatcher;

    public OverviewViewModel()
        : this(CreateDefaultService(), pageNavigator: null)
    {
    }

    private OverviewViewModel(IoBoardService ioBoardService, IPageNavigator? pageNavigator = null)
        : this(ioBoardService, new LineControlService(ioBoardService, new JsonAppLogService()), pageNavigator)
    {
    }

    public OverviewViewModel(
        IoBoardService ioBoardService,
        LineControlService lineControlService,
        IPageNavigator? pageNavigator = null)
    {
        this.ioBoardService = ioBoardService;
        this.lineControlService = lineControlService;
        this.pageNavigator = pageNavigator;
        var appDispatcher = System.Windows.Application.Current?.Dispatcher;
        dispatcher = appDispatcher is { HasShutdownStarted: false, HasShutdownFinished: false }
                     && appDispatcher.CheckAccess()
            ? appDispatcher
            : Dispatcher.CurrentDispatcher;

        ioBoardService.SnapshotChanged += OnSnapshotChanged;
        lineControlService.StateChanged += OnLineStateChanged;

        ApplySnapshot(ioBoardService.CurrentSnapshot);
        ApplyLineState(lineControlService.State, lineControlService.StatusMessage);
    }

    [ObservableProperty]
    private bool isIoConnected;

    [ObservableProperty]
    private string communicationStatusText = string.Empty;

    [ObservableProperty]
    private string mainCardIp = string.Empty;

    [ObservableProperty]
    private double scanCycleMs;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunning))]
    [NotifyPropertyChangedFor(nameof(IsFault))]
    [NotifyCanExecuteChangedFor(nameof(StartAutoCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopAutoCommand))]
    private LineRunState lineState;

    [ObservableProperty]
    private string lineStateText = string.Empty;

    [ObservableProperty]
    private string operationMessage = string.Empty;

    public bool IsRunning => LineState == LineRunState.Running;

    public bool IsFault => LineState == LineRunState.Fault;

    private bool CanStartAuto() => LineState == LineRunState.Idle;

    private bool CanStopAuto() => LineState != LineRunState.Idle;

    [RelayCommand(CanExecute = nameof(CanStartAuto))]
    private async Task StartAutoAsync()
    {
        await lineControlService.StartAsync().ConfigureAwait(false);
    }

    [RelayCommand(CanExecute = nameof(CanStopAuto))]
    private async Task StopAutoAsync()
    {
        await lineControlService.StopAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private void OpenOutputTest()
    {
        pageNavigator?.NavigateTo("OutputTest");
    }

    private void OnSnapshotChanged(object? sender, IoBoardSnapshot snapshot)
    {
        _ = RunOnUiThreadAsync(() => ApplySnapshot(snapshot));
    }

    private void OnLineStateChanged(object? sender, LineStateChangedEventArgs e)
    {
        _ = RunOnUiThreadAsync(() => ApplyLineState(e.State, e.Message));
    }

    private void ApplySnapshot(IoBoardSnapshot snapshot)
    {
        IsIoConnected = snapshot.IsConnected;
        MainCardIp = string.IsNullOrWhiteSpace(snapshot.MainCardIp) ? "--" : snapshot.MainCardIp;
        ScanCycleMs = snapshot.ScanCycleMs;
        CommunicationStatusText = snapshot.IsConnected
            ? $"在线 · {snapshot.DriverName}"
            : string.IsNullOrWhiteSpace(snapshot.LastError) ? "离线" : snapshot.LastError;
    }

    private void ApplyLineState(LineRunState state, string message)
    {
        LineState = state;
        LineStateText = state switch
        {
            LineRunState.Running => "运行",
            LineRunState.Fault => "异常",
            _ => "待机"
        };
        OperationMessage = message;
    }

    private Task RunOnUiThreadAsync(Action action)
    {
        if (dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
        {
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action).Task;
    }

    private static IoBoardService CreateDefaultService()
    {
        return new IoBoardService(
            new IoBoardDriverFactory(),
            new JsonIoPointMapProvider(),
            new JsonSettingsService());
    }
}
