// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Collections.Generic;
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
    private const int GuardSize = 16;
    private const byte GuardValue = 0xA5;

    public static int Main(string[] args)
    {
        Target target = new();
        using WindowsRuntimeObjectReferenceValue reference =
            WindowsRuntimeInterfaceMarshaller<IWidget2>.ConvertToUnmanaged(target, in ABI.Contoso.IWidget2Impl.IID);
        void* ccw = reference.GetThisPtrUnsafe();

        switch (args[0])
        {
            case string scenario when scenario.StartsWith("scalar-output:", StringComparison.Ordinal):
                string[] scalar = scenario.Split(':');
                VerifyScalarOutput(target, ccw, scalar[1], scalar[2]);
                break;
            case "shared-clear":
                VerifySharedClear();
                break;
            case string scenario when scenario.StartsWith("struct-fill-rollback:", StringComparison.Ordinal):
                VerifyStructFillRollback(ccw, int.Parse(scenario["struct-fill-rollback:".Length..]));
                break;
            case string scenario when scenario.StartsWith("final-output-rollback:", StringComparison.Ordinal):
                VerifyOutputRollback(target, ccw, scenario["final-output-rollback:".Length..], finalReceiveArray: true);
                break;
            case string scenario when scenario.StartsWith("output-rollback:", StringComparison.Ordinal):
                VerifyOutputRollback(target, ccw, scenario["output-rollback:".Length..]);
                break;
            case string scenario when scenario.StartsWith("fill-rollback:", StringComparison.Ordinal):
                string[] rollback = scenario.Split(':');
                VerifyFillRollback(target, ccw, int.Parse(rollback[1]), rollback[2]);
                break;
            case string scenario when scenario.StartsWith("array-abi:", StringComparison.Ordinal):
                string[] parts = scenario.Split(':');
                VerifyArrayAbi(target, ccw, parts[1], int.Parse(parts[2]));
                break;
            case string scenario when scenario.StartsWith("resource-free:", StringComparison.Ordinal):
                VerifyResourceFreeArrays(target, ccw, int.Parse(scenario["resource-free:".Length..]));
                break;
            case "conversion-failure":
                VerifyConversionFailure(target, ccw);
                break;
            case "setter-failure":
                VerifySetterFailure(target, ccw);
                break;
            case "other-members":
                VerifyOtherMembers(target, ccw);
                break;
            case string scenario when scenario.StartsWith("retention:", StringComparison.Ordinal):
                VerifyMarshalledObjectCollection(target, ccw, int.Parse(scenario["retention:".Length..]));
                break;
            case string scenario when scenario.StartsWith("mapped-rental:", StringComparison.Ordinal):
                VerifyMappedRental(target, ccw, int.Parse(scenario["mapped-rental:".Length..]));
                break;
            case string scenario when scenario.StartsWith("fill-failure:", StringComparison.Ordinal):
                VerifyArrayFailure(target, ccw, int.Parse(scenario["fill-failure:".Length..]), passArray: false);
                break;
            case string scenario when scenario.StartsWith("pass-failure:", StringComparison.Ordinal):
                VerifyArrayFailure(target, ccw, int.Parse(scenario["pass-failure:".Length..]), passArray: true);
                break;
            default:
                bool methods = args[0].StartsWith("methods:", StringComparison.Ordinal);
                VerifyArrays(target, ccw, int.Parse(methods ? args[0]["methods:".Length..] : args[0]), methods);
                break;
        }

        return 0;
    }

    private static void VerifyScalarOutput(Target target, void* ccw, string shape, string failure)
    {
        target.OutputFailure = failure;
        int expected = failure switch
        {
            "managed" => RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(new UnauthorizedAccessException()),
            "conversion" => RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(new ArgumentNullException()),
            _ => 0
        };
        if (shape == "string")
        {
            void* value = (void*)12345;
            int hr = ((delegate* unmanaged[MemberFunction]<void*, void**, int>)Entry(ccw, "GetName"))(ccw, &value);
            Check(hr == expected, "scalar string HRESULT");
            if (hr == 0)
            {
                try
                {
                    Check(HStringMarshaller.ConvertToManaged(value) == "scalar", "scalar string contents");
                }
                finally
                {
                    HStringMarshaller.Free(value);
                }
            }
            else
            {
                Check(value == null, "failed scalar string output was not initialized");
            }
        }
        else
        {
            ABI.System.Type value = default;
            value.Name = (void*)12345;
            int hr = ((delegate* unmanaged[MemberFunction]<void*, ABI.System.Type*, int>)Entry(ccw, "GetScalarType"))(ccw, &value);
            Check(hr == expected, "scalar Type HRESULT");
            if (hr == 0)
            {
                try
                {
                    Check(ABI.System.TypeMarshaller.ConvertToManaged(value) == typeof(int), "scalar Type contents");
                }
                finally
                {
                    ABI.System.TypeMarshaller.Dispose(value);
                }
            }
            else
            {
                Check(value.Name == null, "failed scalar Type output was not initialized");
            }
        }
    }

    private static void VerifyOutputRollback(Target target, void* ccw, string failure, bool finalReceiveArray = false)
    {
        target.OutputFailure = "success";
        NativeOutputs warmup = default;
        Check(CallOutputs(ccw, &warmup, finalReceiveArray) == 0, "output warmup");
        warmup.Dispose();

        for (int iteration = 0; iteration < 3; iteration++)
        {
            uint references = ReferenceCount(ccw);
            target.OutputFailure = failure;
            NativeOutputs outputs = default;
            try
            {
                int hr = CallOutputs(ccw, &outputs, finalReceiveArray);
                if (failure == "success")
                {
                    Check(hr == 0, "successful outputs HRESULT");
                    Check(HStringMarshaller.ConvertToManaged(outputs.Text) == "output", "owned string output");
                    Check(outputs.Interface != null && outputs.Item != null, "owned interface and object outputs");
                    Check(outputs.Optional != null && outputs.Pair != null, "owned boxed outputs");
                    NestedPayload payload = ABI.Contoso.NestedPayloadMarshaller.ConvertToManaged(outputs.Payload);
                    Check(payload.Inner.Text == "payload" && payload.Text == "outer", "nested struct output");
                    Check(ABI.System.TypeMarshaller.ConvertToManaged(outputs.Type) == typeof(int), "Type output");
                    Check(outputs.NumbersLength == 3 && new ReadOnlySpan<int>(outputs.Numbers, 3).SequenceEqual([1, 2, 3]), "blittable receive array");
                    Check(TypeArrayMarshaller.ConvertToManaged(outputs.TypesLength, outputs.Types).SequenceEqual(Target.TypeValues(3)), "Type receive array");
                    if (!finalReceiveArray)
                    {
                        Check(ABI.System.TypeMarshaller.ConvertToManaged(outputs.Result) == typeof(int), "owned return value");
                    }
                }
                else
                {
                    int expected = RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(
                        failure == "managed" ? new UnauthorizedAccessException() : new ArgumentNullException());
                    Check(hr == expected, "output rollback HRESULT");
                    Check(outputs.Text == null && outputs.Interface == null && outputs.Item == null, "scalar outputs must be reset");
                    Check(outputs.Optional == null && outputs.Pair == null, "generic outputs must be reset");
                    Check(outputs.Payload.Inner.Text == null && outputs.Payload.Text == null &&
                        outputs.Type.Name == null && outputs.Result.Name == null, "ABI struct outputs must be reset");
                    Check(outputs.Numbers == null && outputs.NumbersLength == 0, "blittable receive array must be freed and reset");
                    Check(outputs.Types == null && outputs.TypesLength == 0, "resource receive array must be freed and reset");
                    Check(ReferenceCount(ccw) == references, "callee rollback must release COM outputs before caller cleanup");
                }
            }
            finally
            {
                outputs.Dispose();
            }

            Check(ReferenceCount(ccw) == references, "output rollback leaked or over-released COM references");
        }
    }

    private static int CallOutputs(void* ccw, NativeOutputs* outputs, bool finalReceiveArray)
    {
        if (finalReceiveArray)
        {
            int result = 0;
            int hr = ((delegate* unmanaged[MemberFunction]<void*, void*, void**, void**, void**, ABI.Contoso.NestedPayload*, ABI.System.Type*,
                void**, void**, uint*, int**, uint*, ABI.System.Type**, int*, int>)Entry(ccw, "GetFinalOutputs"))(
                ccw, ccw, &outputs->Text, &outputs->Interface, &outputs->Item, &outputs->Payload, &outputs->Type,
                &outputs->Optional, &outputs->Pair, &outputs->NumbersLength, &outputs->Numbers,
                &outputs->TypesLength, &outputs->Types, &result);
            if (hr == 0)
            {
                Check(result == 42, "blittable return after final receive array");
            }
            return hr;
        }

        return ((delegate* unmanaged[MemberFunction]<void*, void*, void**, void**, void**, ABI.Contoso.NestedPayload*, ABI.System.Type*,
            void**, void**, uint*, int**, uint*, ABI.System.Type**, ABI.System.Type*, int>)Entry(ccw, "GetOutputs"))(
            ccw, ccw, &outputs->Text, &outputs->Interface, &outputs->Item, &outputs->Payload, &outputs->Type,
            &outputs->Optional, &outputs->Pair, &outputs->NumbersLength, &outputs->Numbers,
            &outputs->TypesLength, &outputs->Types, &outputs->Result);
    }

    private static void VerifyFillRollback(Target target, void* ccw, int length, string failure)
    {
        target.OutputFailure = failure;
        ABI.Contoso.Stamp* stamps = stackalloc ABI.Contoso.Stamp[length];
        new Span<byte>(stamps, sizeof(ABI.Contoso.Stamp) * length).Clear();
        nint[] first = new nint[length];
        ABI.System.Type* secondData = stackalloc ABI.System.Type[length];
        new Span<byte>(secondData, sizeof(ABI.System.Type) * length).Clear();
        if (failure == "managed")
        {
            first.AsSpan().Fill(12345);
            for (int i = 0; i < length; i++)
            {
                secondData[i].Name = (void*)12345;
                stamps[i].Value = 12345;
            }
        }
        if (failure == "partial")
        {
            Stamp[] dirtyStamps = ArrayPool<Stamp>.Shared.Rent(length);
            dirtyStamps.AsSpan().Fill(new Stamp { When = DateTimeOffset.UnixEpoch, Value = 12345 });
            ArrayPool<Stamp>.Shared.Return(dirtyStamps);
            object[] dirtyObjects = ArrayPool<object>.Shared.Rent(length);
            dirtyObjects.AsSpan().Fill(target);
            ArrayPool<object>.Shared.Return(dirtyObjects);
        }

        uint references = ReferenceCount(ccw);
        fixed (nint* firstData = first)
        {
            ABI.System.Type result = default;
            try
            {
                int hr = ((delegate* unmanaged[MemberFunction]<void*, uint, void*, uint, void*, uint, void*, ABI.System.Type*, int>)Entry(ccw, "FillBoth"))(
                    ccw, (uint)length, stamps, (uint)length, failure == "null" ? null : firstData, (uint)length, secondData, &result);
                if (failure is "success" or "partial")
                {
                    Check(hr == 0, "fill output success HRESULT");
                    Check(TypeArrayMarshaller.ConvertToManaged((uint)length, secondData).All(type => type == typeof(int)), "fill Type values");
                    Check(ABI.System.TypeMarshaller.ConvertToManaged(result) == typeof(int), "fill return value");
                    for (int i = 0; i < length; i++)
                    {
                        Stamp stamp = ABI.Contoso.StampMarshaller.ConvertToManaged(stamps[i]);
                        bool written = failure != "partial" || i == 0;
                        Check((first[i] != 0) == written, "fill outputs contain only written COM references");
                        Check(stamp.When == (written ? DateTimeOffset.UnixEpoch : default) &&
                            stamp.Value == (written ? 42 : 0), "resource-free mapped struct fill");
                    }
                }
                else
                {
                    int expected = RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(
                        failure == "managed" ? new UnauthorizedAccessException() : new ArgumentNullException());
                    Check(hr == expected, "fill rollback HRESULT");
                    nint expectedPointer = failure == "managed" ? 12345 : 0;
                    Check(first.All(value => value == expectedPointer), "first native buffer ownership after failure");
                    for (int i = 0; i < length; i++)
                    {
                        Check((nint)secondData[i].Name == expectedPointer, "second native buffer ownership after failure");
                        if (failure == "managed")
                        {
                            Check(stamps[i].Value == 12345, "managed failure modified the resource-free buffer");
                        }
                    }
                    Check(result.Name == null, "failed fill return value");
                    Check(ReferenceCount(ccw) == references, "fill rollback leaked or over-released COM references");
                }
            }
            finally
            {
                if (failure != "managed")
                {
                    WindowsRuntimeUnknownArrayMarshaller.Dispose((uint)length, (void**)firstData);
                    TypeArrayMarshaller.Dispose((uint)length, secondData);
                }
                ABI.System.TypeMarshaller.Dispose(result);
            }
        }

        Check(ReferenceCount(ccw) == references, "fill output caller cleanup changed COM ownership");
    }

    private static void VerifySharedClear()
    {
        Span<byte> storage = stackalloc byte[2 * GuardSize + 3 * sizeof(ABI.Contoso.Payload)];
        storage.Fill(GuardValue);
        WindowsRuntimeArrayMarshaller.Clear(3, null, sizeof(ABI.Contoso.Payload));
        fixed (byte* data = storage)
        {
            WindowsRuntimeArrayMarshaller.Clear(0, data + GuardSize, sizeof(ABI.Contoso.Payload));
            Check(storage.IndexOfAnyExcept(GuardValue) < 0, "zero-length clear modified storage");
            WindowsRuntimeArrayMarshaller.Clear(3, data + GuardSize, sizeof(ABI.Contoso.Payload));
        }
        CheckClearedStorage(storage);
    }

    private static void VerifyStructFillRollback(void* ccw, int length)
    {
        Span<byte> payloadStorage = stackalloc byte[2 * GuardSize + length * sizeof(ABI.Contoso.Payload)];
        Span<byte> typeStorage = stackalloc byte[2 * GuardSize + length * sizeof(ABI.System.Type)];
        payloadStorage.Fill(GuardValue);
        typeStorage.Fill(GuardValue);
        fixed (byte* payloadData = payloadStorage)
        fixed (byte* typeData = typeStorage)
        {
            ABI.System.Type result = default;
            int hr = ((delegate* unmanaged[MemberFunction]<void*, uint, void*, uint, void*, ABI.System.Type*, int>)Entry(ccw, "FillPayloadsAndTypes"))(
                ccw, (uint)length, payloadData + GuardSize, (uint)length, typeData + GuardSize, &result);
            Check(hr == RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(new ArgumentNullException()),
                "struct fill rollback HRESULT");
            Check(result.Name == null, "failed struct fill return value");
        }
        CheckClearedStorage(payloadStorage);
        CheckClearedStorage(typeStorage);
    }

    private static void CheckClearedStorage(ReadOnlySpan<byte> storage)
    {
        Check(storage[..GuardSize].IndexOfAnyExcept(GuardValue) < 0 &&
            storage[^GuardSize..].IndexOfAnyExcept(GuardValue) < 0, "clear modified a guard region");
        Check(storage[GuardSize..^GuardSize].IndexOfAnyExcept((byte)0) < 0, "ABI buffer was not completely cleared");
    }

    private static uint ReferenceCount(void* ccw)
    {
        uint count = ((delegate* unmanaged[MemberFunction]<void*, uint>)(*(void***)ccw)[1])(ccw);
        _ = ((delegate* unmanaged[MemberFunction]<void*, uint>)(*(void***)ccw)[2])(ccw);
        return count;
    }

    private struct NativeOutputs
    {
        public void* Text;
        public void* Interface;
        public void* Item;
        public ABI.Contoso.NestedPayload Payload;
        public ABI.System.Type Type;
        public void* Optional;
        public void* Pair;
        public uint NumbersLength;
        public int* Numbers;
        public uint TypesLength;
        public ABI.System.Type* Types;
        public ABI.System.Type Result;

        public readonly void Dispose()
        {
            HStringMarshaller.Free(Text);
            WindowsRuntimeUnknownMarshaller.Free(Interface);
            WindowsRuntimeUnknownMarshaller.Free(Item);
            WindowsRuntimeUnknownMarshaller.Free(Optional);
            WindowsRuntimeUnknownMarshaller.Free(Pair);
            ABI.Contoso.NestedPayloadMarshaller.Dispose(Payload);
            ABI.System.TypeMarshaller.Dispose(Type);
            Marshal.FreeCoTaskMem((nint)Numbers);
            TypeArrayMarshaller.Free(TypesLength, Types);
            ABI.System.TypeMarshaller.Dispose(Result);
        }
    }

    private static void VerifyArrays(Target target, void* ccw, int length, bool methods = false)
    {
        int[] expectedValues = Enumerable.Range(1, length).ToArray();
        int[] nativeValues = (int[])expectedValues.Clone();
        fixed (int* data = nativeValues)
        {
            Check(Set(ccw, "Values", (uint)length, data) == 0, "int setter");
            Check(nativeValues.SequenceEqual(expectedValues), "setter modified native input");
            if (methods)
            {
                Check(CallArrayMethod(ccw, "AcceptValues", (uint)length, data) == 0, "int method");
                Check(target.SeenValues.SequenceEqual(expectedValues), "int method contents");
            }
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
            if (methods)
            {
                Check(CallArrayMethod(ccw, "AcceptNames", namesLength, nativeNames) == 0, "string method");
                Check(target.SeenNames.SequenceEqual(names), "string method contents");
            }
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
            if (methods)
            {
                Check(CallArrayMethod(ccw, "AcceptObjects", objectsLength, nativeObjects) == 0, "object method");
                Check(target.SeenObjects.SequenceEqual(objects), "object method contents");
            }
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
            if (methods)
            {
                Check(CallArrayMethod(ccw, "AcceptInterfaces", (uint)length, data) == 0, "interface method");
                Check(target.SeenInterfaces.SequenceEqual(Enumerable.Repeat<IWidget2>(target, length)), "interface method contents");
            }
        }
        CheckOwned(target.Interfaces, Enumerable.Repeat<IWidget2>(target, length).ToArray());

        nint[] widgets = new nint[length];
        fixed (nint* data = widgets)
        {
            Check(Set(ccw, "Widgets", (uint)length, data) == 0, "runtime class setter");
            if (methods)
            {
                Check(CallArrayMethod(ccw, "AcceptWidgets", (uint)length, data) == 0, "runtime class method");
                Check(target.SeenWidgets.Length == length && target.SeenWidgets.All(widget => widget is null), "runtime class method contents");
            }
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
                if (methods)
                {
                    Check(CallArrayMethod(ccw, "AcceptPayloads", (uint)length, data) == 0, "struct method");
                    Check(target.SeenPayloads.SequenceEqual(payloads), "struct method contents");
                }
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

        if (methods)
        {
            DateTimeOffset[] dates = Enumerable.Range(0, length).Select(i => DateTimeOffset.FromUnixTimeSeconds(i)).ToArray();
            ABI.System.DateTimeOffset[] nativeDates = dates.Select(ABI.System.DateTimeOffsetMarshaller.ConvertToUnmanaged).ToArray();
            fixed (ABI.System.DateTimeOffset* data = nativeDates)
            {
                Check(CallArrayMethod(ccw, "AcceptDates", (uint)length, data) == 0, "mapped value method");
                Check(target.SeenDates.SequenceEqual(dates), "mapped value method contents");
            }

            nint[] filledNames = new nint[length];
            fixed (nint* data = filledNames)
            {
                try
                {
                    Check(CallArrayMethod(ccw, "FillNames", (uint)length, data) == 0, "string fill method");
                    Check(HStringArrayMarshaller.ConvertToManaged((uint)length, (void**)data)
                        .SequenceEqual(Enumerable.Range(0, length).Select(i => $"filled-{i}")), "string fill contents");
                }
                finally
                {
                    HStringArrayMarshaller.Dispose((uint)length, (void**)data);
                }
            }
        }

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

    private static void VerifyMarshalledObjectCollection(Target target, void* ccw, int length)
    {
        WeakReference value = PassObjectToGeneratedCallback(target, ccw, length);
        for (int attempt = 0; attempt < 5 && value.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        Check(!value.IsAlive, "generated callback retains a marshalled object after the call");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference PassObjectToGeneratedCallback(Target target, void* ccw, int length)
    {
        object value = new();
        object[] values = Enumerable.Repeat(value, length).ToArray();
        WeakReference reference = new(value);
        WindowsRuntimeObjectArrayMarshaller.ConvertToUnmanaged(values, out uint size, out void** native);
        try
        {
            Check(CallArrayMethod(ccw, "AcceptObjects", size, native) == 0, "object callback");
            Check(target.SeenObjects.Length == length && target.SeenObjects.All(item => ReferenceEquals(item, value)),
                "generated callback did not receive the marshalled object");
            target.ForgetSeenObjects();
        }
        finally
        {
            WindowsRuntimeUnknownArrayMarshaller.Free(size, native);
        }
        return reference;
    }

    private static void VerifyMappedRental(Target target, void* ccw, int length)
    {
        using WindowsRuntimeObjectReference reference =
            WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(ccw, in ABI.Contoso.IWidget2Impl.IID, out _);
        DateTimeOffset[] values = Enumerable.Range(0, length).Select(i => DateTimeOffset.FromUnixTimeSeconds(i)).ToArray();
        ABI.Contoso.IWidget2Methods.AcceptDates(reference, values);
        ABI.System.DateTimeOffset[] expected = ArrayPool<ABI.System.DateTimeOffset>.Shared.Rent(length);
        ArrayPool<ABI.System.DateTimeOffset>.Shared.Return(expected);
        ABI.Contoso.IWidget2Methods.AcceptDates(reference, values);
        Check(target.SeenDates.SequenceEqual(values), "projected mapped array contents");
        ABI.System.DateTimeOffset[] returned = ArrayPool<ABI.System.DateTimeOffset>.Shared.Rent(length);
        try
        {
            Check(ReferenceEquals(expected, returned), "projection did not return its temporary ABI buffer");
        }
        finally
        {
            ArrayPool<ABI.System.DateTimeOffset>.Shared.Return(returned);
        }
    }

    private static void VerifyResourceFreeArrays(Target target, void* ccw, int length)
    {
        using WindowsRuntimeObjectReference reference =
            WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(ccw, in ABI.Contoso.IWidget2Impl.IID, out _);
        Stamp[] stamps = Enumerable.Range(0, length).Select(i => new Stamp
        {
            When = DateTimeOffset.FromUnixTimeSeconds(i),
            Value = i + 1
        }).ToArray();
        ABI.Contoso.IWidget2Methods.AcceptStamps(reference, stamps);
        Check(target.SeenStamps.Length == length, "resource-free pass array length");
        for (int i = 0; i < length; i++)
        {
            Check(target.SeenStamps[i].When == stamps[i].When &&
                target.SeenStamps[i].Value == stamps[i].Value, "resource-free pass array contents");
        }

        object[] first = new object[length];
        Type[] second = new Type[length];
        Type result = ABI.Contoso.IWidget2Methods.FillBoth(reference, stamps, first, second);
        Check(result == typeof(int), "resource-free fill array return");
        Check(stamps.All(stamp => stamp.When == DateTimeOffset.UnixEpoch && stamp.Value == 42),
            "resource-free fill array contents");
        Check(first.All(value => ReferenceEquals(value, target)) && second.All(type => type == typeof(int)),
            "resource-owning fill array contents");
    }

    private static void VerifyArrayFailure(Target target, void* ccw, int length, bool passArray)
    {
        using WindowsRuntimeObjectReference reference =
            WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(ccw, in ABI.Contoso.IWidget2Impl.IID, out _);
        object[] values = Enumerable.Range(0, length).Select(i => (object)new Uri($"https://example.com/{i}")).ToArray();
        ABI.Contoso.IWidget2Methods.AcceptObjects(reference, values.AsSpan(0, 1));
        values[length / 2] = new Uri("relative", UriKind.Relative);
        void** vtable = stackalloc void*[3];
        vtable[0] = null;
        vtable[1] = null;
        vtable[2] = (delegate* unmanaged[MemberFunction]<void*, uint>)&ReleaseMarker;
        NativeMarker marker = new() { Vtable = vtable };
        nint[] poisoned = ArrayPool<nint>.Shared.Rent(length);
        poisoned.AsSpan().Fill((nint)(&marker));
        ArrayPool<nint>.Shared.Return(poisoned);
        target.ThrowOnFill = !passArray;
        bool failed = false;
        bool seedReturned = false;
        try
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                failed = false;
                try
                {
                    if (passArray)
                    {
                        ABI.Contoso.IWidget2Methods.AcceptObjects(reference, values);
                    }
                    else
                    {
                        ABI.Contoso.IWidget2Methods.FillObjects(reference, new object[length]);
                    }
                }
                catch (ArgumentException) when (passArray)
                {
                    failed = true;
                }
                catch (UnauthorizedAccessException) when (!passArray)
                {
                    failed = true;
                }
                Check(failed, "projected array call did not propagate the expected failure");
            }
        }
        finally
        {
            nint[] cleanup = ArrayPool<nint>.Shared.Rent(length);
            try
            {
                seedReturned = ReferenceEquals(poisoned, cleanup);
            }
            finally
            {
                ArrayPool<nint>.Shared.Return(cleanup, clearArray: true);
            }
        }
        Check(seedReturned, "projection did not return the seeded ABI buffer");
        Check(marker.Releases == 0, "failure cleanup released stale pooled COM pointers");
    }

    private struct NativeMarker
    {
        public void** Vtable;
        public int Releases;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint ReleaseMarker(void* value)
    {
        ((NativeMarker*)value)->Releases++;
        return 1;
    }

    private static int CallArrayMethod(void* ccw, string name, uint length, void* data)
    {
        return ((delegate* unmanaged[MemberFunction]<void*, uint, void*, int>)Entry(ccw, name))(ccw, length, data);
    }

    private static void VerifyArrayAbi(Target target, void* ccw, string elementType, int length)
    {
        target.ArrayLength = length;
        using WindowsRuntimeObjectReference reference =
            WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(ccw, in ABI.Contoso.IWidget2Impl.IID, out _);
        switch (elementType)
        {
            case "nullable":
                int?[] nullable = Target.NullableValues(length);
                ABI.Contoso.IWidget2Methods.AcceptNullable(reference, nullable);
                Check(target.SeenNullable.SequenceEqual(nullable), "nullable pass array");
                int?[] nullableFill = new int?[length];
                ABI.Contoso.IWidget2Methods.FillNullable(reference, nullableFill);
                Check(nullableFill.SequenceEqual(nullable), "nullable fill array");
                Check(ABI.Contoso.IWidget2Methods.GetNullable(reference).SequenceEqual(nullable), "nullable return array");
                ABI.Contoso.IWidget2Methods.ReceiveNullable(reference, out int?[] nullableReceive);
                Check(nullableReceive.SequenceEqual(nullable), "nullable receive array");
                break;
            case "pairs":
                KeyValuePair<string, string>[] pairs = Target.PairValues(length);
                ABI.Contoso.IWidget2Methods.AcceptPairs(reference, pairs);
                Check(target.SeenPairs.SequenceEqual(pairs), "KVP pass array");
                KeyValuePair<string, string>[] pairsFill = new KeyValuePair<string, string>[length];
                ABI.Contoso.IWidget2Methods.FillPairs(reference, pairsFill);
                Check(pairsFill.SequenceEqual(pairs), "KVP fill array");
                Check(ABI.Contoso.IWidget2Methods.GetPairs(reference).SequenceEqual(pairs), "KVP return array");
                ABI.Contoso.IWidget2Methods.ReceivePairs(reference, out KeyValuePair<string, string>[] pairsReceive);
                Check(pairsReceive.SequenceEqual(pairs), "KVP receive array");
                break;
            case "types":
                Type[] types = Target.TypeValues(length);
                ABI.Contoso.IWidget2Methods.AcceptTypes(reference, types);
                Check(target.SeenTypes.SequenceEqual(types), "Type pass array");
                Type[] typesFill = new Type[length];
                ABI.Contoso.IWidget2Methods.FillTypes(reference, typesFill);
                Check(typesFill.SequenceEqual(types), "Type fill array");
                Check(ABI.Contoso.IWidget2Methods.GetTypes(reference).SequenceEqual(types), "Type return array");
                ABI.Contoso.IWidget2Methods.ReceiveTypes(reference, out Type[] typesReceive);
                Check(typesReceive.SequenceEqual(types), "Type receive array");
                break;
            default:
                throw new ArgumentException("Unknown array element type.", nameof(elementType));
        }
    }

    private static void* Entry(void* ccw, string name)
    {
        Type vtable = Type.GetType("ABI.Contoso.IWidget2Vftbl, WinRT.Sdk.Projection", throwOnError: true)!;
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
        public string OutputFailure { get; set; } = "success";

        public void GetName(out string name)
        {
            name = "scalar";
            if (OutputFailure == "managed")
            {
                throw new UnauthorizedAccessException();
            }
        }

        public void GetScalarType(out Type value)
        {
            value = OutputFailure == "conversion" ? null : typeof(int);
            if (OutputFailure == "managed")
            {
                throw new UnauthorizedAccessException();
            }
        }

        public Type GetOutputs(object input, out string text, out IWidget2 iface, out object item, out NestedPayload payload, out Type type,
            out int? optional, out KeyValuePair<string, string> pair, out int[] numbers, out Type[] types)
        {
            Check(ReferenceEquals(input, this), "borrowed input identity");
            text = "output";
            iface = this;
            item = this;
            payload = new NestedPayload
            {
                Inner = new Payload { Id = 1, Text = "payload", Timestamp = DateTimeOffset.FromUnixTimeSeconds(1) },
                Text = "outer"
            };
            type = OutputFailure == "type" ? null : typeof(int);
            optional = 42;
            pair = new("key", "value");
            numbers = [1, 2, 3];
            types = TypeValues(3);
            if (OutputFailure == "array")
            {
                types[1] = null;
            }
            if (OutputFailure == "managed")
            {
                throw new UnauthorizedAccessException();
            }
            return OutputFailure == "return" ? null : typeof(int);
        }

        public int GetFinalOutputs(object input, out string text, out IWidget2 iface, out object item, out NestedPayload payload, out Type type,
            out int? optional, out KeyValuePair<string, string> pair, out int[] numbers, out Type[] types)
        {
            _ = GetOutputs(input, out text, out iface, out item, out payload, out type, out optional, out pair, out numbers, out types);
            return 42;
        }

        public Type FillBoth(Span<Stamp> stamps, Span<object> first, Span<Type> second)
        {
            if (OutputFailure == "managed")
            {
                throw new UnauthorizedAccessException();
            }
            int written = OutputFailure == "partial" ? Math.Min(1, stamps.Length) : stamps.Length;
            stamps[..written].Fill(new Stamp { When = DateTimeOffset.UnixEpoch, Value = 42 });
            int objectsWritten = OutputFailure == "partial" ? Math.Min(1, first.Length) : first.Length;
            first[..objectsWritten].Fill(this);
            second.Fill(typeof(int));
            if (OutputFailure.StartsWith("copy", StringComparison.Ordinal))
            {
                int index = OutputFailure == "copy-first" ? 0 : OutputFailure == "copy-last" ? second.Length - 1 : second.Length / 2;
                second[index] = null;
            }
            return OutputFailure == "return" ? null : typeof(int);
        }

        public Type FillPayloadsAndTypes(Span<Payload> first, Span<Type> second)
        {
            first.Fill(new Payload { Id = 42, Text = "owned", Timestamp = DateTimeOffset.UnixEpoch });
            second.Fill(typeof(int));
            // Fail return conversion after both native fill buffers have acquired resources
            return null;
        }

        public bool ThrowOnSet { get; set; }
        public bool ThrowOnFill { get; set; }
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
        public int ArrayLength { get; set; }
        public int?[] SeenNullable { get; private set; } = [];
        public KeyValuePair<string, string>[] SeenPairs { get; private set; } = [];
        public Type[] SeenTypes { get; private set; } = [];
        public static int?[] NullableValues(int length) => Enumerable.Range(0, length).Select(i => i % 3 == 0 ? (int?)null : i).ToArray();
        public static KeyValuePair<string, string>[] PairValues(int length) => Enumerable.Range(0, length).Select(i => new KeyValuePair<string, string>($"key-{i}", $"value-{i}")).ToArray();
        public static Type[] TypeValues(int length) => Enumerable.Range(0, length).Select(i => (i % 3) switch { 0 => typeof(int), 1 => typeof(string), _ => typeof(Payload) }).ToArray();
        public void AcceptNullable(ReadOnlySpan<int?> value) => SeenNullable = value.ToArray();
        public void AcceptPairs(ReadOnlySpan<KeyValuePair<string, string>> value) => SeenPairs = value.ToArray();
        public void AcceptTypes(ReadOnlySpan<Type> value) => SeenTypes = value.ToArray();
        public void FillNullable(Span<int?> value) => NullableValues(value.Length).AsSpan().CopyTo(value);
        public void FillPairs(Span<KeyValuePair<string, string>> value) => PairValues(value.Length).AsSpan().CopyTo(value);
        public void FillTypes(Span<Type> value) => TypeValues(value.Length).AsSpan().CopyTo(value);
        public int?[] GetNullable() => NullableValues(ArrayLength);
        public KeyValuePair<string, string>[] GetPairs() => PairValues(ArrayLength);
        public Type[] GetTypes() => TypeValues(ArrayLength);
        public void ReceiveNullable(out int?[] value) => value = GetNullable();
        public void ReceivePairs(out KeyValuePair<string, string>[] value) => value = GetPairs();
        public void ReceiveTypes(out Type[] value) => value = GetTypes();
        public string[] SeenNames { get; private set; } = [];
        public object[] SeenObjects { get; private set; } = [];
        public Widget[] SeenWidgets { get; private set; } = [];
        public IWidget2[] SeenInterfaces { get; private set; } = [];
        public Payload[] SeenPayloads { get; private set; } = [];
        public DateTimeOffset[] SeenDates { get; private set; } = [];
        public Stamp[] SeenStamps { get; private set; } = [];
        public void AcceptStamps(ReadOnlySpan<Stamp> value) => SeenStamps = value.ToArray();
        public void AcceptValues(ReadOnlySpan<int> value) => SeenValues = value.ToArray();
        public void AcceptNames(ReadOnlySpan<string> value) => SeenNames = value.ToArray();
        public void AcceptObjects(ReadOnlySpan<object> value) => SeenObjects = value.ToArray();
        public void ForgetSeenObjects() => SeenObjects = [];
        public void AcceptWidgets(ReadOnlySpan<Widget> value) => SeenWidgets = value.ToArray();
        public void AcceptInterfaces(ReadOnlySpan<IWidget2> value) => SeenInterfaces = value.ToArray();
        public void AcceptPayloads(ReadOnlySpan<Payload> value) => SeenPayloads = value.ToArray();
        public void AcceptDates(ReadOnlySpan<DateTimeOffset> value) => SeenDates = value.ToArray();
        public void FillNames(Span<string> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = $"filled-{i}";
            }
        }
        public void FillObjects(Span<object> value)
        {
            if (ThrowOnFill)
            {
                throw new UnauthorizedAccessException();
            }
            value.Clear();
        }
        public void FillValues(Span<int> value) => value.Fill(42);
    }
}
