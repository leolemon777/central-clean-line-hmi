using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using PipelineControl.Shared.Configuration;
using PipelineControl.UI.Bootstrap;
using System.Text;
using System.Windows;

namespace PipelineControl.UI.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IOptionsMonitor<AppOptions> appOptions;

    public MainWindowViewModel(IOptionsMonitor<AppOptions> appOptions)
    {
        this.appOptions = appOptions;
        ShowVersionInfoCommand = new RelayCommand(ShowVersionInfo);
    }

    public string ReadyText => "中央净软线控制台 · 骨架已就绪";

    public IRelayCommand ShowVersionInfoCommand { get; }

    private void ShowVersionInfo()
    {
        AppOptions options = appOptions.CurrentValue;
        StringBuilder message = new();

        _ = message.Append("ApplicationName: ").AppendLine(options.ApplicationName);
        _ = message.Append("App.Version: ").AppendLine(options.Version);
        _ = message.Append("StationName: ").AppendLine(options.StationName);
        _ = message.Append("LineName: ").AppendLine(options.LineName);
        _ = message.AppendLine();
        _ = message.AppendLine("已加载 NuGet 包列表:");

        foreach (string package in PackageCatalog.Packages)
        {
            _ = message.Append("- ").AppendLine(package);
        }

        _ = MessageBox.Show(message.ToString(), "版本信息", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
