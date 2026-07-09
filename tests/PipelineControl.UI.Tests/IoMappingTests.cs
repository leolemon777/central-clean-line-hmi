using PipelineControl.UI.Services.Io;
using PipelineControl.UI.Services.Io.Mapping;
using System.IO;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class IoMappingTests
{
    [Fact]
    public void Default_mapping_contains_64_inputs_and_64_outputs()
    {
        var map = new JsonIoPointMapProvider("missing-io-points.json").Load();

        Assert.Equal(64, map.Inputs.Count);
        Assert.Equal(64, map.Outputs.Count);
        Assert.Contains(map.Notes, note => note.Contains("三张物理卡", StringComparison.Ordinal));
    }

    [Fact]
    public void Resource_mapping_file_is_copied_and_readable()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "io-points.json");
        var map = new JsonIoPointMapProvider(path).Load();

        Assert.True(File.Exists(path));
        Assert.Equal(64, map.Inputs.Count);
        Assert.Equal(64, map.Outputs.Count);
    }

    [Theory]
    [InlineData(1, "线头第一工位光电")]
    [InlineData(2, "线头升降台行程开关")]
    [InlineData(3, "线头下限位开关")]
    [InlineData(4, "线头上限位开关")]
    [InlineData(5, "线尾AGV信号")]
    [InlineData(6, "线尾升降台行程开关")]
    [InlineData(7, "线尾下限开关")]
    [InlineData(8, "线尾上限开关")]
    [InlineData(9, "线头防呆光电")]
    public void Resource_mapping_contains_confirmed_input_names(int pointNo, string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "io-points.json");
        var map = new JsonIoPointMapProvider(path).Load();
        var point = map.Inputs.Single(point => point.PointNo == pointNo);

        Assert.Equal(name, point.Name);
        Assert.Equal(name, point.Description);
    }

    [Theory]
    [InlineData(17, 1, 0, "线头电缸上升")]
    [InlineData(18, 1, 1, "线头电缸上升")]
    [InlineData(19, 1, 2, "线头电缸下降")]
    [InlineData(20, 1, 3, "线头电缸下降")]
    [InlineData(21, 1, 4, "线尾电缸上升")]
    [InlineData(22, 1, 5, "线尾电缸上升")]
    [InlineData(23, 1, 6, "线尾电缸下降")]
    [InlineData(24, 1, 7, "线尾电缸下降")]
    [InlineData(25, 1, 8, "线头气缸伸出")]
    [InlineData(26, 1, 9, "线尾气缸伸出")]
    public void Resource_mapping_contains_extension1_output_action_names(int pointNo, int moduleIndex, int bitIndex, string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "io-points.json");
        var map = new JsonIoPointMapProvider(path).Load();
        var point = map.Outputs.Single(point => point.PointNo == pointNo);

        Assert.Equal(moduleIndex, point.ModuleIndex);
        Assert.Equal(bitIndex, point.BitIndex);
        Assert.Equal(name, point.Name);
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(16, 0, 15)]
    [InlineData(17, 1, 0)]
    [InlineData(32, 1, 15)]
    [InlineData(33, 2, 0)]
    [InlineData(40, 2, 7)]
    [InlineData(41, 3, 0)]
    [InlineData(56, 3, 15)]
    [InlineData(57, 4, 0)]
    [InlineData(64, 4, 7)]
    public void Default_mapping_matches_planned_module_split(int pointNo, int moduleIndex, int bitIndex)
    {
        var map = new JsonIoPointMapProvider("missing-io-points.json").Load();

        AssertPoint(map.Inputs.Single(point => point.PointNo == pointNo), IoType.Input, moduleIndex, bitIndex);
        AssertPoint(map.Outputs.Single(point => point.PointNo == pointNo), IoType.Output, moduleIndex, bitIndex);
    }

    [Theory]
    [InlineData(1, "X0", "Y0")]
    [InlineData(16, "X15", "Y15")]
    [InlineData(17, "X0", "Y0")]
    [InlineData(40, "X23", "Y23")]
    [InlineData(41, "X0", "Y0")]
    [InlineData(64, "X23", "Y23")]
    public void Default_mapping_names_points_from_zero_inside_each_physical_card(
        int pointNo,
        string inputName,
        string outputName)
    {
        var map = new JsonIoPointMapProvider("missing-io-points.json").Load();

        Assert.Equal(inputName, map.Inputs.Single(point => point.PointNo == pointNo).Name);
        Assert.Equal(outputName, map.Outputs.Single(point => point.PointNo == pointNo).Name);
    }

    private static void AssertPoint(IoPointDefinition point, IoType ioType, int moduleIndex, int bitIndex)
    {
        Assert.Equal(ioType, point.IoType);
        Assert.Equal(moduleIndex, point.ModuleIndex);
        Assert.Equal(bitIndex, point.BitIndex);
        Assert.True(point.IsEnabled);
        Assert.False(point.SafeDefaultValue);
    }
}
