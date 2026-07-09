using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PipelineControl.UI.Services.Navigation;
using PipelineControl.UI.Services.Settings;
using PipelineControl.UI.Services.Theme;
using PipelineControl.UI.ViewModels.Shell.Models;
using System.Collections.ObjectModel;

namespace PipelineControl.UI.ViewModels.Shell;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IPageNavigator pageNavigator;
    private readonly IThemeService? themeService;
    private readonly ISettingsService? settingsService;

    public MainWindowViewModel(
        IPageNavigator pageNavigator,
        StatusBarViewModel statusBar,
        IThemeService? themeService = null,
        ISettingsService? settingsService = null)
    {
        this.pageNavigator = pageNavigator;
        this.themeService = themeService;
        this.settingsService = settingsService;
        StatusBar = statusBar;

        CurrentSideNavGroups =
        [
            new SideNavGroup("主控", new[]
            {
                new SideNavItem("总控", "⌂", "Overview"),
                new SideNavItem("IO 点位", "IO", "OutputTest"),
                new SideNavItem("伺服", "↻", "Servo"),
                new SideNavItem("通讯", "⚙", "CommConfig")
            })
        ];

        pageNavigator.Navigated += OnNavigated;
        if (this.themeService is not null)
        {
            this.themeService.AppThemeChanged += OnAppThemeChanged;
        }

        SelectSideNav(CurrentSideNavGroups.SelectMany(group => group.Items).First());
    }

    public ObservableCollection<SideNavGroup> CurrentSideNavGroups { get; }

    public StatusBarViewModel StatusBar { get; }

    [ObservableProperty]
    private SideNavItem? selectedSideNav;

    [ObservableProperty]
    private object? currentPageView;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserInitial))]
    private string userName = "系统";

    public bool IsDarkTheme => themeService?.CurrentAppTheme == AppTheme.Dark;

    public string ThemeToggleText => IsDarkTheme ? "亮色" : "暗色";

    public string ThemeToggleTooltip => IsDarkTheme ? "切换到亮色主题" : "切换到暗色主题";

    [RelayCommand]
    private void SelectSideNav(SideNavItem? item)
    {
        if (item is null)
        {
            return;
        }

        foreach (var sideItem in CurrentSideNavGroups.SelectMany(group => group.Items))
        {
            sideItem.IsSelected = ReferenceEquals(sideItem, item);
        }

        SelectedSideNav = item;
        pageNavigator.NavigateTo(item.PageKey);
    }

    private void OnNavigated(object? sender, string pageKey)
    {
        CurrentPageView = pageNavigator.CurrentPageView;
    }

    [RelayCommand]
    private async Task ToggleThemeAsync()
    {
        if (themeService is null)
        {
            return;
        }

        var target = themeService.CurrentAppTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        themeService.ApplyAppTheme(target);
        if (settingsService is null)
        {
            return;
        }

        try
        {
            var settings = await settingsService.LoadAsync().ConfigureAwait(true);
            settings.Theme.ThemeMode = target == AppTheme.Dark ? "暗色" : "亮色";
            await settingsService.SaveLocalAsync(settings).ConfigureAwait(true);
        }
        catch
        {
            // 主题已即时切换；配置保存失败不应阻断现场操作。
        }
    }

    private void OnAppThemeChanged(object? sender, AppTheme theme)
    {
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(ThemeToggleText));
        OnPropertyChanged(nameof(ThemeToggleTooltip));
    }

    public string UserInitial => string.IsNullOrWhiteSpace(UserName) ? string.Empty : UserName[..1];
}
