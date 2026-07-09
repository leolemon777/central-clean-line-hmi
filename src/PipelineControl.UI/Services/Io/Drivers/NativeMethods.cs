using System.Runtime.InteropServices;

namespace PipelineControl.UI.Services.Io.Drivers;

internal static partial class NativeMethods
{
    internal const string NativeDllName = "MultiCard.dll";

    // 厂家当前 C# Demo 走 MultiCardCS.MultiCardCS 托管包装，真实驱动优先使用托管包装。
    // 如果后续厂家提供稳定的 C ABI，再把 DllImport 集中补到本类，避免 P/Invoke 分散到业务层。
    [DllImport(NativeDllName, EntryPoint = "MC_Open")]
    internal static extern int McOpenPlaceholder();
}
