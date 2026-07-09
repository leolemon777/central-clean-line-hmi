using PipelineControl.UI.Services.Io;
using Xunit;

namespace PipelineControl.UI.Tests;

public sealed class IoApiResultTests
{
    [Theory]
    [InlineData(0, "执行成功")]
    [InlineData(1, "执行失败")]
    [InlineData(2, "版本不支持该 API")]
    [InlineData(7, "参数错误")]
    [InlineData(-1, "通讯失败")]
    [InlineData(-6, "打开控制器失败")]
    [InlineData(-7, "控制器无响应")]
    public void Error_code_mapping_is_centralized(int code, string expected)
    {
        var result = ApiResult.FromCode(code, "测试");

        Assert.Equal(code == 0, result.IsSuccess);
        Assert.Contains(expected, result.Message, StringComparison.Ordinal);
    }
}
