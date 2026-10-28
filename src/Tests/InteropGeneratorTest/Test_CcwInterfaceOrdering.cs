// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using InteropGeneratorTest.Helpers;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.Models;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_CcwInterfaceOrdering
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task EquivalentInterfaceSets_ShareOneMarshallerRegardlessOfDeclarationOrder(bool useFrameworkImplementations)
    {
        using InteropGeneratorRunner runner = new(useFrameworkImplementations: useFrameworkImplementations, ccwGroupingCases: true);
        (RuntimeContext context, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("shared"));
        Dictionary<string, string> marshallers = GetUserDefinedMarshallers(module, context);

        string[] shared = Enumerable.Range(0, 32)
            .Select(index => marshallers["GroupingInput.Shared" + index])
            .ToArray();

        Assert.AreEqual(32, shared.Length);
        Assert.AreEqual(1, shared.Distinct(StringComparer.Ordinal).Count(),
            "Types with the same semantic interface set must share the first ordered representative's marshaller.");
        StringAssert.Contains(shared[0], "Shared0",
            "The first type in global canonical order must remain the shared group's representative.");
        Assert.AreNotEqual(shared[0], marshallers["GroupingInput.DisposableOnly"]);
        Assert.AreNotEqual(shared[0], marshallers["GroupingInput.ExplicitStringable"]);
        Assert.AreNotEqual(marshallers["GroupingInput.DisposableOnly"], marshallers["GroupingInput.ExplicitStringable"]);

        string[] expectedOrder = Enumerable.Range(0, 32)
            .Select(index => "GroupingInput.Shared" + index)
            .Append("GroupingInput.DisposableOnly")
            .Append("GroupingInput.ExplicitStringable")
            .Order(StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEqual(expectedOrder, marshallers.Keys.ToArray(),
            "Proxy associations must retain global type order rather than being emitted in vtable-group order.");
    }

    [TestMethod]
    public async Task CcwGroups_AreByteIdenticalAcrossInputOrderAndParallelism()
    {
        using InteropGeneratorRunner runner = new(ccwGroupingCases: true);
        byte[] expected = File.ReadAllBytes(await runner.GenerateAsync("serial", parallelism: 1));

        foreach ((bool reverse, int parallelism) in new[] { (true, 1), (false, -1), (true, 2) })
        {
            byte[] actual = File.ReadAllBytes(await runner.GenerateAsync($"replay-{reverse}-{parallelism}", reverse, parallelism));
            CollectionAssert.AreEqual(expected, actual,
                $"Interface grouping changed the output with reverse={reverse}, DOP={parallelism}.");
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task PreparedSetOrder_MatchesLegacyComparisonForPrefixesScopesAndShapes(bool cacheDisplayNames)
    {
        using InteropGeneratorRunner runner = new(ccwGroupingCases: true);
        (RuntimeContext context, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("comparison"));
        SignatureComparer comparer = new(context, SignatureComparisonFlags.VersionAgnostic);
        AssemblyReference firstAssembly = new("IdentityA", new Version(1, 0, 0, 0));
        AssemblyReference secondAssembly = new("IdentityB", new Version(1, 0, 0, 0));
        TypeSignature first = new TypeReference(module, firstAssembly, "IdentityControl", "SameName").ToTypeSignature(false);
        TypeSignature second = new TypeReference(module, secondAssembly, "IdentityControl", "SameName").ToTypeSignature(false);
        TypeSignature generic = new TypeReference(module, firstAssembly, "IdentityControl", "Container`1")
            .MakeGenericInstanceType(false, [first]);
        TypeSignature otherGeneric = new TypeReference(module, firstAssembly, "IdentityControl", "Container`1")
            .MakeGenericInstanceType(false, [second]);
        TypeSignature array = first.MakeSzArrayType();
        TypeSignature nested = new TypeReference(module, (TypeReference)first.GetUnderlyingTypeDefOrRef()!, null, "Nested")
            .ToTypeSignature(false);
        TypeSignature otherNested = new TypeReference(module, (TypeReference)second.GetUnderlyingTypeDefOrRef()!, null, "Nested")
            .ToTypeSignature(false);
        TypeSignature combinedArgument = new TypeReference(module, null, "Ambiguous", "A;Ambiguous.B").ToTypeSignature(false);
        TypeSignature separateFirst = new TypeReference(module, null, "Ambiguous", "A").ToTypeSignature(false);
        TypeSignature separateSecond = new TypeReference(module, null, "Ambiguous", "B").ToTypeSignature(false);
        TypeReference genericDefinition = new(module, null, "Ambiguous", "I`1");
        TypeSignature oneArgument = genericDefinition.MakeGenericInstanceType(false, [combinedArgument]);
        TypeSignature twoArguments = genericDefinition.MakeGenericInstanceType(false, [separateFirst, separateSecond]);
        TypeDescriptorComparer descriptorComparer = cacheDisplayNames
            ? TypeDescriptorComparer.CreateForEmission(context)
            : new(context);
        Assert.AreEqual(descriptorComparer.GetOrderKey(oneArgument), descriptorComparer.GetOrderKey(twoArguments),
            "The delimiter collision must exercise the structural comparison fallback.");
        Assert.AreNotEqual(0, descriptorComparer.Compare(oneArgument, twoArguments));

        TypeSignatureEquatableSet[] sets =
        [
            new(comparer),
            new(comparer, first),
            new(comparer, first, second),
            new(comparer, second, first),
            new(comparer, second),
            new(comparer, generic),
            new(comparer, otherGeneric),
            new(comparer, array),
            new(comparer, nested),
            new(comparer, otherNested),
            new(comparer, oneArgument),
            new(comparer, twoArguments),
            new(comparer, module.CorLibTypeFactory.Int32),
            new(comparer, module.CorLibTypeFactory.Int32.MakeSzArrayType()),
            new(comparer, module.CorLibTypeFactory.Int32.MakeByReferenceType()),
            new(comparer, module.CorLibTypeFactory.Int32.MakePointerType()),
            new(comparer, module.CorLibTypeFactory.String),
            new(comparer, module.CorLibTypeFactory.String.MakeSzArrayType())
        ];

        TypeDescriptorComparer uncached = new(context);

        foreach (TypeSignature type in sets.SelectMany(static set => set))
        {
            Assert.AreEqual(uncached.GetOrderKey(type), descriptorComparer.GetOrderKey(type),
                $"Caching display names changed the ordering key for '{type}'.");
        }

        TypeSignature[] input = [.. sets.SelectMany(static set => set).Reverse()];
        CollectionAssert.AreEqual(
            input.Order(uncached).ToArray(),
            TypeDescriptorComparer.Order(input, context).ToArray(),
            "Shared ordering must preserve legacy ordering for scopes, shapes, and structural key collisions.");

        OrderedVtableTypes[] prepared = sets.Select(set => new OrderedVtableTypes(set, descriptorComparer)).ToArray();

        for (int i = 0; i < sets.Length; i++)
        {
            for (int j = 0; j < sets.Length; j++)
            {
                Assert.AreEqual(Math.Sign(sets[i].CompareTo(sets[j])), Math.Sign(prepared[i].CompareTo(prepared[j])),
                    $"Prepared comparison disagrees with the legacy comparer for sets {i} and {j}.");
            }
        }

        TypeSignatureEquatableSet[] legacy = sets.Order().ToArray();
        TypeSignatureEquatableSet[] optimized = prepared.Order().Select(view => view.Set).ToArray();

        for (int i = 0; i < legacy.Length; i++)
        {
            Assert.IsTrue(ReferenceEquals(legacy[i], optimized[i]),
                $"A tie or prefix was not ordered stably at index {i}.");
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void TypeOrdering_IsDeferredAndRefreshesNamesForEachEnumeration(bool useKeySelector)
    {
        TypeDefinition first = new("Ordering", "A", 0);
        TypeDefinition second = new("Ordering", "M", 0);
        bool rename = true;
        int selections = 0;

        IEnumerable<TypeDefinition> Source()
        {
            yield return first;
            if (rename)
            {
                first.Name = "Z";
            }
            yield return second;
        }

        IEnumerable<TypeDefinition> ordered = useKeySelector
            ? TypeDescriptorComparer.OrderBy(
                Source(),
                type =>
                {
                    selections++;
                    return type;
                },
                runtimeContext: null)
            : TypeDescriptorComparer.Order(Source(), runtimeContext: null);

        Assert.AreEqual(0, selections);
        CollectionAssert.AreEqual(new[] { second, first }, ordered.ToArray(),
            "All source processing must finish before ordering keys are computed.");
        Assert.AreEqual(useKeySelector ? 2 : 0, selections);

        rename = false;
        first.Name = "A";

        CollectionAssert.AreEqual(new[] { first, second }, ordered.ToArray(),
            "A later enumeration must not reuse stale cached names.");
        Assert.AreEqual(useKeySelector ? 4 : 0, selections);
    }

    [TestMethod]
    public void TypeOrdering_PreservesNullKeysAndStableTies()
    {
        TypeDefinition first = new("Ordering", "A", 0);
        TypeDefinition second = new("Ordering", "B", 0);
        (int Index, ITypeDescriptor? Type)[] input = [(0, second), (1, null), (2, first), (3, null), (4, first)];

        CollectionAssert.AreEqual(
            new[] { 1, 3, 2, 4, 0 },
            TypeDescriptorComparer.OrderBy(input, static item => item.Type, runtimeContext: null)
                .Select(static item => item.Index).ToArray());
        Assert.IsEmpty(TypeDescriptorComparer.Order(Array.Empty<ITypeDescriptor>(), runtimeContext: null));
        CollectionAssert.AreEqual(
            new[] { first },
            TypeDescriptorComparer.Order(new[] { first }, runtimeContext: null).ToArray());
        CollectionAssert.AreEqual(
            new[] { null!, first, second },
            TypeDescriptorComparer.Order(new[] { second, first, null! }, runtimeContext: null).ToArray());
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            TypeDescriptorComparer.Order<ITypeDescriptor>(null!, runtimeContext: null));
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            TypeDescriptorComparer.OrderBy(new[] { first }, null!, runtimeContext: null));
    }

    private static Dictionary<string, string> GetUserDefinedMarshallers(ModuleDefinition module, RuntimeContext context)
    {
        Dictionary<string, string> marshallers = [];

        foreach (CustomAttribute attribute in module.Assembly!.CustomAttributes)
        {
            if (attribute.Constructor?.DeclaringType is not TypeSpecification { Signature: GenericInstanceTypeSignature generic } ||
                generic.GenericType.Name != "TypeMapAssociationAttribute`1" ||
                !generic.TypeArguments[0].FullName.EndsWith("WindowsRuntimeComWrappersTypeMapGroup", StringComparison.Ordinal))
            {
                continue;
            }

            TypeSignature source = (TypeSignature)attribute.Signature!.FixedArguments[0].Element!;

            if (!source.FullName.StartsWith("GroupingInput.", StringComparison.Ordinal))
            {
                continue;
            }

            TypeSignature target = (TypeSignature)attribute.Signature.FixedArguments[1].Element!;
            TypeDefinition proxy = target.Resolve(context);
            string marshaller = proxy.CustomAttributes.Single(item =>
                item.Constructor?.DeclaringType?.Name?.ToString().EndsWith("ComWrappersMarshallerAttribute", StringComparison.Ordinal) is true)
                .Constructor!.DeclaringType!.FullName;
            marshallers.Add(source.FullName, marshaller);
        }

        return marshallers;
    }
}
