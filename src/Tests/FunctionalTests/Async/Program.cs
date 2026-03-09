using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TestComponentCSharp;
using Windows.Foundation;
using Windows.Foundation.Tasks;
using Windows.Storage.Buffers;
using Windows.Storage.IO;
using Windows.Storage.Streams;
using Windows.Web.Http;
using WindowsRuntime.InteropServices;

var instance = new Class();

instance.IntProperty = 12;
var async_get_int = instance.GetIntAsync();
int async_int = 0;
async_get_int.Completed = (info, status) => async_int = info.GetResults();
async_get_int.GetResults();

if (async_int != 12)
{
    return 101;
}

instance.StringProperty = "foo";
var async_get_string = instance.GetStringAsync();
string async_string = "";
async_get_string.Completed = (info, status) => async_string = info.GetResults();
int async_progress;
async_get_string.Progress = (info, progress) => async_progress = progress;
async_get_string.GetResults();

if (async_string != "foo")
{
    return 102;
}

var task = InvokeAddAsync(instance, 20, 10);
if (task.Wait(25))
{
    return 103;
}

instance.CompleteAsync();
if (!task.Wait(1000))
{
    return 104;
}

if (task.Status != TaskStatus.RanToCompletion || task.Result != 30)
{
    return 105;
}

var ports = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
    Windows.Devices.SerialCommunication.SerialDevice.GetDeviceSelector(),
    new string[] { "System.ItemNameDisplay" });
foreach (var port in ports)
{
    object o = port.Properties["System.ItemNameDisplay"];
    if (o is null)
    {
        return 106;
    }
}

var folderPath = Path.GetDirectoryName(AppContext.BaseDirectory);
var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(folderPath + "\\Async.exe");
var handle = WindowsRuntimeStorageExtensions.CreateSafeFileHandle(file, FileAccess.Read);
if (handle is null)
{
    return 107;
}
handle.Close();

handle = null;
var getFolderFromPathAsync = Windows.Storage.StorageFolder.GetFolderFromPathAsync(folderPath);
getFolderFromPathAsync.Completed += (s, e) =>
{
    handle = WindowsRuntimeStorageExtensions.CreateSafeFileHandle(s.GetResults(), "TestComponent.dll", FileMode.Open, FileAccess.Read);
};
await Task.Delay(1000);
if (handle is null)
{
    return 108;
}
handle.Close();

using var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read).AsTask().ConfigureAwait(continueOnCapturedContext: false);
using var sw = new StreamReader(stream.AsStreamForRead());
if (string.IsNullOrEmpty(sw.ReadToEnd()))
{
    return 109;
}

unsafe
{
    using var fileStream = File.OpenRead(folderPath + "\\Async.exe");
    var randomAccessStream = fileStream.AsRandomAccessStream();
    var ptr = WindowsRuntimeMarshal.ConvertToUnmanaged(randomAccessStream);
    if (ptr is null)
    {
        return 110;
    }

    if (Marshal.QueryInterface((nint)ptr, typeof(IRandomAccessStream).GUID, out var ptr2) != 0 ||
        ptr2 == IntPtr.Zero)
    {
        return 111;
    }

    // Test WindowsRuntimeExternalArrayBuffer CCW (created via AsBuffer())
    var arr = new byte[100];
    var buffer = arr.AsBuffer();
    ptr = WindowsRuntimeMarshal.ConvertToUnmanaged(buffer);
    if (ptr is null)
    {
        return 112;
    }

    if (Marshal.QueryInterface((nint)ptr, typeof(IBuffer).GUID, out ptr2) != 0 ||
        ptr2 == IntPtr.Zero)
    {
        return 113;
    }

    // Test WindowsRuntimePinnedArrayBuffer CCW (created via WindowsRuntimeBuffer.Create())
    var pinnedBuffer = WindowsRuntimeBuffer.Create(100);
    ptr = WindowsRuntimeMarshal.ConvertToUnmanaged(pinnedBuffer);
    if (ptr is null)
    {
        return 128;
    }

    if (Marshal.QueryInterface((nint)ptr, typeof(IBuffer).GUID, out ptr2) != 0 ||
        ptr2 == IntPtr.Zero)
    {
        return 129;
    }

    var asyncOperation = randomAccessStream.ReadAsync(buffer, 50, InputStreamOptions.Partial);
    ptr = WindowsRuntimeMarshal.ConvertToUnmanaged(asyncOperation);
    if (ptr is null)
    {
        return 114;
    }

    // IID for IAsyncOperationWithProgress<IBuffer, uint>
    Guid IID_IAsyncOperationWithProgress = new("d26b2819-897f-5c7d-84d6-56d796561431");
    if (Marshal.QueryInterface((nint)ptr, IID_IAsyncOperationWithProgress, out ptr2) != 0 ||
        ptr2 == IntPtr.Zero)
    {
        return 115;
    }

    Guid IID_IAsyncInfo = new("00000036-0000-0000-C000-000000000046");
    if (Marshal.QueryInterface((nint)ptr, IID_IAsyncInfo, out ptr2) != 0 ||
        ptr2 == IntPtr.Zero)
    {
        return 116;
    }
}

bool progressCalledWithExpectedResults = false;
var asyncProgressHandler = new AsyncActionProgressHandler<HttpProgress>((info, progress) =>
{
    if (progress.BytesReceived == 3 && progress.TotalBytesToReceive == 4)
    {
        progressCalledWithExpectedResults = true;
    }
});
Class.UnboxAndCallProgressHandler(asyncProgressHandler);
if (!progressCalledWithExpectedResults)
{
    return 117;
}

// Test stream adapter span/memory overrides using InMemoryRandomAccessStream
{
    var random = new Random(42);
    byte[] data = new byte[256];
    random.NextBytes(data);

    using var adaptedStream = new InMemoryRandomAccessStream().AsStream();

    // Test Write(ReadOnlySpan<byte>) and Read(Span<byte>)
    adaptedStream.Write(new ReadOnlySpan<byte>(data));
    adaptedStream.Seek(0, SeekOrigin.Begin);

    Span<byte> spanRead = new byte[256];
    int spanBytesRead = adaptedStream.Read(spanRead);

    if (spanBytesRead != 256)
    {
        return 118;
    }

    if (!data.SequenceEqual(spanRead))
    {
        return 119;
    }

    // Test WriteAsync(ReadOnlyMemory<byte>) and ReadAsync(Memory<byte>)
    adaptedStream.Seek(0, SeekOrigin.Begin);
    await adaptedStream.WriteAsync(new ReadOnlyMemory<byte>(data));
    adaptedStream.Seek(0, SeekOrigin.Begin);

    Memory<byte> memoryRead = new byte[256];
    int memoryBytesRead = await adaptedStream.ReadAsync(memoryRead);

    if (memoryBytesRead != 256)
    {
        return 120;
    }

    if (!data.SequenceEqual(memoryRead.Span))
    {
        return 121;
    }

    // Test ReadByte/WriteByte (which delegate to span overrides)
    adaptedStream.Seek(0, SeekOrigin.Begin);
    adaptedStream.WriteByte(0xAB);
    adaptedStream.WriteByte(0xCD);
    adaptedStream.Seek(0, SeekOrigin.Begin);

    if (adaptedStream.ReadByte() != 0xAB)
    {
        return 122;
    }

    if (adaptedStream.ReadByte() != 0xCD)
    {
        return 123;
    }

    // Test empty span/memory operations
    if (adaptedStream.Read(Span<byte>.Empty) != 0)
    {
        return 124;
    }

    adaptedStream.Write(ReadOnlySpan<byte>.Empty);

    if (await adaptedStream.ReadAsync(Memory<byte>.Empty) != 0)
    {
        return 125;
    }

    await adaptedStream.WriteAsync(ReadOnlyMemory<byte>.Empty);

    // Test cancellation for memory-based async operations
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    try
    {
        _ = await adaptedStream.ReadAsync(new byte[256].AsMemory(), cts.Token);
        return 126;
    }
    catch (OperationCanceledException)
    {
    }

    try
    {
        await adaptedStream.WriteAsync(new byte[256].AsMemory(), cts.Token);
        return 127;
    }
    catch (OperationCanceledException)
    {
    }
}

// Test writing each managed buffer type to a native WinRT stream (exercises CCW interop)
{
    byte[] testData = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

    // Test WindowsRuntimeExternalArrayBuffer (from AsBuffer()) written to native stream
    using var stream1 = new InMemoryRandomAccessStream();
    IBuffer externalArrayBuffer = testData.AsBuffer();
    await stream1.WriteAsync(externalArrayBuffer);
    stream1.Seek(0);

    byte[] read1 = new byte[8];
    IBuffer readBuffer1 = read1.AsBuffer();
    await stream1.ReadAsync(readBuffer1, 8, InputStreamOptions.None);
    if (!testData.SequenceEqual(read1))
    {
        return 130;
    }

    // Test WindowsRuntimePinnedArrayBuffer (from WindowsRuntimeBuffer.Create()) written to native stream
    using var stream2 = new InMemoryRandomAccessStream();
    IBuffer pinnedArrayBuffer = WindowsRuntimeBuffer.Create(testData);
    await stream2.WriteAsync(pinnedArrayBuffer);
    stream2.Seek(0);

    byte[] read2 = new byte[8];
    IBuffer readBuffer2 = read2.AsBuffer();
    await stream2.ReadAsync(readBuffer2, 8, InputStreamOptions.None);
    if (!testData.SequenceEqual(read2))
    {
        return 131;
    }

    // Test WindowsRuntimePinnedMemoryBuffer (created internally by the stream adapter when
    // using span/memory-based Write, which pins the data and wraps it in a PinnedMemoryBuffer
    // before passing it as an IBuffer CCW to the native WinRT stream's WriteAsync)
    using var stream3 = new InMemoryRandomAccessStream();
    using var adaptedStream3 = stream3.AsStream();
    adaptedStream3.Write(new ReadOnlySpan<byte>(testData));

    stream3.Seek(0);

    byte[] read3 = new byte[8];
    IBuffer readBuffer3 = read3.AsBuffer();
    await stream3.ReadAsync(readBuffer3, 8, InputStreamOptions.None);
    if (!testData.SequenceEqual(read3))
    {
        return 132;
    }
}

return 100;

static async Task<int> InvokeAddAsync(Class instance, int lhs, int rhs)
{
    return await instance.AddAsync(lhs, rhs);
}