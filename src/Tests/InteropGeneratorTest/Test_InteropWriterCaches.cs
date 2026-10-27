// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Code.Native;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Writers;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_InteropWriterCaches
{
    [TestMethod]
    public void TokenMemoization_CachesIdentityButNotEquivalentObjects()
    {
        ModuleDefinition module = new("Tokens.dll");
        DiagnosticBag diagnostics = new();
        CountingTokenProvider inner = new();
        MemoizingMetadataTokenProvider memo = new(inner, diagnostics);
        TypeReference first = new(module, module.CorLibTypeFactory.CorLibScope, "Fixture", "Target");
        TypeReference equivalent = new(module, module.CorLibTypeFactory.CorLibScope, "Fixture", "Target");

        MetadataToken expected = memo.GetTypeReferenceToken(first, "first");
        Assert.AreEqual(expected, memo.GetTypeReferenceToken(first, "repeat"));
        Assert.AreEqual(1, inner.Calls);
        _ = memo.GetTypeReferenceToken(equivalent, "distinct");
        Assert.AreEqual(2, inner.Calls);
        Assert.AreEqual("distinct", inner.LastSource);

        MemberReference member = new(first, "Invoke", MethodSignature.CreateStatic(
            returnType: module.CorLibTypeFactory.Void, genericParameterCount: 1, parameterTypes: []));
        _ = memo.GetMemberReferenceToken(member);
        _ = memo.GetMemberReferenceToken(member);
        TypeSpecification type = new(first.MakeGenericInstanceType(false, [module.CorLibTypeFactory.Int32]));
        _ = memo.GetTypeSpecificationToken(type);
        _ = memo.GetTypeSpecificationToken(type);
        MethodSpecification method = member.MakeGenericInstanceMethod([module.CorLibTypeFactory.Int32]);
        _ = memo.GetMethodSpecificationToken(method);
        _ = memo.GetMethodSpecificationToken(method);
        Assert.AreEqual(5, inner.Calls);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void FailedTokenRequests_AreReportedAgainWithTheirOwnSource(bool fatalOnly)
    {
        ModuleDefinition module = new("InvalidTokens.dll");
        DiagnosticBag diagnostics = new();
        CountingTokenProvider inner = new()
        {
            OnRequest = () =>
            {
                if (fatalOnly)
                {
                    diagnostics.MarkAsFatal();
                }
                else
                {
                    diagnostics.RegisterException(new InvalidOperationException("invalid reference"));
                }
            }
        };
        MemoizingMetadataTokenProvider memo = new(inner, diagnostics);
        TypeReference reference = new(module, module.CorLibTypeFactory.CorLibScope, "Fixture", "Target");
        _ = memo.GetTypeReferenceToken(reference, "first");
        _ = memo.GetTypeReferenceToken(reference, "second");
        _ = memo.GetTypeReferenceToken(reference, "third");
        Assert.AreEqual(3, inner.Calls);
        Assert.AreEqual("third", inner.LastSource);
        if (!fatalOnly)
        {
            Assert.AreEqual(3, diagnostics.Exceptions.Count);
        }
    }

    [TestMethod]
    public void NullAndPassThroughRequests_AreNeverMemoized()
    {
        CountingTokenProvider inner = new();
        MemoizingMetadataTokenProvider memo = new(inner, new DiagnosticBag());
        _ = memo.GetTypeReferenceToken(null!, "null-first");
        _ = memo.GetTypeReferenceToken(null!, "null-second");
        _ = memo.GetMemberReferenceToken(null!, "null-member-first");
        _ = memo.GetMemberReferenceToken(null!, "null-member-second");
        _ = memo.GetTypeSpecificationToken(null!, "null-type-first");
        _ = memo.GetTypeSpecificationToken(null!, "null-type-second");
        _ = memo.GetMethodSpecificationToken(null!, "null-method-first");
        _ = memo.GetMethodSpecificationToken(null!, "null-method-second");
        Assert.AreEqual(8, inner.Calls);
        Assert.AreEqual("null-method-second", inner.LastSource);
        _ = memo.GetMethodDefinitionToken(null!, "definition-first");
        _ = memo.GetMethodDefinitionToken(null!, "definition-second");
        Assert.AreEqual(10, inner.Calls);
        Assert.AreEqual("definition-second", inner.LastSource);
    }

    [TestMethod]
    public void Serializer_UsesCurrentContextAndResetsOnTokenProviderChange()
    {
        DiagnosticBag diagnostics = new();
        CountingTokenProvider first = new();
        CountingTokenProvider second = new();
        CapturingSerializer inner = new();
        MemoizingMethodBodySerializer serializer = new(inner, diagnostics);
        NativeSymbolsProvider symbols = new();
        MethodBodySerializationContext firstContext = new(first, symbols, diagnostics);
        MethodDefinition method = new("Method", MethodAttributes.Static, MethodSignature.CreateStatic(new ModuleDefinition("Fixture.dll").CorLibTypeFactory.Void));

        _ = serializer.SerializeMethodBody(firstContext, method);
        MethodBodySerializationContext wrapped = inner.Context!;
        Assert.AreNotSame(firstContext, wrapped);
        Assert.AreSame(symbols, wrapped.SymbolsProvider);
        Assert.AreSame(diagnostics, wrapped.ErrorListener);
        _ = serializer.SerializeMethodBody(firstContext, method);
        Assert.AreSame(wrapped, inner.Context);

        NativeSymbolsProvider newSymbols = new();
        _ = serializer.SerializeMethodBody(new(first, newSymbols, diagnostics), method);
        Assert.AreNotSame(wrapped, inner.Context);
        Assert.AreSame(wrapped.TokenProvider, inner.Context!.TokenProvider);
        Assert.AreSame(newSymbols, inner.Context.SymbolsProvider);
        _ = serializer.SerializeMethodBody(new(second, symbols, diagnostics), method);
        Assert.AreNotSame(wrapped.TokenProvider, inner.Context!.TokenProvider);

        MethodBodySerializationContext foreign = new(first, symbols, EmptyErrorListener.Instance);
        _ = serializer.SerializeMethodBody(foreign, method);
        Assert.AreSame(foreign, inner.Context);
    }

    [TestMethod]
    public void PayloadReuse_PreservesExtraDataAndRepeatedWritesObserveMutation()
    {
        (ModuleDefinition module, CustomAttribute attribute) = CreateModule();
        CustomAttributeSignature original = attribute.Signature!;
        original.ExtraData = [0x5A, 0xC3, 0x00];
        byte[] expected = WriteOriginal(module);
        CustomAttributePayloadCache payloads = new(module);
        ReadOnlyMemory<byte> firstPayload = payloads.GetOrAdd(attribute);
        Assert.IsTrue(firstPayload.Span.EndsWith(original.ExtraData));

        CollectionAssert.AreEqual(expected, WriteCached(module, payloads));
        Assert.AreSame(original, attribute.Signature);
        CollectionAssert.AreEqual(expected, WriteCached(module, payloads),
            "A consumed payload cache must not retain prior write state.");

        original.FixedArguments[0] = new(module.CorLibTypeFactory.String, "changed");
        byte[] changed = WriteOriginal(module);
        Assert.IsFalse(expected.AsSpan().SequenceEqual(changed));
        _ = payloads.GetOrAdd(attribute);
        CollectionAssert.AreEqual(changed, WriteCached(module, payloads));
        Assert.AreSame(original, attribute.Signature);
    }

    [TestMethod]
    public void PayloadCache_RecomputesReplacedSignaturesAndExtraData()
    {
        (ModuleDefinition module, CustomAttribute attribute) = CreateModule();
        CustomAttributePayloadCache payloads = new(module);
        byte[] expected = payloads.GetOrAdd(attribute).ToArray();
        attribute.Signature = new(new CustomAttributeArgument(module.CorLibTypeFactory.String, "replacement"));
        byte[] replacement = payloads.GetOrAdd(attribute).ToArray();
        Assert.IsFalse(expected.AsSpan().SequenceEqual(replacement));
        attribute.Signature.ExtraData = [0xAA, 0xBB];
        ReadOnlyMemory<byte> withExtra = payloads.GetOrAdd(attribute);
        Assert.IsTrue(withExtra.Span.EndsWith(new byte[] { 0xAA, 0xBB }));
        Assert.AreEqual(replacement.Length + 2, withExtra.Length);
    }

    [TestMethod]
    public async Task ConcurrentPayloadRequests_AgreeAndKeepSignaturesAttached()
    {
        (ModuleDefinition module, CustomAttribute attribute) = CreateModule();
        CustomAttributeSignature original = attribute.Signature!;
        CustomAttributePayloadCache payloads = new(module);
        byte[][] results = await Task.WhenAll(Enumerable.Range(0, 32)
            .Select(_ => Task.Run(() => payloads.GetOrAdd(attribute).ToArray())));
        foreach (byte[] result in results)
        {
            CollectionAssert.AreEqual(results[0], result);
        }
        Assert.AreSame(original, attribute.Signature);
        CollectionAssert.AreEqual(WriteOriginal(module), WriteCached(module, payloads));
    }

    [TestMethod]
    public void FailedWrite_RestoresOriginalAttributeSignatures()
    {
        (ModuleDefinition module, CustomAttribute attribute) = CreateModule();
        attribute.Signature = new();
        CustomAttributeSignature invalid = attribute.Signature;
        CustomAttributePayloadCache payloads = new(module);
        _ = payloads.GetOrAdd(attribute);
        Assert.ThrowsExactly<AggregateException>(() => WriteCached(module, payloads));
        Assert.AreSame(invalid, attribute.Signature);

        attribute.Signature = new(new CustomAttributeArgument(module.CorLibTypeFactory.String, "repaired"));
        _ = payloads.GetOrAdd(attribute);
        CollectionAssert.AreEqual(WriteOriginal(module), WriteCached(module, payloads));
    }

    [TestMethod]
    public void PayloadCache_RejectsADifferentOutputModule()
    {
        (ModuleDefinition module, CustomAttribute attribute) = CreateModule();
        CustomAttributeSignature original = attribute.Signature!;
        CustomAttributePayloadCache payloads = new(module);
        _ = payloads.GetOrAdd(attribute);
        Assert.ThrowsExactly<ArgumentException>(() => payloads.SubstituteSignatures(new ModuleDefinition("Other.dll")));
        Assert.AreSame(original, attribute.Signature);
    }

    private static (ModuleDefinition Module, CustomAttribute Attribute) CreateModule()
    {
        ModuleDefinition module = new("WriterFixture.dll");
        _ = new AssemblyDefinition("WriterFixture", new Version(1, 0, 0, 0)) { Modules = { module } };
        TypeDefinition attributeType = new("Fixture", "MarkerAttribute", TypeAttributes.Public, module.CorLibTypeFactory.Object.Type);
        module.TopLevelTypes.Add(attributeType);
        MethodDefinition constructor = new(".ctor",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
            MethodSignature.CreateInstance(returnType: module.CorLibTypeFactory.Void,
                parameterTypes: [module.CorLibTypeFactory.String]));
        constructor.CilMethodBody = new() { Instructions = { { CilOpCodes.Ret } } };
        attributeType.Methods.Add(constructor);
        CustomAttribute attribute = new(constructor, new CustomAttributeSignature(
            new CustomAttributeArgument(module.CorLibTypeFactory.String, "original")));
        module.Assembly!.CustomAttributes.Add(attribute);
        return (module, attribute);
    }

    private static byte[] WriteOriginal(ModuleDefinition module)
    {
        using MemoryStream stream = new();
        module.Write(stream);
        return stream.ToArray();
    }

    private static byte[] WriteCached(ModuleDefinition module, CustomAttributePayloadCache payloads)
    {
        using MemoryStream stream = new();
        InteropModuleWriter.Write(module, stream, payloads);
        return stream.ToArray();
    }

    private sealed class CapturingSerializer : IMethodBodySerializer
    {
        public MethodBodySerializationContext? Context { get; private set; }
        public ISegmentReference SerializeMethodBody(MethodBodySerializationContext context, MethodDefinition method)
        {
            Context = context;
            return SegmentReference.Null;
        }
    }

    private sealed class CountingTokenProvider : IMetadataTokenProvider
    {
        public int Calls { get; private set; }
        public object? LastSource { get; private set; }
        public Action? OnRequest { get; init; }
        private MetadataToken Next(TableIndex table, object? source)
        {
            Calls++;
            LastSource = source;
            OnRequest?.Invoke();
            return new(table, (uint)Calls);
        }
        public MetadataToken GetTypeReferenceToken(TypeReference type, object? diagnosticSource = null) => Next(TableIndex.TypeRef, diagnosticSource);
        public MetadataToken GetTypeDefinitionToken(TypeDefinition type, object? diagnosticSource = null) => Next(TableIndex.TypeDef, diagnosticSource);
        public MetadataToken GetOrImportTypeDefinitionToken(TypeDefinition type, object? diagnosticSource = null) => Next(TableIndex.TypeRef, diagnosticSource);
        public MetadataToken GetFieldDefinitionToken(FieldDefinition field, object? diagnosticSource = null) => Next(TableIndex.Field, diagnosticSource);
        public MetadataToken GetOrImportFieldDefinitionToken(FieldDefinition field, object? diagnosticSource = null) => Next(TableIndex.MemberRef, diagnosticSource);
        public MetadataToken GetMethodDefinitionToken(MethodDefinition method, object? diagnosticSource = null) => Next(TableIndex.Method, diagnosticSource);
        public MetadataToken GetOrImportMethodDefinitionToken(MethodDefinition method, object? diagnosticSource = null) => Next(TableIndex.MemberRef, diagnosticSource);
        public MetadataToken GetMemberReferenceToken(MemberReference member, object? diagnosticSource = null) => Next(TableIndex.MemberRef, diagnosticSource);
        public MetadataToken GetStandAloneSignatureToken(StandAloneSignature signature, object? diagnosticSource = null) => Next(TableIndex.StandAloneSig, diagnosticSource);
        public MetadataToken GetAssemblyReferenceToken(AssemblyReference assembly, object? diagnosticSource = null) => Next(TableIndex.AssemblyRef, diagnosticSource);
        public MetadataToken GetTypeSpecificationToken(TypeSpecification type, object? diagnosticSource = null) => Next(TableIndex.TypeSpec, diagnosticSource);
        public MetadataToken GetMethodSpecificationToken(MethodSpecification method, object? diagnosticSource = null) => Next(TableIndex.MethodSpec, diagnosticSource);
        public uint GetUserStringIndex(string value) => (uint)++Calls;
    }
}
