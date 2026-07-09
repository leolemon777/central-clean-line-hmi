using System.Windows.Threading;

namespace PipelineControl.UI.Tests;

internal static class WpfTestPump
{
    private static readonly Lazy<Dispatcher> SharedDispatcher = new(CreateSharedDispatcher);

    public static void EnsureDispatcherContext()
    {
        if (SynchronizationContext.Current is not DispatcherSynchronizationContext)
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        }
    }

    public static void Run(Task task)
    {
        EnsureDispatcherContext();

        if (!task.IsCompleted)
        {
            var frame = new DispatcherFrame();
            task.ContinueWith(
                _ => frame.Continue = false,
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.PushFrame(frame);
        }

        if (task.IsFaulted)
        {
            throw task.Exception?.GetBaseException() ?? new InvalidOperationException("异步初始化失败。");
        }

        if (task.IsCanceled)
        {
            throw new OperationCanceledException("异步初始化已取消。");
        }
    }

    public static void RunOnWpfThread(Action action)
    {
        SharedDispatcher.Value.Invoke(() =>
        {
            EnsureDispatcherContext();
            EnsureApplicationResources();
            action();
        });
    }

    private static Dispatcher CreateSharedDispatcher()
    {
        Dispatcher? dispatcher = null;
        Exception? startupError = null;
        using var ready = new ManualResetEventSlim();

        var thread = new Thread(() =>
        {
            try
            {
                EnsureDispatcherContext();
                EnsureApplicationResources();
                dispatcher = Dispatcher.CurrentDispatcher;
            }
            catch (Exception ex)
            {
                startupError = ex;
            }
            finally
            {
                ready.Set();
            }

            if (startupError is null)
            {
                Dispatcher.Run();
            }
        })
        {
            IsBackground = true,
            Name = "PipelineControl.WpfTests"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        ready.Wait();

        if (startupError is not null)
        {
            throw startupError;
        }

        return dispatcher ?? throw new InvalidOperationException("WPF test dispatcher was not created.");
    }

    private static void EnsureApplicationResources()
    {
        if (System.Windows.Application.Current is not null)
        {
            return;
        }

        var app = new PipelineControl.UI.App
        {
            ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
        };
        app.InitializeComponent();
    }
}
