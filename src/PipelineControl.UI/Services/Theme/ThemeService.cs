using System.Collections;
using System.Windows;
using System.Windows.Media;

namespace PipelineControl.UI.Services.Theme;

public sealed class ThemeService : IThemeService
{
    private bool appPaletteInitialized;

    public AppTheme CurrentAppTheme { get; private set; } = AppTheme.Light;

    public event EventHandler<AppTheme>? AppThemeChanged;

    public void ApplyAppTheme(AppTheme theme)
    {
        if (CurrentAppTheme == theme && appPaletteInitialized)
        {
            return;
        }

        var changed = CurrentAppTheme != theme;
        ApplyPalette(theme);
        appPaletteInitialized = true;
        CurrentAppTheme = theme;
        if (changed)
        {
            AppThemeChanged?.Invoke(this, theme);
        }
    }

    public static AppTheme ParseAppTheme(string value)
    {
        return value.Trim() switch
        {
            "暗色" or "夜间" or "黑夜" or "夜间模式" => AppTheme.Dark,
            _ => AppTheme.Light
        };
    }

    private static void ApplyPalette(AppTheme theme)
    {
        var resources = System.Windows.Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        var palette = LoadPalette(theme);
        var colors = new Dictionary<string, Color>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in palette)
        {
            if (entry.Key is string key && TryResolveColor(entry.Value, out var color))
            {
                colors[key] = color;
                UpdateColorResource(resources, key, color);
                UpdateBrushResource(resources, $"{key}.Brush", color);
            }
        }

        UpdateSystemBrushResources(resources, colors);
    }

    private static ResourceDictionary LoadPalette(AppTheme theme)
    {
        var fileName = theme == AppTheme.Dark
            ? "AppDarkTheme.xaml"
            : "AppLightTheme.xaml";

        return (ResourceDictionary)System.Windows.Application.LoadComponent(
            new Uri($"/PipelineControl.UI;component/Themes/{fileName}", UriKind.Relative));
    }

    private static bool TryResolveColor(object? value, out Color color)
    {
        switch (value)
        {
            case Color current:
                color = current;
                return true;
            case SolidColorBrush brush:
                color = brush.Color;
                return true;
            case string text when ColorConverter.ConvertFromString(text) is Color parsed:
                color = parsed;
                return true;
            default:
                color = default;
                return false;
        }
    }

    private static void UpdateColorResource(ResourceDictionary resources, string key, Color color)
    {
        var owner = FindOwner(resources, key);
        if (owner is not null)
        {
            owner[key] = color;
        }
    }

    private static void UpdateBrushResource(ResourceDictionary resources, string key, Color color)
    {
        var owner = FindOwner(resources, key);
        if (owner?[key] is SolidColorBrush brush && !brush.IsFrozen)
        {
            brush.Color = color;
            return;
        }

        if (owner is not null)
        {
            owner[key] = new SolidColorBrush(color);
            return;
        }

        if (TryFindBrush(resources, key, out brush) && !brush.IsFrozen)
        {
            brush.Color = color;
        }
    }

    private static void UpdateSystemBrushResources(ResourceDictionary resources, IReadOnlyDictionary<string, Color> colors)
    {
        if (!colors.TryGetValue("Bg.Card", out var card) ||
            !colors.TryGetValue("Bg.Hover", out var hover) ||
            !colors.TryGetValue("Text.Primary", out var text) ||
            !colors.TryGetValue("Brand.Primary", out var selected) ||
            !colors.TryGetValue("Brand.OnPrimary", out var selectedText) ||
            !colors.TryGetValue("Border.Strong", out var border))
        {
            return;
        }

        resources[SystemColors.WindowBrushKey] = new SolidColorBrush(card);
        resources[SystemColors.ControlBrushKey] = new SolidColorBrush(card);
        resources[SystemColors.ControlLightBrushKey] = new SolidColorBrush(card);
        resources[SystemColors.ControlDarkBrushKey] = new SolidColorBrush(border);
        resources[SystemColors.ControlTextBrushKey] = new SolidColorBrush(text);
        resources[SystemColors.WindowTextBrushKey] = new SolidColorBrush(text);
        resources[SystemColors.GrayTextBrushKey] = new SolidColorBrush(colors.TryGetValue("Text.Tertiary", out var tertiary) ? tertiary : text);
        resources[SystemColors.ScrollBarBrushKey] = new SolidColorBrush(card);
        resources[SystemColors.MenuBrushKey] = new SolidColorBrush(card);
        resources[SystemColors.MenuTextBrushKey] = new SolidColorBrush(text);
        resources[SystemColors.HighlightBrushKey] = new SolidColorBrush(selected);
        resources[SystemColors.HighlightTextBrushKey] = new SolidColorBrush(selectedText);
        resources[SystemColors.InactiveSelectionHighlightBrushKey] = new SolidColorBrush(hover);
        resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = new SolidColorBrush(text);
    }

    private static bool TryFindBrush(ResourceDictionary resources, string key, out SolidColorBrush brush)
    {
        foreach (var mergedDictionary in resources.MergedDictionaries)
        {
            if (TryFindBrush(mergedDictionary, key, out brush))
            {
                return true;
            }
        }

        if (resources.Contains(key) && resources[key] is SolidColorBrush current)
        {
            brush = current;
            return true;
        }

        brush = null!;
        return false;
    }

    private static ResourceDictionary? FindOwner(ResourceDictionary resources, string key)
    {
        foreach (var mergedDictionary in resources.MergedDictionaries)
        {
            var owner = FindOwner(mergedDictionary, key);
            if (owner is not null)
            {
                return owner;
            }
        }

        if (resources.Contains(key))
        {
            return resources;
        }

        return null;
    }
}

