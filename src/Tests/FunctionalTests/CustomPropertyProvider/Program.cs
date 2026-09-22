// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CustomPropertyProviderLibrary;
using Windows.UI.Xaml.Data;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using WindowsRuntime.Xaml;

GenericProperties<int> integer = new();
GenericProperties<string> text = new();
ICustomPropertyProvider integerProvider = integer;
ICustomPropertyProvider textProvider = text;

if (integerProvider.Type != typeof(GenericProperties<int>) ||
    textProvider.Type != typeof(GenericProperties<string>))
{
    return 101;
}

ICustomProperty integerValue = integerProvider.GetCustomProperty("Value");
ICustomProperty textValue = textProvider.GetCustomProperty("Value");
ICustomProperty integerCount = integerProvider.GetCustomProperty("Count");
ICustomProperty textCount = textProvider.GetCustomProperty("Count");
ICustomProperty integerShared = integerProvider.GetCustomProperty("Shared");
ICustomProperty textShared = textProvider.GetCustomProperty("Shared");

if (integerValue.Type != typeof(int) || textValue.Type != typeof(string) ||
    integerCount.Type != typeof(int) || textCount.Type != typeof(int) ||
    integerShared.Type != typeof(int) || textShared.Type != typeof(string) ||
    ReferenceEquals(integerValue, textValue) || ReferenceEquals(integerCount, textCount) ||
    ReferenceEquals(integerShared, textShared) ||
    !ReferenceEquals(integerValue, integerProvider.GetCustomProperty("Value")))
{
    return 102;
}

integerValue.SetValue(integer, 42);
textValue.SetValue(text, "text");
integerCount.SetValue(integer, 7);
textCount.SetValue(text, 8);
integerShared.SetValue(null, 100);
textShared.SetValue(null, "shared");

// All three descriptors are generic cached fields returned through the same interface-typed switch
if (!Equals(GetNativeValue(integerValue, integer), 42) ||
    !Equals(GetNativeValue(textValue, text), "text") ||
    !Equals(GetNativeValue(integerCount, integer), 7) ||
    !Equals(GetNativeValue(textCount, text), 8) ||
    !Equals(GetNativeValue(integerShared, null), 100) ||
    !Equals(GetNativeValue(textShared, null), "shared"))
{
    return 103;
}

SetNativeValue(integerValue, integer, 84);
SetNativeValue(textValue, text, "updated");
SetNativeValue(integerCount, integer, 9);
SetNativeValue(textCount, text, 10);
SetNativeValue(integerShared, null, 200);
SetNativeValue(textShared, null, "updated shared");

if (integer.Value != 84 || text.Value != "updated" ||
    integer.Count != 9 || text.Count != 10 ||
    GenericProperties<int>.Shared != 200 || GenericProperties<string>.Shared != "updated shared")
{
    return 104;
}

GenericIndexers<int, string> intIndexerOwner = new();
GenericIndexers<string, int> stringIndexerOwner = new();
ICustomPropertyProvider intIndexerProvider = intIndexerOwner;
ICustomPropertyProvider stringIndexerProvider = stringIndexerOwner;
ICustomProperty intIndexer = intIndexerProvider.GetIndexedProperty("Item", typeof(int));
ICustomProperty stringIndexer = stringIndexerProvider.GetIndexedProperty("Item", typeof(string));

if (intIndexer.Type != typeof(string) || stringIndexer.Type != typeof(int) ||
    ReferenceEquals(intIndexer, stringIndexer) ||
    !ReferenceEquals(intIndexer, intIndexerProvider.GetIndexedProperty("Item", typeof(int))))
{
    return 105;
}

intIndexer.SetIndexedValue(intIndexerOwner, "indexed", 1);
stringIndexer.SetIndexedValue(stringIndexerOwner, 123, "key");

if (!Equals(GetNativeValue(intIndexer, intIndexerOwner, 2), "indexed") ||
    !Equals(GetNativeValue(stringIndexer, stringIndexerOwner, "other"), 123) ||
    intIndexerOwner.LastKey != 2 || stringIndexerOwner.LastKey != "other")
{
    return 106;
}

SetNativeValue(intIndexer, intIndexerOwner, "native index", 3);
SetNativeValue(stringIndexer, stringIndexerOwner, 456, "native key");

if (intIndexerOwner.Value != "native index" || stringIndexerOwner.Value != 456 ||
    intIndexerOwner.LastKey != 3 || stringIndexerOwner.LastKey != "native key")
{
    return 107;
}

GenericContainer<int>.Nested nested = new();
ICustomPropertyProvider nestedProvider = nested;
ICustomProperty nestedValue = nestedProvider.GetCustomProperty("Value");
ICustomProperty nestedOptional = nestedProvider.GetCustomProperty("Optional");

if (nestedProvider.Type != typeof(GenericContainer<int>.Nested) ||
    nestedValue.Type != typeof(int) || nestedOptional.Type != typeof(int?))
{
    return 108;
}

nestedValue.SetValue(nested, 256);
nestedOptional.SetValue(nested, 512);

if (!Equals(GetNativeValue(nestedValue, nested), 256) ||
    !Equals(GetNativeValue(nestedOptional, nested), 512))
{
    return 109;
}

SetNativeValue(nestedValue, nested, 1024);
SetNativeValue(nestedOptional, nested, null);

if (nested.Value != 1024 || nested.Optional is not null ||
    GetNativeValue(nestedOptional, nested) is not null)
{
    return 110;
}

ICustomProperty cachedProperty = PropertyFactory<string>.Create<long>();

if (cachedProperty.Type != typeof(long) || !Equals(GetNativeValue(cachedProperty, null), 0L))
{
    return 111;
}

ICustomProperty interfaceTypedSingleton = PropertyFactory<string>.CreateSelfCached<short>();

if (interfaceTypedSingleton.Type != typeof(short) ||
    !Equals(GetNativeValue(interfaceTypedSingleton, null), (short)0))
{
    return 112;
}

ICustomProperty interfaceTypedCache = PropertyFactory<string>.CreateInterfaceCached<uint>();

if (interfaceTypedCache.Type != typeof(uint) ||
    !Equals(GetNativeValue(interfaceTypedCache, null), 0U))
{
    return 113;
}

object cachedArray = PropertyFactory<string>.CreateArray<KeyValuePair<int, string>>();

if (cachedArray is not Array { Length: 1 })
{
    return 114;
}

unsafe
{
    void* bindableVector = GetInterface(cachedArray, new Guid("393DE7DE-6FD0-4C0D-BB71-47244A113E93"));

    WindowsRuntimeMarshal.Free(bindableVector);
}

if (!CheckNativeProvider(new NongenericDictionary(), "key", "key", 1))
{
    return 115;
}

GenericDictionary<string, long> directDictionary = new(new Dictionary<string, long> { ["key"] = 42 });

if (!CheckNativeProvider(directDictionary, "key", 42L, 1))
{
    return 116;
}

IFirstModel firstModel = new FirstModel();
ISecondModel secondModel = new SecondModel();
IReadOnlyDictionary<string, IFirstModel> firstDictionary = new Dictionary<string, IFirstModel> { ["key"] = firstModel };
IReadOnlyDictionary<string, ISecondModel> secondDictionary = new Dictionary<string, ISecondModel> { ["key"] = secondModel };
IReadOnlyList<IFirstModel> firstList = new List<IFirstModel> { firstModel };
IReadOnlyList<ISecondModel> secondList = new List<ISecondModel> { secondModel };

// These closed owners must only be discovered through factories in the referenced assembly.
ICustomPropertyProvider firstDictionaryProvider = firstDictionary.AsBindable();
ICustomPropertyProvider secondDictionaryProvider = secondDictionary.AsBindable();
ICustomPropertyProvider firstListProvider = firstList.AsBindable();
ICustomPropertyProvider secondListProvider = secondList.AsBindable();

if (!CheckNativeProvider(firstDictionaryProvider, "key", firstModel, 1))
{
    return 117;
}

if (!CheckNativeProvider(secondDictionaryProvider, "key", secondModel, 1))
{
    return 118;
}

if (!CheckNativeProvider(firstListProvider, 0, firstModel, 1))
{
    return 119;
}

if (!CheckNativeProvider(secondListProvider, 0, secondModel, 1))
{
    return 120;
}

ICustomPropertyProvider repeatedProvider = firstDictionary.AsBindable();

if (!ReferenceEquals(firstDictionaryProvider.GetCustomProperty("Count"), repeatedProvider.GetCustomProperty("Count")) ||
    !ReferenceEquals(firstDictionaryProvider.GetIndexedProperty("Item", typeof(string)), repeatedProvider.GetIndexedProperty("Item", typeof(string))) ||
    ReferenceEquals(firstDictionaryProvider.GetCustomProperty("Count"), secondDictionaryProvider.GetCustomProperty("Count")) ||
    ReferenceEquals(firstDictionaryProvider.GetIndexedProperty("Item", typeof(string)), secondDictionaryProvider.GetIndexedProperty("Item", typeof(string))))
{
    return 121;
}

return 100;

static bool CheckNativeProvider(ICustomPropertyProvider provider, object index, object expectedValue, int expectedCount)
{
    ICustomProperty count = provider.GetCustomProperty("Count");
    ICustomProperty indexer = provider.GetIndexedProperty("Item", index.GetType());
    ICustomProperty nativeCount = GetNativeProperty(provider, "Count");
    ICustomProperty nativeIndexer = GetNativeProperty(provider, "Item", index.GetType());

    return ReferenceEquals(count, nativeCount) &&
        ReferenceEquals(indexer, nativeIndexer) &&
        Equals(count.GetValue(provider), expectedCount) &&
        Equals(indexer.GetIndexedValue(provider, index), expectedValue) &&
        Equals(GetNativeValue(nativeCount, provider), expectedCount) &&
        Equals(GetNativeValue(nativeIndexer, provider, index), expectedValue);
}

static unsafe ICustomProperty GetNativeProperty(ICustomPropertyProvider provider, string name, Type indexType = null)
{
    void* providerPtr = null;
    void* result = null;
    void* memberName = null;
    ABI.System.Type nativeIndexType = default;

    try
    {
        providerPtr = GetInterface(provider, new Guid("7C925755-3E48-42B4-8677-76372267033F"));
        memberName = HStringMarshaller.ConvertToUnmanaged(name);
        void** vtable = *(void***)providerPtr;

        if (indexType is null)
        {
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void*, void**, int>)vtable[6])(providerPtr, memberName, &result));
        }
        else
        {
            nativeIndexType = ABI.System.TypeMarshaller.ConvertToUnmanaged(indexType);
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void*, ABI.System.Type, void**, int>)vtable[7])(providerPtr, memberName, nativeIndexType, &result));
        }

        return (ICustomProperty)WindowsRuntimeMarshal.ConvertToManaged(result);
    }
    finally
    {
        ABI.System.TypeMarshaller.Dispose(nativeIndexType);
        HStringMarshaller.Free(memberName);
        WindowsRuntimeMarshal.Free(result);
        WindowsRuntimeMarshal.Free(providerPtr);
    }
}

static unsafe void* GetInterface(object value, Guid iid)
{
    if (value is null)
    {
        return null;
    }

    void* unknown = WindowsRuntimeMarshal.ConvertToUnmanaged(value);

    try
    {
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)unknown, in iid, out nint result));

        return (void*)result;
    }
    finally
    {
        WindowsRuntimeMarshal.Free(unknown);
    }
}

static unsafe object GetNativeValue(ICustomProperty property, object target, object index = null)
{
    void* propertyPtr = null;
    void* targetPtr = null;
    void* indexPtr = null;
    void* result = null;

    try
    {
        propertyPtr = GetInterface(property, new Guid("30DA92C0-23E8-42A0-AE7C-734A0E5D2782"));
        targetPtr = GetInterface(target, new Guid("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90"));
        void** vtable = *(void***)propertyPtr;

        if (index is null)
        {
            // ICustomProperty.GetValue follows the Type and Name accessors
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void*, void**, int>)vtable[8])(propertyPtr, targetPtr, &result));
        }
        else
        {
            indexPtr = GetInterface(index, new Guid("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90"));

            // ICustomProperty.GetIndexedValue follows GetValue and SetValue
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void*, void*, void**, int>)vtable[10])(propertyPtr, targetPtr, indexPtr, &result));
        }

        return WindowsRuntimeMarshal.ConvertToManaged(result);
    }
    finally
    {
        WindowsRuntimeMarshal.Free(result);
        WindowsRuntimeMarshal.Free(indexPtr);
        WindowsRuntimeMarshal.Free(targetPtr);
        WindowsRuntimeMarshal.Free(propertyPtr);
    }
}

static unsafe void SetNativeValue(ICustomProperty property, object target, object value, object index = null)
{
    void* propertyPtr = null;
    void* targetPtr = null;
    void* valuePtr = null;
    void* indexPtr = null;

    try
    {
        propertyPtr = GetInterface(property, new Guid("30DA92C0-23E8-42A0-AE7C-734A0E5D2782"));
        targetPtr = GetInterface(target, new Guid("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90"));
        valuePtr = GetInterface(value, new Guid("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90"));
        void** vtable = *(void***)propertyPtr;

        if (index is null)
        {
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void*, void*, int>)vtable[9])(propertyPtr, targetPtr, valuePtr));
        }
        else
        {
            indexPtr = GetInterface(index, new Guid("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90"));
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void*, void*, void*, int>)vtable[11])(propertyPtr, targetPtr, valuePtr, indexPtr));
        }
    }
    finally
    {
        WindowsRuntimeMarshal.Free(indexPtr);
        WindowsRuntimeMarshal.Free(valuePtr);
        WindowsRuntimeMarshal.Free(targetPtr);
        WindowsRuntimeMarshal.Free(propertyPtr);
    }
}

[GeneratedCustomPropertyProvider]
public sealed partial class GenericProperties<T>
{
    public T Value { get; set; }

    public int Count { get; set; }

    public static T Shared { get; set; }
}

[GeneratedCustomPropertyProvider]
public sealed partial class GenericIndexers<TKey, TValue>
{
    public TKey LastKey { get; private set; }

    public TValue Value { get; set; }

    public TValue this[TKey index]
    {
        get
        {
            LastKey = index;

            return Value;
        }
        set
        {
            LastKey = index;
            Value = value;
        }
    }
}

public partial class GenericContainer<T> where T : struct
{
    [GeneratedCustomPropertyProvider]
    public sealed partial class Nested
    {
        public T Value { get; set; }

        public T? Optional { get; set; }
    }
}

static class PropertyFactory<T>
{
    public static ICustomProperty Create<TValue>() where TValue : struct => PropertyCache<T, TValue>.Instance;

    public static ICustomProperty CreateSelfCached<TValue>() where TValue : struct => ConstantProperty<TValue>.Instance;

    public static ICustomProperty CreateInterfaceCached<TValue>() where TValue : struct => InterfacePropertyCache<T, TValue>.Instance;

    public static object CreateArray<TValue>() => ArrayCache<T, TValue>.Value;
}

static class PropertyCache<TUnused, TValue> where TValue : struct
{
    // The field uses its owner's !1, which the factory supplies through its method parameter (!!0)
    public static readonly ConstantProperty<TValue> Instance = new();
}

static class InterfacePropertyCache<TUnused, TValue> where TValue : struct
{
    public static readonly ICustomProperty Instance = TransitivePropertyCache<TValue, TUnused>.Instance;
}

static class TransitivePropertyCache<TValue, TUnused> where TValue : struct
{
    public static readonly ICustomProperty Instance = new ConstantProperty<TValue>();

    static TransitivePropertyCache()
    {
        // The initializer graph has a cycle, but each closed initializer must be scanned only once
        _ = InterfacePropertyCache<TUnused, TValue>.Instance;
    }
}

static class ArrayCache<TUnused, TValue>
{
    public static readonly object Value = new TValue[1];
}

sealed class ConstantProperty<T> : ICustomProperty where T : struct
{
    // Discovery must use the field's declaring type when its value type is just an interface
    public static readonly ICustomProperty Instance = new ConstantProperty<T>();

    public Type Type => typeof(T);
    public string Name => "Cached";
    public bool CanRead => true;
    public bool CanWrite => false;
    public object GetValue(object target) => default(T);
    public void SetValue(object target, object value) => throw new NotSupportedException();
    public object GetIndexedValue(object target, object index) => throw new NotSupportedException();
    public void SetIndexedValue(object target, object value, object index) => throw new NotSupportedException();
}

[GeneratedCustomPropertyProvider]
public sealed partial class NongenericDictionary
{
    public string this[string key] => key;

    public int Count => 1;
}

interface IFirstModel;
interface ISecondModel;
sealed class FirstModel : IFirstModel;
sealed class SecondModel : ISecondModel;
