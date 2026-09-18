// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Contoso;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0")]
[assembly: DisableRuntimeMarshalling]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Interop")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Runtime")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Sdk.Projection")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Interop")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Runtime")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Sdk.Projection")]
[assembly: TypeMapAssemblyTarget<DynamicInterfaceCastableImplementationTypeMapGroup>("WinRT.Interop")]
[assembly: TypeMapAssemblyTarget<DynamicInterfaceCastableImplementationTypeMapGroup>("WinRT.Runtime")]
[assembly: TypeMapAssemblyTarget<DynamicInterfaceCastableImplementationTypeMapGroup>("WinRT.Sdk.Projection")]

internal static unsafe class Program
{
    public static int Main(string[] args)
    {
        Target target = new();
        using WindowsRuntimeObjectReferenceValue reference =
            WindowsRuntimeInterfaceMarshaller<IWidget2>.ConvertToUnmanaged(target, in ABI.Contoso.IWidget2Impl.IID);
        void* ccw = reference.GetThisPtrUnsafe();

        switch (args[0])
        {
            case "conversion-failure":
                VerifyConversionFailure(target, ccw);
                break;
            case "setter-failure":
                VerifySetterFailure(target, ccw);
                break;
            case "other-members":
                VerifyOtherMembers(target, ccw);
                break;
            default:
                VerifyArrays(target, ccw, int.Parse(args[0]));
                break;
        }

        return 0;
    }

    private static void VerifyArrays(Target target, void* ccw, int length)
    {
        int[] expectedValues = Enumerable.Range(1, length).ToArray();
        int[] nativeValues = (int[])expectedValues.Clone();
        fixed (int* data = nativeValues)
        {
            Check(Set(ccw, "Values", (uint)length, data) == 0, "int setter");
            Check(nativeValues.SequenceEqual(expectedValues), "setter modified native input");
            nativeValues.AsSpan().Clear();
        }
        CheckOwned(target.Values, expectedValues);

        bool[] flags = Enumerable.Range(0, length).Select(i => i % 2 == 0).ToArray();
        fixed (bool* data = flags)
        {
            Check(Set(ccw, "Flags", (uint)length, data) == 0, "bool setter");
        }
        CheckOwned(target.Flags, flags);

        Tag[] tags = Enumerable.Range(1, length).Select(i => (Tag)i).ToArray();
        fixed (Tag* data = tags)
        {
            Check(Set(ccw, "Tags", (uint)length, data) == 0, "enum setter with escaped parameter name");
        }
        CheckOwned(target.Tags, tags);

        string[] names = Enumerable.Range(0, length).Select(i => i == 0 ? "" : $"name-{i}").ToArray();
        HStringArrayMarshaller.ConvertToUnmanaged(names, out uint namesLength, out void** nativeNames);
        try
        {
            Check(Set(ccw, "Names", namesLength, nativeNames) == 0, "string setter");
            Check(HStringArrayMarshaller.ConvertToManaged(namesLength, nativeNames).SequenceEqual(names), "borrowed HSTRINGs");
        }
        finally
        {
            HStringArrayMarshaller.Dispose(namesLength, nativeNames);
            Marshal.FreeCoTaskMem((nint)nativeNames);
        }
        CheckOwned(target.Names, names);

        object[] objects = Enumerable.Range(0, length).Select(i => (i % 3) switch
        {
            0 => (object)target,
            1 => $"object-{i}",
            _ => i
        }).ToArray();
        WindowsRuntimeObjectArrayMarshaller.ConvertToUnmanaged(objects, out uint objectsLength, out void** nativeObjects);
        try
        {
            Check(Set(ccw, "Objects", objectsLength, nativeObjects) == 0, "object setter");
            Check(WindowsRuntimeObjectArrayMarshaller.ConvertToManaged(objectsLength, nativeObjects).SequenceEqual(objects), "borrowed objects");
        }
        finally
        {
            for (uint i = 0; i < objectsLength; i++)
            {
                WindowsRuntimeMarshal.Free(nativeObjects[i]);
            }
            Marshal.FreeCoTaskMem((nint)nativeObjects);
        }
        CheckOwned(target.Objects, objects);

        nint[] interfaces = Enumerable.Repeat((nint)ccw, length).ToArray();
        fixed (nint* data = interfaces)
        {
            int hr = Set(ccw, "Interfaces", (uint)length, data);
            RestrictedErrorInfo.ThrowExceptionForHR(hr);
            Check(hr == 0, "interface setter");
        }
        CheckOwned(target.Interfaces, Enumerable.Repeat<IWidget2>(target, length).ToArray());

        nint[] widgets = new nint[length];
        fixed (nint* data = widgets)
        {
            Check(Set(ccw, "Widgets", (uint)length, data) == 0, "runtime class setter");
        }
        CheckOwned(target.Widgets, new Widget[length]);

        Payload[] payloads = Enumerable.Range(0, length).Select(i => new Payload
        {
            Id = i + 1,
            Text = $"payload-{i}",
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(i)
        }).ToArray();
        ABI.Contoso.Payload[] nativePayloads = new ABI.Contoso.Payload[length];
        try
        {
            for (int i = 0; i < length; i++)
            {
                nativePayloads[i] = ABI.Contoso.PayloadMarshaller.ConvertToUnmanaged(payloads[i]);
            }
            fixed (ABI.Contoso.Payload* data = nativePayloads)
            {
                Check(Set(ccw, "Payloads", (uint)length, data) == 0, "non-blittable struct setter");
            }
        }
        finally
        {
            foreach (ABI.Contoso.Payload payload in nativePayloads)
            {
                ABI.Contoso.PayloadMarshaller.Dispose(payload);
            }
        }
        CheckOwned(target.Payloads, payloads);

        // A later setter call must not overwrite an array retained by an earlier call.
        int[] retained = target.Values;
        int replacement = 123;
        Check(Set(ccw, "Values", 1, &replacement) == 0, "second setter");
        CheckOwned(retained, expectedValues);
        target.Values[0] = 456;
        Check(replacement == 123, "managed array aliases the caller's buffer");
    }

    private static void VerifyConversionFailure(Target target, void* ccw)
    {
        Payload[] original = [new Payload { Id = 1, Text = "original", Timestamp = DateTimeOffset.UnixEpoch }];
        target.Payloads = original;
        int previousCalls = target.PayloadSetterCalls;
        ABI.Contoso.Payload[] native = new ABI.Contoso.Payload[2];
        try
        {
            native[0] = ABI.Contoso.PayloadMarshaller.ConvertToUnmanaged(original[0]);
            native[1] = ABI.Contoso.PayloadMarshaller.ConvertToUnmanaged(original[0]);
            native[1].Timestamp = Unsafe.BitCast<long, ABI.System.DateTimeOffset>(long.MaxValue);
            fixed (ABI.Contoso.Payload* data = native)
            {
                int hr = Set(ccw, "Payloads", 2, data);
                Check(hr == RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(new ArgumentOutOfRangeException()), "conversion HRESULT");
                Check(target.PayloadSetterCalls == previousCalls, "partially converted array reached setter");
                Check(ReferenceEquals(target.Payloads, original), "conversion failure changed property");
                Check(HStringMarshaller.ConvertToManaged(data[0].Text) == "original", "first borrowed HSTRING was released");
                Check(HStringMarshaller.ConvertToManaged(data[1].Text) == "original", "failing element's HSTRING was released");
                Check(Unsafe.BitCast<ABI.System.DateTimeOffset, long>(data[1].Timestamp) == long.MaxValue, "conversion modified native buffer");
            }
        }
        finally
        {
            foreach (ABI.Contoso.Payload payload in native)
            {
                ABI.Contoso.PayloadMarshaller.Dispose(payload);
            }
        }

        string[] originalNames = ["retained"];
        target.Names = originalNames;
        int nullHr = Set(ccw, "Names", 1, null);
        Check(nullHr == RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(new ArgumentNullException()), "null buffer HRESULT");
        Check(ReferenceEquals(originalNames, target.Names), "invalid buffer reached setter");
    }

    private static void VerifySetterFailure(Target target, void* ccw)
    {
        int[] expected = Enumerable.Range(1, 17).ToArray();
        int[] native = (int[])expected.Clone();
        target.ThrowOnSet = true;
        fixed (int* data = native)
        {
            Check(Set(ccw, "Values", (uint)native.Length, data) == new UnauthorizedAccessException().HResult, "setter HRESULT");
            Check(native.SequenceEqual(expected), "throwing setter changed input");
            native.AsSpan().Clear();
        }
        CheckOwned(target.Values, expected);
    }

    private static void VerifyOtherMembers(Target target, void* ccw)
    {
        target.Values = [1, 2, 3];
        uint length = 0;
        void* data = null;
        int hr = ((delegate* unmanaged[MemberFunction]<void*, uint*, void**, int>)Entry(ccw, "get_Values"))(ccw, &length, &data);
        try
        {
            Check(hr == 0, "array getter");
            Check(new ReadOnlySpan<int>(data, (int)length).SequenceEqual(target.Values), "array getter contents");
            ((int*)data)[0] = 42;
            Check(target.Values[0] == 1, "getter aliases managed storage");
        }
        finally
        {
            Marshal.FreeCoTaskMem((nint)data);
        }

        target.Names = ["first", "second"];
        data = null;
        hr = ((delegate* unmanaged[MemberFunction]<void*, uint*, void**, int>)Entry(ccw, "get_Names"))(ccw, &length, &data);
        try
        {
            Check(hr == 0, "string array getter");
            Check(HStringArrayMarshaller.ConvertToManaged(length, (void**)data).SequenceEqual(target.Names), "string getter contents");
        }
        finally
        {
            HStringArrayMarshaller.Dispose(length, (void**)data);
            Marshal.FreeCoTaskMem((nint)data);
        }

        void* label = HStringMarshaller.ConvertToUnmanaged("label");
        try
        {
            hr = ((delegate* unmanaged[MemberFunction]<void*, void*, int>)Entry(ccw, "put_Label"))(ccw, label);
            Check(hr == 0 && target.Label == "label", "scalar setter");
        }
        finally
        {
            HStringMarshaller.Free(label);
        }

        int[] values = Enumerable.Range(1, 33).ToArray();
        fixed (int* buffer = values)
        {
            Check(CallArrayMethod(ccw, "AcceptValues", (uint)values.Length, buffer) == 0, "ReadOnlySpan method");
            Check(target.SeenValues.SequenceEqual(values), "ReadOnlySpan contents");
            Check(CallArrayMethod(ccw, "FillValues", (uint)values.Length, buffer) == 0, "Span method");
            Check(values.All(value => value == 42), "Span writeback");
        }

        string[] names = Enumerable.Range(0, 16).Select(i => $"name-{i}").ToArray();
        HStringArrayMarshaller.ConvertToUnmanaged(names, out length, out void** nativeNames);
        try
        {
            Check(CallArrayMethod(ccw, "AcceptNames", length, nativeNames) == 0, "string ReadOnlySpan method");
            Check(target.SeenNames.SequenceEqual(names), "string ReadOnlySpan contents");
        }
        finally
        {
            HStringArrayMarshaller.Dispose(length, nativeNames);
            Marshal.FreeCoTaskMem((nint)nativeNames);
        }
    }

    private static int Set(void* ccw, string property, uint length, void* data)
    {
        return CallArrayMethod(ccw, $"put_{property}", length, data);
    }

    private static int CallArrayMethod(void* ccw, string name, uint length, void* data)
    {
        return ((delegate* unmanaged[MemberFunction]<void*, uint, void*, int>)Entry(ccw, name))(ccw, length, data);
    }

    private static void* Entry(void* ccw, string name)
    {
        Type vtable = typeof(IWidget2).Assembly.GetType("ABI.Contoso.IWidget2Vftbl", throwOnError: true)!;
        string field = vtable.GetFields().Single(
            field => field.Name.StartsWith($"{name}_", StringComparison.Ordinal)).Name;
        int slot = (int)Marshal.OffsetOf(vtable, field) / sizeof(nint);
        return (*(void***)ccw)[slot];
    }

    private static void CheckOwned<T>(T[] actual, T[] expected)
    {
        Check(actual.Length == expected.Length, $"array length for {typeof(T)}");
        T[] rented = ArrayPool<T>.Shared.Rent(Math.Max(1, actual.Length));
        try
        {
            rented.AsSpan().Clear();
            Check(actual.SequenceEqual(expected), $"retained array contents for {typeof(T)}");
        }
        finally
        {
            ArrayPool<T>.Shared.Return(rented, clearArray: true);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class Target : IWidget2
    {
        public bool ThrowOnSet { get; set; }
        public int PayloadSetterCalls { get; private set; }
        public int[] Values
        {
            get;
            set
            {
                field = value;
                if (ThrowOnSet)
                {
                    throw new UnauthorizedAccessException();
                }
            }
        } = [];
        public Payload[] Payloads
        {
            get;
            set
            {
                PayloadSetterCalls++;
                field = value;
            }
        } = [];
        public bool[] Flags { get; set; } = [];
        public string[] Names { get; set; } = [];
        public object[] Objects { get; set; } = [];
        public Widget[] Widgets { get; set; } = [];
        public IWidget2[] Interfaces { get; set; } = [];
        public Tag[] Tags { get; set; } = [];
        public string Label { get; set; } = "";
        public int[] SeenValues { get; private set; } = [];
        public string[] SeenNames { get; private set; } = [];
        public void AcceptValues(ReadOnlySpan<int> value) => SeenValues = value.ToArray();
        public void AcceptNames(ReadOnlySpan<string> value) => SeenNames = value.ToArray();
        public void FillValues(Span<int> value) => value.Fill(42);
    }
}
