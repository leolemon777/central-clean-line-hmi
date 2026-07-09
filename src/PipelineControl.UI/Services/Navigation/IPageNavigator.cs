namespace PipelineControl.UI.Services.Navigation;

public interface IPageNavigator
{
    event EventHandler<string>? Navigated;

    object? CurrentPageView { get; }

    void NavigateTo(string pageKey);
}
