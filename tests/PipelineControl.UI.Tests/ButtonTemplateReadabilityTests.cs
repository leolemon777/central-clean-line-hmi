using System.IO;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class ButtonTemplateReadabilityTests
{
    [Fact]
    public void Button_family_templates_bind_content_presenter_foreground()
    {
        var missing = new List<string>();
        var sourceRoot = Path.Combine(FindRepositoryRoot(), "src", "PipelineControl.UI");

        foreach (var filePath in Directory.EnumerateFiles(sourceRoot, "*.xaml", SearchOption.AllDirectories))
        {
            if (filePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || filePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var document = XDocument.Load(filePath, LoadOptions.SetLineInfo);
            foreach (var template in document.Descendants().Where(IsButtonFamilyTemplate))
            {
                foreach (var presenter in template.Descendants().Where(element => element.Name.LocalName == "ContentPresenter"))
                {
                    var bindsForeground = presenter.Attributes().Any(attribute => attribute.Name.LocalName == "TextElement.Foreground");
                    var usesButtonTextStyle = presenter.Attributes().Any(attribute =>
                        attribute.Name.LocalName == "Style"
                        && attribute.Value.Contains("ButtonTemplateContentPresenterStyle", StringComparison.Ordinal));

                    if (bindsForeground && usesButtonTextStyle)
                    {
                        continue;
                    }

                    var lineInfo = (IXmlLineInfo)presenter;
                    var relativePath = Path.GetRelativePath(sourceRoot, filePath);
                    missing.Add($"{relativePath}:{lineInfo.LineNumber}");
                }
            }
        }

        Assert.True(
            missing.Count == 0,
            "These button-family templates render content without binding TextElement.Foreground and ButtonTemplateContentPresenterStyle: "
            + string.Join(", ", missing));
    }

    private static bool IsButtonFamilyTemplate(XElement element)
    {
        if (element.Name.LocalName != "ControlTemplate")
        {
            return false;
        }

        var targetType = element.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "TargetType")?.Value;
        return targetType is not null
               && (targetType.Contains("Button", StringComparison.Ordinal)
                   || targetType.Contains("CheckBox", StringComparison.Ordinal));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CentralCleanLineHmi.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing CentralCleanLineHmi.sln.");
    }
}
