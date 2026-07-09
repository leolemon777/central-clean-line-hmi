namespace PipelineControl.UI.Services.Theme;

public enum AppTheme
{
    Light,
    Dark
}

public interface IThemeService
{
    AppTheme CurrentAppTheme { get; }

    event EventHandler<AppTheme>? AppThemeChanged;

    void ApplyAppTheme(AppTheme theme);
}

