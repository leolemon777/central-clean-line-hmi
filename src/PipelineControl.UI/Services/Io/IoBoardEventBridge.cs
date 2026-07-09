using PipelineControl.UI.Services.Logs;
using System.Diagnostics;

namespace PipelineControl.UI.Services.Io;

public sealed class IoBoardEventBridge : IDisposable
{
    private readonly IoBoardService ioBoardService;
    private readonly IAppLogService appLogService;
    private bool disposed;

    public IoBoardEventBridge(
        IoBoardService ioBoardService,
        IAppLogService appLogService)
    {
        this.ioBoardService = ioBoardService;
        this.appLogService = appLogService;

        ioBoardService.LogAppended += OnLogAppended;
        ioBoardService.AlarmRaised += OnAlarmRaised;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        ioBoardService.LogAppended -= OnLogAppended;
        ioBoardService.AlarmRaised -= OnAlarmRaised;
        disposed = true;
    }

    private void OnLogAppended(object? sender, IoBoardLogEntry entry)
    {
        FireAndForget(() => appLogService.WriteAsync(new AppLogEntry
        {
            Timestamp = entry.Time,
            Level = ParseLevel(entry.Level),
            Category = ResolveCategory(entry.Message),
            Message = entry.Message,
            Source = nameof(IoBoardService),
            Target = "IO",
            Details = entry.Message,
            Command = ResolveCommand(entry.Message)
        }));
    }

    private void OnAlarmRaised(object? sender, IoBoardAlarm alarm)
    {
        FireAndForget(() => appLogService.WriteAsync(new AppLogEntry
        {
            Timestamp = alarm.Time,
            Level = LogLevelKind.Error,
            Category = LogCategory.Runtime,
            Message = $"IO 异常: {alarm.Message}",
            Source = nameof(IoBoardService),
            Target = "IO",
            Details = alarm.Message,
            StatusCode = alarm.Code
        }));
    }

    private static void FireAndForget(Func<Task> action)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"IO event bridge failed: {ex}");
            }
        });
    }

    private static LogLevelKind ParseLevel(string level)
    {
        return level.Trim().ToUpperInvariant() switch
        {
            "DEBUG" or "DBG" => LogLevelKind.Debug,
            "WARN" or "WARNING" or "WRN" => LogLevelKind.Warn,
            "ERROR" or "ERR" => LogLevelKind.Error,
            _ => LogLevelKind.Info
        };
    }

    private static LogCategory ResolveCategory(string message)
    {
        if (message.Contains("输出", StringComparison.Ordinal) || message.Contains("复位", StringComparison.Ordinal))
        {
            return LogCategory.Operation;
        }

        if (message.Contains("通讯", StringComparison.Ordinal) || message.Contains("连接", StringComparison.Ordinal))
        {
            return LogCategory.Communication;
        }

        return LogCategory.Runtime;
    }

    private static string ResolveCommand(string message)
    {
        if (message.Contains("输出", StringComparison.Ordinal))
        {
            return "WRITE_DO";
        }

        if (message.Contains("轮询", StringComparison.Ordinal))
        {
            return "POLL_DI";
        }

        if (message.Contains("复位", StringComparison.Ordinal))
        {
            return "RESET";
        }

        if (message.Contains("连接", StringComparison.Ordinal))
        {
            return "CONNECT";
        }

        return string.Empty;
    }
}
