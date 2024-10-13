using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TestComponentCSharp;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.Web.Http;
using WinRT;

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
    handle = WindowsRuntimeStorageExtensions.CreateSafeFileHandle(s.GetResults(), "Async.pdb", FileMode.Open, FileAccess.Read);
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

using var fileStream = File.OpenRead(folderPath + "\\Async.exe");
var randomAccessStream = fileStream.AsRandomAccessStream();
var ptr = MarshalInterface<IRandomAccessStream>.FromManaged(randomAccessStream);
if (ptr == IntPtr.Zero)
{
    return 110;
}
var arr = new byte[100];
var buffer = arr.AsBuffer();
ptr = MarshalInterface<IBuffer>.FromManaged(buffer);
if (ptr == IntPtr.Zero)
{
    return 111;
}

var asyncOperation = randomAccessStream.ReadAsync(buffer, 50, InputStreamOptions.Partial);
ptr = MarshalInterface<IAsyncOperationWithProgress<IBuffer, uint>>.FromManaged(asyncOperation);
if (ptr == IntPtr.Zero)
{
    return 112;
}

ptr = MarshalInterface<IAsyncInfo>.FromManaged(asyncOperation);
if (ptr == IntPtr.Zero)
{
    return 113;
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
    return 114;
}

return 100;

static async Task<int> InvokeAddAsync(Class instance, int lhs, int rhs)
{
    return await instance.AddAsync(lhs, rhs);
}