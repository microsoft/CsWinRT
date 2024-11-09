using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Input;
using TestComponentCSharp;
using Windows.Foundation;
using Windows.Web.Http;
using WinRT;
using WinRT.Interop;

[assembly: WinRT.GeneratedWinRTExposedExternalType(typeof(int[]))]
[assembly: GeneratedWinRTExposedExternalType(typeof(Dictionary<string, CancellationTokenSource>))]
[assembly: GeneratedWinRTExposedExternalTypeAttribute(typeof(AsyncActionProgressHandler<HttpProgress>))]

Class instance = new Class();
var expected = new int[] { 0, 1, 2 };
instance.BindableIterableProperty = expected;
if (expected != instance.BindableIterableProperty)
{
    return 101;
}

var expected2 = new double[] { 0, 1, 2 };
try
{
    instance.BindableIterableProperty = expected2;
    // Shouldn't reach here as we didn't opt-in double[].
    return 101;
}
catch (Exception)
{
}

var cancellationDictionary = new Dictionary<string, CancellationTokenSource>();
instance.BindableIterableProperty = cancellationDictionary;
if (cancellationDictionary != instance.BindableIterableProperty)
{
    return 101;
}

var customCommand = new CustomCommand() as ICommand;
var ccw = MarshalInspectable<object>.CreateMarshaler(customCommand);
ccw.TryAs<IUnknownVftbl>(ABI.System.Windows.Input.ICommandMethods.IID, out var commandCCW);
if (commandCCW == null)
{
    return 101;
}

// Didn't opt-in
var customCommand2 = new CustomCommand2() as ICommand;
ccw = MarshalInspectable<object>.CreateMarshaler(customCommand2);
ccw.TryAs<IUnknownVftbl>(ABI.System.Windows.Input.ICommandMethods.IID, out commandCCW);
if (commandCCW != null)
{
    return 101;
}

instance.IntProperty = 12;
var async_get_int = instance.GetIntAsync();
int async_int = 0;
async_get_int.Completed = (info, status) => async_int = info.GetResults();
async_get_int.GetResults();
if (async_int != 12)
{
    return 101;
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
    return 101;
}

// Ensure we can use the IProperties interface from the native side.
var managedProperties = new ManagedProperties(42);
instance.CopyProperties(managedProperties);
if (managedProperties.ReadWriteProperty != instance.ReadWriteProperty)
{
    return 101;
}

return 100;

[GeneratedWinRTExposedType]
sealed partial class CustomCommand : ICommand
{
    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
        throw new NotImplementedException();
    }

    public void Execute(object parameter)
    {
        throw new NotImplementedException();
    }
}

sealed partial class CustomCommand2 : ICommand
{
    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter)
    {
        throw new NotImplementedException();
    }

    public void Execute(object parameter)
    {
        throw new NotImplementedException();
    }
}

[global::WinRT.GeneratedWinRTExposedTypeAttribute]
sealed partial class ManagedProperties : IProperties1, IUriHandler
{
    private readonly int _value;

    public ManagedProperties(int value)
    {
        _value = value;
    }

    public int ReadWriteProperty => _value;

    public void AddUriHandler(ProvideUri provideUri)
    {
        _ = provideUri();
    }

    void IUriHandler.AddUriHandler(ProvideUri provideUri) => AddUriHandler(provideUri);
}
