// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Authoring;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.Web.Http;
using WindowsRuntime.InteropServices;

namespace Preinitialization;

internal static class Program
{
    private static void Main()
    {
        // Actual CCW creation keeps the tables alive; typeof/GC.KeepAlive alone would not.
        RoundTrip(42);
        RoundTrip("preinitialization");
        RoundTrip(DateTimeOffset.UnixEpoch);
        RoundTrip(new Point(1, 2));
        RoundTrip(new EventHandler(static (_, _) => { }));
        RoundTrip(new PropertyChangedEventHandler(static (_, _) => { }));
        RoundTrip(WindowsRuntimeBuffer.Create(16));

        // SDK interfaces, delegates, enums, blittable structs, and non-blittable structs.
        RoundTrip(new BackgroundTask());
        RoundTrip(new WorkItemHandler(static _ => { }));
        RoundTrip(FileAttributes.Normal);
        RoundTrip(Color.FromArgb(255, 1, 2, 3));
        RoundTrip(default(HttpProgress));
        RoundTrip(new ValueConverter());

        // The same writer also runs at publish time for a third-party reference projection.
        RoundTrip(new Thermometer());
        RoundTrip(new TemperatureChangedHandler(static (_, _) => { }));
        RoundTrip(Season.Summer);
        RoundTrip(new Measurement { Value = 42, Season = Season.Summer });

        // Generic collection/async vtables, delegate and boxed-delegate tables, KVPs, and arrays.
        RoundTrip(new ObservableVector { 1, 2 });
        RoundTrip(new ObservableMap { ["key"] = 42 });
        RoundTrip(new MapChangedEventArgs());
        RoundTrip(new KeyValuePair<string, int>("key", 42));
        RoundTrip(new EventHandler<int>(static (_, _) => { }));
        RoundTrip(new EventHandler<object, string>(static (_, _) => { }));
        RoundTrip(new VectorChangedEventHandler<int>(static (_, _) => { }));
        RoundTrip(new MapChangedEventHandler<string, int>(static (_, _) => { }));
        RoundTrip(new[] { 1, 2 });
        RoundTrip(new[] { "one", "two" });
        RoundTrip(new[] { Color.FromArgb(255, 1, 2, 3) });
        RoundTrip(new HttpProgress[1]);
        RoundTrip(new[] { new KeyValuePair<string, int>("key", 42) });

        IAsyncAction action = AsyncInfo.Run(static _ => Task.CompletedTask);
        IAsyncOperation<int> operation = AsyncInfo.Run(static _ => Task.FromResult(42));
        IAsyncActionWithProgress<int> actionWithProgress = AsyncInfo.Run<int>(static (_, _) => Task.CompletedTask);
        IAsyncOperationWithProgress<string, int> operationWithProgress = AsyncInfo.Run<string, int>(static (_, _) => Task.FromResult("done"));

        RoundTrip(action);
        RoundTrip(operation);
        RoundTrip(actionWithProgress);
        RoundTrip(operationWithProgress);
        RoundTrip(new AsyncActionCompletedHandler(static (_, _) => { }));
        RoundTrip(new AsyncOperationCompletedHandler<int>(static (_, _) => { }));
        RoundTrip(new AsyncActionProgressHandler<int>(static (_, _) => { }));
        RoundTrip(new AsyncActionWithProgressCompletedHandler<int>(static (_, _) => { }));
        RoundTrip(new AsyncOperationProgressHandler<string, int>(static (_, _) => { }));
        RoundTrip(new AsyncOperationWithProgressCompletedHandler<string, int>(static (_, _) => { }));

        action.Close();
        operation.Close();
        actionWithProgress.Close();
        operationWithProgress.Close();

        // A deliberately non-preinitializable, unrelated .cctor must appear in MSTAT.
        Console.WriteLine($"Preinitialization smoke test completed ({RuntimeInitializedControl.Value}).");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void RoundTrip(object value)
    {
        void* pointer = WindowsRuntimeMarshal.ConvertToUnmanaged(value);

        try
        {
            if (pointer is null || !ReferenceEquals(value, WindowsRuntimeMarshal.ConvertToManaged(pointer)))
            {
                throw new InvalidOperationException($"CCW round trip failed for '{value.GetType()}'.");
            }
        }
        finally
        {
            WindowsRuntimeMarshal.Free(pointer);
        }
    }
}

internal static class RuntimeInitializedControl
{
    public static readonly int Value;

    static RuntimeInitializedControl()
    {
        Value = Environment.TickCount;
    }
}

internal sealed class BackgroundTask : IBackgroundTask
{
    public void Run(IBackgroundTaskInstance taskInstance) { }
}

internal sealed class Thermometer : IThermometer
{
    public int Temperature => 42;

    public void Reset() { }
}

internal sealed class ValueConverter : IValueConverter, INotifyPropertyChanged
{
    public object Convert(object value, Type targetType, object parameter, string language) => value;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => value;

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add { }
        remove { }
    }
}

internal sealed class ObservableVector : List<int>, IObservableVector<int>
{
    public event VectorChangedEventHandler<int> VectorChanged
    {
        add { }
        remove { }
    }
}

internal sealed class ObservableMap : Dictionary<string, int>, IObservableMap<string, int>
{
    public event MapChangedEventHandler<string, int> MapChanged
    {
        add { }
        remove { }
    }
}

internal sealed class MapChangedEventArgs : IMapChangedEventArgs<string>
{
    public CollectionChange CollectionChange => CollectionChange.ItemInserted;

    public string Key => "key";
}
