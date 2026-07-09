using PipelineControl.UI.Services.Io.Drivers;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class MockIoBoardDriverTests
{
    [Fact]
    public void Output_write_can_be_read_back()
    {
        var driver = new MockIoBoardDriver();

        Assert.True(driver.Connect("127.0.0.1").IsSuccess);
        Assert.True(driver.WriteOutputBit(1, 3, true).IsSuccess);

        var read = driver.ReadOutputBit(1, 3);

        Assert.True(read.IsSuccess);
        Assert.True(read.Value);
    }

    [Fact]
    public void Read_inputs_requires_connection()
    {
        var driver = new MockIoBoardDriver();

        var result = driver.ReadAllInputs(0, 5);

        Assert.False(result.IsSuccess);
        Assert.Equal(-1, result.Code);
    }
}
