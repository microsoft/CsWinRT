// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ForwardedTypeIdentitiesLibrary;
using ForwardedTypeIdentitiesNetStandard;
using Windows.Foundation;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

try
{
    // Duplicate dictionary identities must not invalidate the entire CCW map, including unrelated types.
    StringableProbe unrelated = new("unrelated callback");

    if (!CheckStringable(unrelated))
    {
        return Fail(101, "IStringable before dictionary callbacks");
    }

    DisposableProbe disposable = new();

    if (!CheckDisposable(disposable))
    {
        return Fail(109, "netstandard2.0 IDisposable before dictionary callbacks");
    }

    StringableProbe objectValue = new("object dictionary value");
    const string stringValue = "string dictionary value";

    // These constructions use System.Runtime; the helpers use System.ObjectModel and netstandard for the same types.
    ReadOnlyDictionary<string, object> currentObjects = new(new Dictionary<string, object> { ["key"] = objectValue });
    ReadOnlyDictionary<string, string> currentStrings = new(new Dictionary<string, string> { ["key"] = stringValue });

    if (!CheckDictionary(LegacyDictionaries.CreateObjects(objectValue), false, objectValue))
    {
        return Fail(102, "netstandard1.3 ReadOnlyDictionary<string, object>");
    }

    if (!CheckDictionary(currentObjects, false, objectValue))
    {
        return Fail(103, "net10.0 ReadOnlyDictionary<string, object>");
    }

    if (!CheckDictionary(LegacyDictionaries.CreateStrings(stringValue), true, stringValue))
    {
        return Fail(104, "netstandard1.3 ReadOnlyDictionary<string, string>");
    }

    if (!CheckDictionary(currentStrings, true, stringValue))
    {
        return Fail(105, "net10.0 ReadOnlyDictionary<string, string>");
    }

    if (!CheckDictionary(StandardDictionaries.CreateObjects(objectValue), false, objectValue))
    {
        return Fail(110, "netstandard2.0 ReadOnlyDictionary<string, object>");
    }

    if (!CheckDictionary(StandardDictionaries.CreateStrings(stringValue), true, stringValue))
    {
        return Fail(111, "netstandard2.0 ReadOnlyDictionary<string, string>");
    }

    if (!CheckGetView(new MapOnlyDictionary<object>(objectValue), false, objectValue))
    {
        return Fail(106, "IMap<string, object>.GetView fallback");
    }

    if (!CheckGetView(new MapOnlyDictionary<string>(stringValue), true, stringValue))
    {
        return Fail(107, "IMap<string, string>.GetView fallback");
    }

    if (!CheckStringable(unrelated) || unrelated.CallCount != 2)
    {
        return Fail(108, "IStringable after dictionary callbacks");
    }

    if (!CheckDisposable(disposable) || disposable.DisposeCount != 2)
    {
        return Fail(112, "netstandard2.0 IDisposable after dictionary callbacks");
    }

    return 100;
}
catch (Exception e)
{
    Console.Error.WriteLine(e);

    return 199;
}

static int Fail(int exitCode, string scenario)
{
    Console.Error.WriteLine($"Incorrect forwarded type marshalling: {scenario}.");

    return exitCode;
}

static unsafe bool CheckDictionary(object dictionary, bool stringValues, object expectedValue)
{
    void* ccw = WindowsRuntimeMarshal.ConvertToUnmanaged(dictionary);

    try
    {
        return CheckMapView(ccw, stringValues, expectedValue, dictionary);
    }
    finally
    {
        WindowsRuntimeMarshal.Free(ccw);
    }
}

static unsafe bool CheckMapView(void* unknown, bool stringValues, object expectedValue, object expectedDictionary)
{
    nint view = 0;
    nint unexpected = 0;
    void* key = null;
    void* result = null;

    try
    {
        Guid expectedIid = stringValues ? InterfaceIds.StringMapView : InterfaceIds.ObjectMapView;
        Guid unexpectedIid = stringValues ? InterfaceIds.ObjectMapView : InterfaceIds.StringMapView;

        Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)unknown, expectedIid, out view));

        if (view == 0 ||
            !WindowsRuntimeMarshal.TryGetManagedObject((void*)view, out object? managedView) ||
            !ReferenceEquals(managedView, expectedDictionary))
        {
            return false;
        }

        if (Marshal.QueryInterface((nint)unknown, unexpectedIid, out unexpected) != unchecked((int)0x80004002) ||
            unexpected != 0)
        {
            return false;
        }

        void** vtable = *(void***)view;
        uint size = 0;
        byte hasKey = 0;

        // IMapView<K, V>: Lookup (6), Size (7), HasKey (8).
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, uint*, int>)vtable[7])((void*)view, &size));

        key = HStringMarshaller.ConvertToUnmanaged("key");

        Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void*, byte*, int>)vtable[8])((void*)view, key, &hasKey));
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void*, void**, int>)vtable[6])((void*)view, key, &result));

        if (size != 1 || hasKey != 1)
        {
            return false;
        }

        if (stringValues)
        {
            return HStringMarshaller.ConvertToManaged(result) == (string)expectedValue;
        }

        return WindowsRuntimeMarshal.TryGetManagedObject(result, out object? managedValue) &&
            ReferenceEquals(managedValue, expectedValue);
    }
    finally
    {
        if (stringValues)
        {
            HStringMarshaller.Free(result);
        }
        else
        {
            WindowsRuntimeMarshal.Free(result);
        }

        HStringMarshaller.Free(key);
        WindowsRuntimeMarshal.Free((void*)unexpected);
        WindowsRuntimeMarshal.Free((void*)view);
    }
}

static unsafe bool CheckGetView(object dictionary, bool stringValues, object expectedValue)
{
    void* ccw = WindowsRuntimeMarshal.ConvertToUnmanaged(dictionary);
    nint map = 0;
    nint unexpected = 0;
    void* view = null;

    try
    {
        Guid mapIid = stringValues ? InterfaceIds.StringMap : InterfaceIds.ObjectMap;
        Guid viewIid = stringValues ? InterfaceIds.StringMapView : InterfaceIds.ObjectMapView;

        Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)ccw, mapIid, out map));

        if (map == 0 ||
            Marshal.QueryInterface((nint)ccw, viewIid, out unexpected) != unchecked((int)0x80004002) ||
            unexpected != 0)
        {
            return false;
        }

        // This IDictionary does not implement IReadOnlyDictionary, so GetView must construct ReadOnlyDictionary.
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void**, int>)(*(void***)map)[9])((void*)map, &view));

        if (!WindowsRuntimeMarshal.TryGetManagedObject(view, out object? managedView) ||
            (stringValues ? managedView is not ReadOnlyDictionary<string, string> : managedView is not ReadOnlyDictionary<string, object>))
        {
            return false;
        }

        return CheckMapView(view, stringValues, expectedValue, managedView);
    }
    finally
    {
        WindowsRuntimeMarshal.Free(view);
        WindowsRuntimeMarshal.Free((void*)unexpected);
        WindowsRuntimeMarshal.Free((void*)map);
        WindowsRuntimeMarshal.Free(ccw);
    }
}

static unsafe bool CheckStringable(StringableProbe value)
{
    void* ccw = WindowsRuntimeMarshal.ConvertToUnmanaged(value);
    nint stringable = 0;
    nint unexpected = 0;
    void* result = null;
    int callCount = value.CallCount;

    try
    {
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)ccw, InterfaceIds.Stringable, out stringable));

        if (stringable == 0 ||
            Marshal.QueryInterface((nint)ccw, InterfaceIds.ObjectMapView, out unexpected) != unchecked((int)0x80004002) ||
            unexpected != 0)
        {
            return false;
        }

        Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void**, int>)(*(void***)stringable)[6])((void*)stringable, &result));

        return HStringMarshaller.ConvertToManaged(result) == value.Text && value.CallCount == callCount + 1;
    }
    finally
    {
        HStringMarshaller.Free(result);
        WindowsRuntimeMarshal.Free((void*)unexpected);
        WindowsRuntimeMarshal.Free((void*)stringable);
        WindowsRuntimeMarshal.Free(ccw);
    }
}

static unsafe bool CheckDisposable(DisposableProbe value)
{
    void* ccw = WindowsRuntimeMarshal.ConvertToUnmanaged(value);
    nint closable = 0;
    nint unexpected = 0;
    int disposeCount = value.DisposeCount;

    try
    {
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)ccw, InterfaceIds.Closable, out closable));

        if (closable == 0 ||
            !WindowsRuntimeMarshal.TryGetManagedObject((void*)closable, out object? managedValue) ||
            !ReferenceEquals(managedValue, value) ||
            Marshal.QueryInterface((nint)ccw, InterfaceIds.ObjectMapView, out unexpected) != unchecked((int)0x80004002) ||
            unexpected != 0)
        {
            return false;
        }

        // IClosable.Close must dispatch to the IDisposable implementation in the netstandard2.0 library.
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, int>)(*(void***)closable)[6])((void*)closable));

        return value.DisposeCount == disposeCount + 1;
    }
    finally
    {
        WindowsRuntimeMarshal.Free((void*)unexpected);
        WindowsRuntimeMarshal.Free((void*)closable);
        WindowsRuntimeMarshal.Free(ccw);
    }
}

static class InterfaceIds
{
    public static readonly Guid ObjectMap = new("1B0D3570-0877-5EC2-8A2C-3B9539506ACA");
    public static readonly Guid StringMap = new("F6D1F700-49C2-52AE-8154-826F9908773C");
    public static readonly Guid ObjectMapView = new("BB78502A-F79D-54FA-92C9-90C5039FDF7E");
    public static readonly Guid StringMapView = new("AC7F26F2-FEB7-5B2A-8AC4-345BC62CAEDE");
    public static readonly Guid Stringable = new("96369F54-8EB6-48F0-ABCE-C1B211E627C3");
    public static readonly Guid Closable = new("30D5A829-7FA4-4026-83BB-D75BAE4EA99E");
}

sealed class StringableProbe(string text) : IStringable
{
    public string Text { get; } = text;

    public int CallCount { get; private set; }

    string IStringable.ToString()
    {
        CallCount++;

        return Text;
    }

    public override string ToString() => "Not the IStringable callback";
}

// Deliberately avoid IReadOnlyDictionary so the generated IMap.GetView fallback is exercised.
sealed class MapOnlyDictionary<TValue>(TValue value) : IDictionary<string, TValue>
{
    private readonly Dictionary<string, TValue> _values = new() { ["key"] = value };

    public TValue this[string key]
    {
        get => _values[key];
        set => _values[key] = value;
    }

    public ICollection<string> Keys => _values.Keys;

    public ICollection<TValue> Values => _values.Values;

    public int Count => _values.Count;

    public bool IsReadOnly => false;

    public void Add(string key, TValue value) => _values.Add(key, value);

    public bool ContainsKey(string key) => _values.ContainsKey(key);

    public bool Remove(string key) => _values.Remove(key);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value) => _values.TryGetValue(key, out value);

    public void Add(KeyValuePair<string, TValue> item) => ((ICollection<KeyValuePair<string, TValue>>)_values).Add(item);

    public void Clear() => _values.Clear();

    public bool Contains(KeyValuePair<string, TValue> item) => ((ICollection<KeyValuePair<string, TValue>>)_values).Contains(item);

    public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, TValue>>)_values).CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<string, TValue> item) => ((ICollection<KeyValuePair<string, TValue>>)_values).Remove(item);

    public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
