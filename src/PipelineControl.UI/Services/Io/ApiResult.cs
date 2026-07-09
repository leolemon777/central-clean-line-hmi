namespace PipelineControl.UI.Services.Io;

public record ApiResult(int Code, string Message)
{
    public bool IsSuccess => Code == 0;

    public static ApiResult Ok(string message = "执行成功") => new(0, message);

    public static ApiResult Fail(int code, string? operation = null)
    {
        return new ApiResult(code, BuildMessage(code, operation));
    }

    public static ApiResult FromCode(int code, string? operation = null)
    {
        return code == 0 ? Ok(BuildMessage(code, operation)) : Fail(code, operation);
    }

    public static string BuildMessage(int code, string? operation = null)
    {
        var text = code switch
        {
            0 => "执行成功",
            1 => "执行失败",
            2 => "版本不支持该 API",
            7 => "参数错误",
            -1 => "通讯失败",
            -6 => "打开控制器失败",
            -7 => "控制器无响应",
            _ => $"未知错误码 {code}"
        };

        return string.IsNullOrWhiteSpace(operation) ? text : $"{operation}: {text}";
    }
}

public sealed record ApiResult<T>(int Code, string Message, T? Value) : ApiResult(Code, Message)
{
    public static ApiResult<T> Ok(T value, string message = "执行成功") => new(0, message, value);

    public static new ApiResult<T> Fail(int code, string? operation = null) => new(code, BuildMessage(code, operation), default);

    public static ApiResult<T> FromCode(int code, T? value, string? operation = null)
    {
        return code == 0 ? Ok(value!, BuildMessage(code, operation)) : Fail(code, operation);
    }
}
