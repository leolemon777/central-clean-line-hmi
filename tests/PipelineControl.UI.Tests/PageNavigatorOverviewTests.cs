using PipelineControl.UI.Services.Navigation;
using PipelineControl.UI.Views.Pages.IoMonitor;
using PipelineControl.UI.Views.Pages.Overview;
using PipelineControl.UI.Views.Pages.Settings;
using PipelineControl.UI.Views.Shell;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class PageNavigatorOverviewTests
{
    [Theory]
    [InlineData("Overview", typeof(OverviewPage))]
    [InlineData("OutputTest", typeof(OutputTestPage))]
    [InlineData("CommConfig", typeof(SettingsPage))]
    [InlineData("UnknownPage", typeof(PlaceholderPage))]
    public void NavigateTo_runtime_page_requests_expected_page_type(string pageKey, Type expectedType)
    {
        var services = new RecordingServiceProvider();
        var navigator = new PageNavigator(services);

        navigator.NavigateTo(pageKey);

        Assert.Equal(expectedType, services.RequestedType);
        Assert.Same(services.ReturnedPage, navigator.CurrentPageView);
    }

    private sealed class RecordingServiceProvider : IServiceProvider
    {
        public object ReturnedPage { get; } = new();

        public Type? RequestedType { get; private set; }

        public object? GetService(Type serviceType)
        {
            RequestedType = serviceType;
            return ReturnedPage;
        }
    }
}
