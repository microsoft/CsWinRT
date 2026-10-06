using System;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.Background;
using WindowsRuntime.InteropServices;

[assembly: global::System.Runtime.Versioning.SupportedOSPlatform("Windows")]

// 'ExeBackgroundTask' is only used by this executable, so the COM callable wrapper entries for its Windows
// Runtime interfaces are only in a 'WinRT.Interop.dll' generated with this executable as an input. Without
// them, the object can still be handed to native code, but it does not answer for 'IBackgroundTask'.
// Exit codes: 100 is success.
Guid IID_IBackgroundTask = new("7D13D534-FD12-43CE-8C22-EA1FF13C06DF");

unsafe
{
    void* ccw = WindowsRuntimeMarshal.ConvertToUnmanaged(new ExeBackgroundTask());

    try
    {
        if (Marshal.QueryInterface((nint)ccw, IID_IBackgroundTask, out nint backgroundTask) != 0)
        {
            return 101;
        }

        _ = Marshal.Release(backgroundTask);
    }
    finally
    {
        _ = Marshal.Release((nint)ccw);
    }
}

return 100;

/// <summary>
/// A user type implementing a Windows Runtime interface.
/// </summary>
internal sealed class ExeBackgroundTask : IBackgroundTask
{
    /// <inheritdoc/>
    public void Run(IBackgroundTaskInstance taskInstance)
    {
    }
}
