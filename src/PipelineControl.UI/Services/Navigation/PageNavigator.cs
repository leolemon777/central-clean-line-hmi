using Microsoft.Extensions.DependencyInjection;
using PipelineControl.UI.Views.Pages.IoMonitor;
using PipelineControl.UI.Views.Pages.Overview;
using PipelineControl.UI.Views.Pages.Servo;
using PipelineControl.UI.Views.Pages.Settings;
using PipelineControl.UI.Views.Shell;
using System.Windows;

namespace PipelineControl.UI.Services.Navigation;

public class PageNavigator : IPageNavigator
{
    private readonly IServiceProvider serviceProvider;
    private readonly Dictionary<string, Type> pageRegistry = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Overview"] = typeof(OverviewPage),
        ["OutputTest"] = typeof(OutputTestPage),
        ["Servo"] = typeof(ServoPage),
        ["CommConfig"] = typeof(SettingsPage),
        ["Placeholder"] = typeof(PlaceholderPage),
        ["*"] = typeof(PlaceholderPage)
    };

    public PageNavigator(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public event EventHandler<string>? Navigated;

    public object? CurrentPageView { get; private set; }

    public void NavigateTo(string pageKey)
    {
        var pageType = pageRegistry.TryGetValue(pageKey, out var registeredType)
            ? registeredType
            : pageRegistry["*"];

        var page = serviceProvider.GetRequiredService(pageType);
        if (page is PlaceholderPage && page is FrameworkElement element)
        {
            element.DataContext = pageKey;
        }

        CurrentPageView = page;
        Navigated?.Invoke(this, pageKey);
    }
}
