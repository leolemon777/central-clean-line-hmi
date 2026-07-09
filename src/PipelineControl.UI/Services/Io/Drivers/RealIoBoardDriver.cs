using System.IO;
using System.Reflection;
using System.Text;

namespace PipelineControl.UI.Services.Io.Drivers;

public sealed class RealIoBoardDriver : IIoBoardDriver, IConfigurableIoBoardDriver
{
    private readonly object syncRoot = new();
    private IoBoardConnectionOptions options = new();
    private object? card;
    private Type? cardType;
    private bool connected;

    public string DriverName => "Real MultiCardCS";

    public void Configure(IoBoardConnectionOptions options)
    {
        this.options = options;
    }

    public ApiResult Connect(string pcIp)
    {
        lock (syncRoot)
        {
            var loadResult = EnsureCardLoaded();
            if (!loadResult.IsSuccess)
            {
                return loadResult;
            }

            var result = InvokeAny(
                new[] { "GA_Open", "MC_Open" },
                (short)1,
                string.IsNullOrWhiteSpace(pcIp) ? options.PcIp : pcIp,
                options.PcPort,
                options.MainCardIp,
                options.MainCardPort);

            connected = result.IsSuccess;
            return result;
        }
    }

    public ApiResult Disconnect()
    {
        lock (syncRoot)
        {
            if (card is null)
            {
                connected = false;
                return ApiResult.Ok();
            }

            var result = InvokeAny(new[] { "GA_Close", "MC_Close" });
            connected = false;
            return result;
        }
    }

    public ApiResult Reset()
    {
        lock (syncRoot)
        {
            return connected ? InvokeAny(new[] { "GA_Reset", "MC_Reset" }) : ApiResult.Fail(-1, "Reset");
        }
    }

    public ApiResult<bool> ReadInputBit(int moduleIndex, int bitIndex)
    {
        var images = ReadAllInputs(moduleIndex, 1);
        if (!images.IsSuccess || images.Value is null)
        {
            return ApiResult<bool>.Fail(images.Code, "ReadInputBit");
        }

        return ApiResult<bool>.Ok(images.Value[0].GetBit(bitIndex));
    }

    public ApiResult WriteOutputBit(int moduleIndex, int bitIndex, bool value)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult.Fail(-1, "WriteOutputBit");
            }

            return InvokeAny(new[] { "GA_SetExtDoBit", "MC_SetExtDoBit" }, (short)moduleIndex, (short)bitIndex, (short)(value ? 1 : 0));
        }
    }

    public ApiResult WriteOutputModule(int moduleIndex, int value)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult.Fail(-1, "WriteOutputModule");
            }

            var args = new object[] { (short)moduleIndex, value & 0xFFFF, (short)1 };
            return InvokeAny(new[] { "GA_SetExtDoValue", "MC_SetExtDoValue" }, args);
        }
    }

    public ApiResult<bool> ReadOutputBit(int moduleIndex, int bitIndex)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult<bool>.Fail(-1, "ReadOutputBit");
            }

            var args = new object[] { (short)moduleIndex, (short)bitIndex, (short)0 };
            var result = InvokeAny(new[] { "GA_GetExtDoBit", "MC_GetExtDoBit" }, args);
            return result.IsSuccess ? ApiResult<bool>.Ok(Convert.ToInt16(args[2]) != 0) : ApiResult<bool>.Fail(result.Code, "ReadOutputBit");
        }
    }

    public ApiResult<IReadOnlyList<IoModuleImage>> ReadAllInputs(int startModuleIndex, int moduleCount)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult<IReadOnlyList<IoModuleImage>>.Fail(-1, "ReadAllInputs");
            }

            var images = new List<IoModuleImage>();
            for (var module = startModuleIndex; module < startModuleIndex + moduleCount; module++)
            {
                var args = new object[] { (short)module, 0, (short)1 };
                var result = InvokeAny(new[] { "GA_GetExtDiValue", "MC_GetExtDiValue" }, args);
                if (!result.IsSuccess)
                {
                    return ApiResult<IReadOnlyList<IoModuleImage>>.Fail(result.Code, "ReadAllInputs");
                }

                images.Add(new IoModuleImage(module, Convert.ToInt32(args[1])));
            }

            return ApiResult<IReadOnlyList<IoModuleImage>>.Ok(images);
        }
    }

    public ApiResult<IReadOnlyList<IoModuleImage>> ReadAllOutputs(int startModuleIndex, int moduleCount)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult<IReadOnlyList<IoModuleImage>>.Fail(-1, "ReadAllOutputs");
            }

            var images = new List<IoModuleImage>();
            for (var module = startModuleIndex; module < startModuleIndex + moduleCount; module++)
            {
                var args = new object[] { (short)module, 0, (short)1 };
                var result = InvokeAny(new[] { "GA_GetExtDoValue", "MC_GetExtDoValue" }, args);
                if (!result.IsSuccess)
                {
                    return ReadOutputModuleByBits(module, images);
                }

                images.Add(new IoModuleImage(module, Convert.ToInt32(args[1])));
            }

            return ApiResult<IReadOnlyList<IoModuleImage>>.Ok(images);
        }
    }

    public ApiResult<double> ReadAdcVoltage(int channel)
    {
        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult<double>.Fail(-1, "ReadAdcVoltage");
            }

            // 手册标注 0-10000 对应 0-10V；厂家托管签名为 GA_GetAdc(short, ref short, short, ref int)。
            var args = new object[] { (short)channel, (short)0, (short)1, 0 };
            var result = InvokeAny(new[] { "GA_GetAdc", "MC_GetAdc" }, args);
            if (!result.IsSuccess)
            {
                return ApiResult<double>.Fail(result.Code, "ReadAdcVoltage");
            }

            return ApiResult<double>.Ok(Convert.ToInt16(args[1]) / 1000.0);
        }
    }

    public ApiResult WriteDacVoltage(int channel, double voltage)
    {
        if (voltage is < 0 or > 10)
        {
            return ApiResult.Fail(7, "WriteDacVoltage");
        }

        lock (syncRoot)
        {
            if (!connected)
            {
                return ApiResult.Fail(-1, "WriteDacVoltage");
            }

            var raw = (short)Math.Clamp((int)Math.Round(voltage * 1000), 0, 10000);
            var args = new object[] { (short)channel, raw, (short)1 };
            return InvokeAny(new[] { "GA_SetDac", "MC_SetDac" }, args);
        }
    }

    private ApiResult<IReadOnlyList<IoModuleImage>> ReadOutputModuleByBits(int module, List<IoModuleImage> images)
    {
        var value = 0;
        for (var bit = 0; bit < 16; bit++)
        {
            var args = new object[] { (short)module, (short)bit, (short)0 };
            var result = InvokeAny(new[] { "GA_GetExtDoBit", "MC_GetExtDoBit" }, args);
            if (!result.IsSuccess)
            {
                return ApiResult<IReadOnlyList<IoModuleImage>>.Fail(result.Code, "ReadAllOutputs");
            }

            if (Convert.ToInt16(args[2]) != 0)
            {
                value |= 1 << bit;
            }
        }

        images.Add(new IoModuleImage(module, value));
        return ApiResult<IReadOnlyList<IoModuleImage>>.Ok(images);
    }

    private ApiResult EnsureCardLoaded()
    {
        if (card is not null && cardType is not null)
        {
            return ApiResult.Ok();
        }

        var dllPath = Path.Combine(AppContext.BaseDirectory, "MultiCardCS.dll");
        if (!File.Exists(dllPath))
        {
            return ApiResult.Fail(-6, $"加载 MultiCardCS.dll ({dllPath})");
        }

        try
        {
            var assembly = Assembly.LoadFrom(dllPath);
            cardType = assembly.GetType("MultiCardCS.MultiCardCS", throwOnError: true);
            card = Activator.CreateInstance(cardType!);
            return card is null ? ApiResult.Fail(-6, "创建 MultiCardCS.MultiCardCS") : ApiResult.Ok();
        }
        catch (Exception ex)
        {
            return new ApiResult(-6, BuildLoadFailureMessage(ex));
        }
    }

    private static string BuildLoadFailureMessage(Exception exception)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var builder = new StringBuilder("加载厂家 DLL 失败");
        builder.Append("；").Append(BuildDriverFileStatus(baseDirectory));
        builder.Append("；").Append(DescribeException(exception));
        builder.Append("；提示：MultiCardCLR.dll 依赖 VC++ 2010 x64 运行库 MSVCR100.dll，请确认完整复制部署包或安装厂家要求的 64 位运行库/驱动。");
        return builder.ToString();
    }

    private static string BuildDriverFileStatus(string baseDirectory)
    {
        var names = new[]
        {
            "MultiCardCS.dll",
            "MultiCardCLR.dll",
            "MultiCard.dll",
            "msvcr100.dll"
        };

        return "当前目录文件状态：" + string.Join(
            "，",
            names.Select(name => $"{name}={(File.Exists(Path.Combine(baseDirectory, name)) ? "存在" : "缺失")}"));
    }

    private static string DescribeException(Exception exception)
    {
        var builder = new StringBuilder();
        AppendException(builder, exception);

        if (exception is ReflectionTypeLoadException typeLoadException)
        {
            foreach (var loaderException in typeLoadException.LoaderExceptions.Where(ex => ex is not null))
            {
                builder.Append("；LoaderException: ");
                AppendException(builder, loaderException!);
            }
        }

        return builder.ToString();
    }

    private static void AppendException(StringBuilder builder, Exception exception)
    {
        builder.Append(exception.GetType().Name)
            .Append(" HResult=0x")
            .Append(exception.HResult.ToString("X8"))
            .Append(": ")
            .Append(exception.Message);

        if (exception.InnerException is not null)
        {
            builder.Append("；Inner: ");
            AppendException(builder, exception.InnerException);
        }
    }

    private ApiResult InvokeAny(IEnumerable<string> methodNames, params object[] args)
    {
        if (card is null || cardType is null)
        {
            return ApiResult.Fail(-6, "厂家 DLL 未加载");
        }

        foreach (var methodName in methodNames)
        {
            var method = cardType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method is null)
            {
                continue;
            }

            try
            {
                var code = Convert.ToInt32(method.Invoke(card, args));
                return ApiResult.FromCode(code, methodName);
            }
            catch (TargetInvocationException ex)
            {
                return new ApiResult(-1, $"{methodName}: {DescribeException(ex.InnerException ?? ex)}");
            }
            catch (Exception ex)
            {
                return new ApiResult(-1, $"{methodName}: {DescribeException(ex)}");
            }
        }

        return ApiResult.Fail(2, string.Join("/", methodNames));
    }
}
