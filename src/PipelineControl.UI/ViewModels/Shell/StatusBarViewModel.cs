using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Threading;

namespace PipelineControl.UI.ViewModels.Shell;

public partial class StatusBarViewModel : ObservableObject
{
    private readonly DispatcherTimer clockTimer;

    public StatusBarViewModel()
    {
        mainCardOnline = false;
        mainCardIp = string.Empty;
        extCardsOnline = false;
        scanCycleMs = 0;
        keepAliveRemainMs = 0;
        currentTime = DateTime.Now;

        clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        clockTimer.Tick += (_, _) => CurrentTime = DateTime.Now;
        clockTimer.Start();
    }

    [ObservableProperty]
    private bool mainCardOnline;

    [ObservableProperty]
    private string mainCardIp;

    [ObservableProperty]
    private bool extCardsOnline;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScanCycleLevel))]
    private double scanCycleMs;

    [ObservableProperty]
    private double keepAliveRemainMs;

    [ObservableProperty]
    private DateTime currentTime;

    public ScanCycleLevel ScanCycleLevel => ScanCycleMs <= 15
        ? ScanCycleLevel.Normal
        : ScanCycleMs <= 30
            ? ScanCycleLevel.Warning
            : ScanCycleLevel.Danger;
}

public enum ScanCycleLevel
{
    Normal,
    Warning,
    Danger
}
