// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WindowsRuntime.SourceGenerator.Tests;

public partial class Test_GetIidInterceptorGenerator
{
    private const string CoverageAssemblyName = "IidCoverageConsumer";

    // Reference-shaped metadata intentionally omits implementation-only markers, and records the (internal) default interface by name
    private const string CoverageProjection = """
        using System;
        using System.Runtime.CompilerServices;
        using System.Runtime.InteropServices;
        using WindowsRuntime.InteropServices;

        [assembly: WindowsRuntimeReferenceAssembly]
        [assembly: InternalsVisibleTo("IidCoverageConsumer")]
        [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.DefaultInterface.v1:Projection.RuntimeClass", "Projection.IRuntimeClass")]
        [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.DefaultInterface.v1:Projection.GenericDefaultClass", "System.Collections.Generic.IList<int>")]
        [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.DefaultInterface.v1:Projection.MappedMapClass", "System.Collections.Generic.IDictionary<string, string>")]
        [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.DefaultInterface.v1:Projection.MappedBindableClass", "System.Collections.IEnumerable")]
        [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.DefaultInterface.v1:Projection.MappedXamlClass", "System.ComponentModel.INotifyPropertyChanged")]
        [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.DefaultInterface.v1:Projection.MappedStructListClass", "System.Collections.Generic.IList<System.TimeSpan>")]

        namespace Projection
        {
            [Guid("97D098A5-3B99-4DE9-88A5-E11D2F50C795")]
            public interface IProjected { }

            [Guid("E2B50029-B4C1-4314-A4B8-FB813A2F275E")]
            internal interface IRuntimeClass { }

            [Guid("1D1A8B8B-FA66-414F-9CBD-B65FC99D17FA")]
            public delegate void Handler();

            [Guid("D7527847-57DA-4E15-914C-06806699A098")]
            internal interface IExclusive { }

            public sealed class RuntimeClass { }
            public sealed class GenericDefaultClass { }

            // Runtime classes whose default interface is custom-mapped to a .NET type
            public sealed class MappedMapClass { }
            public sealed class MappedBindableClass { }
            public sealed class MappedXamlClass { }
            public sealed class MappedStructListClass { }
            public struct Value { public int Number; public string Text; }
            public enum EnumValue { Zero }
            [Flags]
            public enum Flags : uint { None }
            public struct Composite
            {
                public double Number;
                public TimeSpan Time;
                public EnumValue Choice;
                public Value Nested;
                public bool Flag;
                public char Character;
                public Guid Identifier;
            }
        }
        """;

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void TypeCategories_ReturnExpectedIids(bool useUwp)
    {
        List<(string Type, Guid Iid)> cases =
        [
            ("System.IDisposable", new("30d5a829-7fa4-4026-83bb-d75bae4ea99e")),
            ("System.ComponentModel.INotifyPropertyChanged", new(useUwp ? "cf75d69c-f2f4-486b-b302-bb4c09baebfa" : "90b17601-b065-586e-83d9-9adc3a695284")),
            ("System.Collections.Specialized.INotifyCollectionChanged", new(useUwp ? "28b167d5-1a31-465b-9b25-d5c3ae686c40" : "530155e1-28a5-5693-87ce-30724d95a06d")),
            ("System.ComponentModel.INotifyDataErrorInfo", new("0ee6c2cc-273e-567d-bc0a-1dd87ee51eba")),
            ("System.Windows.Input.ICommand", new("e5af3542-ca67-4081-995b-709dd13792df")),
            ("System.IServiceProvider", new("68b3a2df-8173-539f-b524-c8a2348f5afb")),
            ("System.Collections.IEnumerable", new("036d2c08-df29-41af-8aa2-d774be62ba6f")),
            ("System.Collections.IEnumerator", new("6a1d6c07-076d-49f2-8314-f52c9c9a8331")),
            ("System.Collections.IList", new("393de7de-6fd0-4c0d-bb71-47244a113e93")),
            ("Windows.Foundation.IStringable", new("96369f54-8eb6-48f0-abce-c1b211e627c3")),
            ("Windows.Storage.Streams.IBuffer", new("905a0fe0-bc53-11df-8c49-001e4fc686da")),
            ("Windows.Foundation.IAsyncAction", new("5a648006-843a-4da9-865b-9d26e5dfad7b")),
            ("Windows.Foundation.IAsyncInfo", new("00000036-0000-0000-c000-000000000046")),
            ("Windows.Foundation.Collections.IVectorChangedEventArgs", new("575933df-34fe-4480-af15-07691f3d5d9b")),
            ("Windows.Foundation.IMemoryBufferReference", new("fbc4dd29-245b-11e4-af98-689423260cf8")),
            ("Windows.Storage.Streams.IInputStream", new("905a0fe2-bc53-11df-8c49-001e4fc686da")),
            ("Windows.Storage.Streams.IOutputStream", new("905a0fe6-bc53-11df-8c49-001e4fc686da")),
            ("Windows.Storage.Streams.IRandomAccessStream", new("905a0fe1-bc53-11df-8c49-001e4fc686da")),
            ("Projection.IProjected", new("97d098a5-3b99-4de9-88a5-e11d2f50c795")),
            ("System.ComponentModel.PropertyChangedEventArgs", new(useUwp ? "4f33a9a0-5cf4-47a4-b16f-d7faaf17457e" : "63d0c952-396b-54f4-af8c-ba8724a427bf")),
            ("System.Collections.Specialized.NotifyCollectionChangedEventArgs", new(useUwp ? "4cf68d33-e3f2-4964-b85e-945b4f7e2f21" : "da049ff2-d2e0-5fe8-8c7b-f87f26060b6f")),
            ("System.ComponentModel.PropertyChangedEventHandler", new(useUwp ? "50f19c16-0a22-4d8e-a089-1ea9951657d2" : "e3de52f6-1e32-5da6-bb2d-b5b6096c962d")),
            ("System.Collections.Specialized.NotifyCollectionChangedEventHandler", new(useUwp ? "ca10b37c-f382-4591-8557-5e24965279b0" : "8b0909dc-2005-5d93-bf8a-725f017baa8d")),
            ("Windows.Foundation.AsyncActionCompletedHandler", new("a4ed5c81-76c9-40bd-8be6-b1d90fb20ae7")),
            ("Projection.Handler", new("1d1a8b8b-fa66-414f-9cbd-b65fc99d17fa")),
            ("Projection.RuntimeClass", new("e2b50029-b4c1-4314-a4b8-fb813a2f275e")),
            ("Projection.GenericDefaultClass", new("b939af5b-b45d-5489-9149-61442c1905fe")),
            ("Projection.MappedMapClass", new("f6d1f700-49c2-52ae-8154-826f9908773c")),
            ("Projection.MappedBindableClass", new("036d2c08-df29-41af-8aa2-d774be62ba6f")),
            ("Projection.MappedXamlClass", new(useUwp ? "cf75d69c-f2f4-486b-b302-bb4c09baebfa" : "90b17601-b065-586e-83d9-9adc3a695284")),
            ("Projection.MappedStructListClass", CreateExpectedParameterizedIid("pinterface({913337e9-11a1-4345-a3a2-4e7f956e222d};struct(Windows.Foundation.TimeSpan;i8))")),
            ("Projection.IExclusive", new("d7527847-57da-4e15-914c-06806699a098"))
        ];

        AssertInterceptedIids(cases, useUwp);
    }

    public static IEnumerable<object[]> GetSupportedGenericFamilies()
    {
        (string Type, string Iid, int Arity)[] families =
        [
            ("System.Collections.Generic.IEnumerable", "faa585ea-6214-4217-afda-7f46de5869b3", 1),
            ("System.Collections.Generic.IEnumerator", "6a79e863-4300-459a-9966-cbb660963ee1", 1),
            ("System.Collections.Generic.IList", "913337e9-11a1-4345-a3a2-4e7f956e222d", 1),
            ("System.Collections.Generic.IReadOnlyList", "bbe1fa4c-b0e3-4583-baef-1f1b2e483e56", 1),
            ("System.Collections.Generic.IDictionary", "3c2925fe-8519-45c1-aa79-197b6718c1c1", 2),
            ("System.Collections.Generic.IReadOnlyDictionary", "e480ce40-a338-4ada-adcf-272272e48cb9", 2),
            ("Windows.Foundation.IAsyncOperation", "9fc2b0bb-e446-44e2-aa61-9cab8f636af2", 1),
            ("Windows.Foundation.IAsyncActionWithProgress", "1f6db258-e803-48a1-9546-eb7353398884", 1),
            ("Windows.Foundation.IAsyncOperationWithProgress", "b5d036d7-e297-498f-ba60-0289e76e23dd", 2),
            ("Windows.Foundation.Collections.IObservableVector", "5917eb53-50b4-4a0d-b309-65862b3f1dbc", 1),
            ("Windows.Foundation.Collections.IObservableMap", "65df2bf5-bf39-41b5-aebc-5a9d865e472b", 2),
            ("Windows.Foundation.Collections.IMapChangedEventArgs", "9939f4df-050a-4c0f-aa60-77075f9c4777", 1),
            ("System.EventHandler", "9de1c535-6ae1-11e0-84e1-18a905bcc53f", 1),
            ("System.EventHandler", "9de1c534-6ae1-11e0-84e1-18a905bcc53f", 2),
            ("Windows.Foundation.AsyncActionProgressHandler", "6d844858-0cff-4590-ae89-95a5a5c8b4b8", 1),
            ("Windows.Foundation.AsyncActionWithProgressCompletedHandler", "9c029f91-cc84-44fd-ac26-0a6c4e555281", 1),
            ("Windows.Foundation.AsyncOperationCompletedHandler", "fcdcf02c-e5d8-4478-915a-4d90b74b83a5", 1),
            ("Windows.Foundation.AsyncOperationProgressHandler", "55690902-0aab-421a-8778-f8ce5026d758", 2),
            ("Windows.Foundation.AsyncOperationWithProgressCompletedHandler", "e85df41d-6aa7-46e3-a8e2-f009d840c627", 2),
            ("Windows.Foundation.Collections.VectorChangedEventHandler", "0c051752-9fbf-4c70-aa0c-0e4c82d9a761", 1),
            ("Windows.Foundation.Collections.MapChangedEventHandler", "179517f3-94ee-41f8-bddc-768a895544f3", 2),
            ("System.Collections.Generic.KeyValuePair", "02b51929-c1c4-4a7e-8940-0312b5c18500", 2),
            ("System.Nullable", "61c17706-2d65-11e0-9ae8-d48564015472", 1)
        ];

        foreach (var family in families)
        {
            yield return [family.Type, family.Iid, family.Arity, false];
            yield return [family.Type, family.Iid, family.Arity, true];
        }
    }

    [TestMethod]
    [DynamicData(nameof(GetSupportedGenericFamilies))]
    public void SupportedGenericFamily_AllTypeCategoriesReturnExpectedIids(string genericType, string definitionIid, int arity, bool useUwp)
    {
        CoverageType[] arguments = GetCoverageTypes(useUwp);
        if (genericType == "System.Nullable")
        {
            arguments = [.. arguments.Where(static type => type.IsValueType && !type.Type.EndsWith('?') &&
                !type.Type.StartsWith("System.Collections.Generic.KeyValuePair", StringComparison.Ordinal))];
        }

        List<(string Type, Guid Iid)> cases = [];
        foreach (CoverageType first in arguments)
        {
            for (int index = 0; index < (arity == 2 ? arguments.Length : 1); index++)
            {
                CoverageType? second = arity == 2 ? arguments[index] : null;
                string type = $"{genericType}<{first.Type}{(second is null ? "" : $", {second.Type}")}>";
                string signature = $"pinterface({{{definitionIid}}};{first.Signature}{(second is null ? "" : $";{second.Signature}")})";
                cases.Add((type, CreateExpectedParameterizedIid(signature)));
                cases.Add(($"{type}[]", CreateExpectedParameterizedIid($"pinterface({{61c17707-2d65-11e0-9ae8-d48564015472}};{signature})")));
            }
        }

        AssertInterceptedIids(cases, useUwp);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SzArrays_AllTypeCategoriesReturnExpectedReferenceArrayIids(bool useUwp)
    {
        List<(string Type, Guid Iid)> cases = [.. GetCoverageTypes(useUwp).Select(static type =>
            (type.Type + "[]", CreateExpectedParameterizedIid($"pinterface({{61c17707-2d65-11e0-9ae8-d48564015472}};{type.Signature})")))];

        AssertInterceptedIids(cases, useUwp);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AdditionalSignatures_ArraysGenericsAndNullableValuesReturnExpectedIids(bool useUwp)
    {
        CoverageType[] types =
        [
            new("bool", "b1", IsValueType: true),
            new("byte", "u1", IsValueType: true),
            new("char", "c2", IsValueType: true),
            new("short", "i2", IsValueType: true),
            new("ushort", "u2", IsValueType: true),
            new("uint", "u4", IsValueType: true),
            new("long", "i8", IsValueType: true),
            new("ulong", "u8", IsValueType: true),
            new("float", "f4", IsValueType: true),
            new("double", "f8", IsValueType: true),
            new("System.DateTimeOffset", "struct(Windows.Foundation.DateTime;i8)", IsValueType: true),
            new("System.Exception", "struct(Windows.Foundation.HResult;i4)"),
            new("System.Type", "struct(Windows.UI.Xaml.Interop.TypeName;string;enum(Windows.UI.Xaml.Interop.TypeKind;i4))"),
            new("System.Numerics.Plane", "struct(Windows.Foundation.Numerics.Plane;struct(Windows.Foundation.Numerics.Vector3;f4;f4;f4);f4)", IsValueType: true),
            new("Windows.Storage.Streams.InputStreamOptions", "enum(Windows.Storage.Streams.InputStreamOptions;u4)", IsValueType: true),
            new("Projection.Flags", "enum(Projection.Flags;u4)", IsValueType: true),
            new("Projection.Composite", "struct(Projection.Composite;f8;struct(Windows.Foundation.TimeSpan;i8);enum(Projection.EnumValue;i4);struct(Projection.Value;i4;string);b1;c2;g16)", IsValueType: true),
            new("System.EventHandler<object, string>", "pinterface({9de1c534-6ae1-11e0-84e1-18a905bcc53f};cinterface(IInspectable);string)")
        ];
        List<(string Type, Guid Iid)> cases = [];
        foreach (CoverageType type in types)
        {
            cases.Add(($"{type.Type}[]", CreateExpectedParameterizedIid($"pinterface({{61c17707-2d65-11e0-9ae8-d48564015472}};{type.Signature})")));
            cases.Add(($"System.Collections.Generic.IList<{type.Type}>", CreateExpectedParameterizedIid($"pinterface({{913337e9-11a1-4345-a3a2-4e7f956e222d}};{type.Signature})")));
            cases.Add(($"System.Collections.Generic.KeyValuePair<string, {type.Type}>", CreateExpectedParameterizedIid($"pinterface({{02b51929-c1c4-4a7e-8940-0312b5c18500}};string;{type.Signature})")));
            cases.Add(($"System.Collections.Generic.KeyValuePair<{type.Type}, string>", CreateExpectedParameterizedIid($"pinterface({{02b51929-c1c4-4a7e-8940-0312b5c18500}};{type.Signature};string)")));
            if (type.IsValueType)
            {
                cases.Add(($"{type.Type}?", CreateExpectedParameterizedIid($"pinterface({{61c17706-2d65-11e0-9ae8-d48564015472}};{type.Signature})")));
            }
        }

        AssertInterceptedIids(cases, useUwp);
    }

    private static CoverageType[] GetCoverageTypes(bool useUwp) =>
    [
        new("System.IDisposable", "{30d5a829-7fa4-4026-83bb-d75bae4ea99e}"),
        new("Windows.Foundation.IStringable", "{96369f54-8eb6-48f0-abce-c1b211e627c3}"),
        new("Projection.IProjected", "{97d098a5-3b99-4de9-88a5-e11d2f50c795}"),
        new("System.ComponentModel.PropertyChangedEventHandler", $"delegate({{{(useUwp ? "50f19c16-0a22-4d8e-a089-1ea9951657d2" : "e3de52f6-1e32-5da6-bb2d-b5b6096c962d")}}})"),
        new("Windows.Foundation.AsyncActionCompletedHandler", "delegate({a4ed5c81-76c9-40bd-8be6-b1d90fb20ae7})"),
        new("Projection.Handler", "delegate({1d1a8b8b-fa66-414f-9cbd-b65fc99d17fa})"),
        new("Projection.RuntimeClass", "rc(Projection.RuntimeClass;{e2b50029-b4c1-4314-a4b8-fb813a2f275e})"),
        new("Projection.GenericDefaultClass", "rc(Projection.GenericDefaultClass;pinterface({913337e9-11a1-4345-a3a2-4e7f956e222d};i4))"),
        new("Projection.MappedMapClass", "rc(Projection.MappedMapClass;pinterface({3c2925fe-8519-45c1-aa79-197b6718c1c1};string;string))"),
        new("Projection.MappedBindableClass", "rc(Projection.MappedBindableClass;{036d2c08-df29-41af-8aa2-d774be62ba6f})"),
        new("Projection.MappedXamlClass", $"rc(Projection.MappedXamlClass;{{{(useUwp ? "cf75d69c-f2f4-486b-b302-bb4c09baebfa" : "90b17601-b065-586e-83d9-9adc3a695284")}}})"),
        new("Projection.MappedStructListClass", "rc(Projection.MappedStructListClass;pinterface({913337e9-11a1-4345-a3a2-4e7f956e222d};struct(Windows.Foundation.TimeSpan;i8)))"),
        new("Projection.IExclusive", "{d7527847-57da-4e15-914c-06806699a098}"),
        new("int", "i4", IsValueType: true),
        new("string", "string"),
        new("object", "cinterface(IInspectable)"),
        new("System.Guid", "g16", IsValueType: true),
        new("System.Uri", "rc(Windows.Foundation.Uri;{9e365e57-48b2-4160-956f-c7385120bbfc})"),
        new("System.ComponentModel.PropertyChangedEventArgs", useUwp
            ? "rc(Windows.UI.Xaml.Data.PropertyChangedEventArgs;{4f33a9a0-5cf4-47a4-b16f-d7faaf17457e})"
            : "rc(Microsoft.UI.Xaml.Data.PropertyChangedEventArgs;{63d0c952-396b-54f4-af8c-ba8724a427bf})"),
        new("System.Collections.Specialized.NotifyCollectionChangedEventArgs", useUwp
            ? "rc(Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs;{4cf68d33-e3f2-4964-b85e-945b4f7e2f21})"
            : "rc(Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs;{da049ff2-d2e0-5fe8-8c7b-f87f26060b6f})"),
        new("System.Collections.Specialized.NotifyCollectionChangedAction", useUwp
            ? "enum(Windows.UI.Xaml.Interop.NotifyCollectionChangedAction;i4)"
            : "enum(Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction;i4)", IsValueType: true),
        new("System.TimeSpan", "struct(Windows.Foundation.TimeSpan;i8)", IsValueType: true),
        new("Windows.Foundation.Point", "struct(Windows.Foundation.Point;f4;f4)", IsValueType: true),
        new("Projection.Value", "struct(Projection.Value;i4;string)", IsValueType: true),
        new("Projection.EnumValue", "enum(Projection.EnumValue;i4)", IsValueType: true),
        new("int?", "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};i4)", IsValueType: true),
        new("System.Collections.Generic.IList<Projection.RuntimeClass>", "pinterface({913337e9-11a1-4345-a3a2-4e7f956e222d};rc(Projection.RuntimeClass;{e2b50029-b4c1-4314-a4b8-fb813a2f275e}))"),
        new("System.EventHandler<Windows.Foundation.IStringable>", "pinterface({9de1c535-6ae1-11e0-84e1-18a905bcc53f};{96369f54-8eb6-48f0-abce-c1b211e627c3})"),
        new("Windows.Foundation.AsyncOperationCompletedHandler<Projection.RuntimeClass>", "pinterface({fcdcf02c-e5d8-4478-915a-4d90b74b83a5};rc(Projection.RuntimeClass;{e2b50029-b4c1-4314-a4b8-fb813a2f275e}))"),
        new("System.Collections.Generic.KeyValuePair<Projection.IProjected, Projection.Handler>", "pinterface({02b51929-c1c4-4a7e-8940-0312b5c18500};{97d098a5-3b99-4de9-88a5-e11d2f50c795};delegate({1d1a8b8b-fa66-414f-9cbd-b65fc99d17fa}))", IsValueType: true)
    ];

    private static void AssertInterceptedIids(IReadOnlyList<(string Type, Guid Iid)> cases, bool useUwp)
    {
        StringBuilder source = new("""
            public static class Test
            {
                public static void Main() { }
            """);
        for (int index = 0; index < cases.Count; index++)
        {
            source.AppendLine($"public static System.Guid Run{index}() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<{cases[index].Type}>();");
        }
        source.AppendLine("}");

        MetadataReference projection = CompileReference(CoverageProjection, "IidCoverageProjection");
        (Compilation compilation, GeneratorDriverRunResult result) = Run(source.ToString(), [projection],
            OutputKind.ConsoleApplication, useUwp, CoverageAssemblyName);
        SyntaxNode root = result.GeneratedTrees.Single().GetRoot();
        Assert.AreEqual(cases.Count, root.DescendantNodes().OfType<AttributeSyntax>()
            .Count(static attribute => attribute.Name.ToString().EndsWith("InterceptsLocationAttribute", StringComparison.Ordinal)));

        using MemoryStream stream = new();
        var emit = compilation.Emit(stream);
        Assert.IsTrue(emit.Success, string.Join("\n", emit.Diagnostics));
        stream.Position = 0;
        using (PEReader pe = new(stream, PEStreamOptions.LeaveOpen))
        {
            MetadataReader reader = pe.GetMetadataReader();
            Assert.IsFalse(reader.MemberReferences.Any(handle =>
                reader.GetString(reader.GetMemberReference(handle).Name) == "GetIid"));
        }

        Type probe = Assembly.Load(stream.ToArray()).GetType("Test")!;
        for (int index = 0; index < cases.Count; index++)
        {
            Assert.AreEqual(cases[index].Iid, probe.GetMethod($"Run{index}")!.Invoke(null, null), cases[index].Type);
        }
    }

    // Independent RFC 4122 UUIDv5 calculation using the WinRT namespace in network byte order
    private static Guid CreateExpectedParameterizedIid(string signature)
    {
        byte[] namespaceBytes = new Guid("11f47ad5-7b73-42c0-abae-878b1e16adee").ToByteArray(bigEndian: true);
        byte[] digest = SHA1.HashData([.. namespaceBytes, .. Encoding.UTF8.GetBytes(signature)]);
        digest[6] = (byte)((digest[6] & 0x0F) | 0x50);
        digest[8] = (byte)((digest[8] & 0x3F) | 0x80);
        return new Guid(digest.AsSpan(0, 16), bigEndian: true);
    }

    private sealed record CoverageType(string Type, string Signature, bool IsValueType = false);
}
