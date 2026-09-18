// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using InteropGeneratorTest.Helpers;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_ForwardedTypeIdentity
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ForwardedFrameworkTypes_HaveOneResolvedAssociationPerGroup(bool useFrameworkImplementations)
    {
        using InteropGeneratorRunner runner = new(useFrameworkImplementations);
        string output = await runner.GenerateAsync("forwarded");
        (RuntimeContext context, ModuleDefinition module) = runner.LoadOutput(output);
        List<Association> associations = ReadAssociations(module, context);

        foreach (IGrouping<(string Group, string Source), Association> group in associations.GroupBy(association => (association.Group, association.ResolvedSource)))
        {
            Assert.AreEqual(1, group.Count(),
                $"Duplicate resolved association for {group.Key}:\n{string.Join("\n", group.Select(association => $"{association.Source} => {association.Target}"))}");
        }

        Association[] dictionaries = associations.Where(association =>
            association.Group.EndsWith("WindowsRuntimeComWrappersTypeMapGroup") &&
            association.Source is GenericInstanceTypeSignature generic &&
            generic.GenericType.Name == "ReadOnlyDictionary`2" &&
            generic.TypeArguments[0].FullName == "System.String" &&
            generic.TypeArguments[1].FullName is "System.Object" or "System.String").ToArray();

        Assert.AreEqual(2, dictionaries.Length, "Both observed closed dictionary instantiations must have CCWs.");
        CollectionAssert.AreEquivalent(new[] { "System.Object", "System.String" },
            dictionaries.Select(association => ((GenericInstanceTypeSignature)association.Source).TypeArguments[1].FullName).ToArray());

        foreach (Association association in associations)
        {
            foreach (TypeSignature type in InteropGeneratorRunner.EnumerateTypes(association.Source))
            {
                ITypeDescriptor namedType = type is GenericInstanceTypeSignature generic ? generic.GenericType : type;

                if (namedType.FullName is
                    "System.Collections.ObjectModel.ReadOnlyDictionary`2" or
                    "System.Collections.ObjectModel.ReadOnlyDictionary`2+KeyCollection" or
                    "System.Collections.ObjectModel.ReadOnlyDictionary`2+ValueCollection")
                {
                    Assert.AreEqual("System.Runtime", type.Scope!.GetAssembly()!.Name!.ToString(),
                        $"The .NET 10 reference surface, not a facade or implementation scope, must be emitted for '{type}'.");
                }
            }
        }

        foreach (string nestedName in new[] { "KeyCollection", "ValueCollection" })
        {
            Assert.AreEqual(1, associations.Count(association =>
                association.Group.EndsWith("WindowsRuntimeComWrappersTypeMapGroup") &&
                association.Source is GenericInstanceTypeSignature generic &&
                generic.GenericType.FullName == "System.Collections.ObjectModel.ReadOnlyDictionary`2+" + nestedName &&
                generic.TypeArguments.All(argument => argument.FullName == "System.String")),
                $"Nested forwarded type '{nestedName}' must retain exactly one CCW.");
        }

        Association[] distinct = associations.Where(association =>
            association.Group.EndsWith("WindowsRuntimeComWrappersTypeMapGroup") &&
            association.Source is GenericInstanceTypeSignature generic &&
            generic.GenericType.Name == "SameName`1").ToArray();
        Assert.AreEqual(2, distinct.Length, "Equal names in genuinely different assemblies must not be merged.");
        Assert.AreNotEqual(distinct[0].ResolvedSource, distinct[1].ResolvedSource);
    }

    [TestMethod]
    public async Task ForwardedFrameworkTypes_AreDeterministicAcrossInputOrderAndParallelism()
    {
        using InteropGeneratorRunner runner = new();
        byte[] expected = File.ReadAllBytes(await runner.GenerateAsync("serial"));

        foreach ((bool reverse, int parallelism) in new[] { (true, 1), (false, -1), (true, 2) })
        {
            byte[] actual = File.ReadAllBytes(await runner.GenerateAsync($"order-{reverse}-dop-{parallelism}", reverse, parallelism));
            CollectionAssert.AreEqual(expected, actual,
                $"Changing reference/implementation order or parallelism changed output (reverse={reverse}, parallelism={parallelism}).");
        }
    }

    [TestMethod]
    [DataRow("Minimal", false, 1)]
    [DataRow("Strict", true, 1)]
    [DataRow("Strict", false, 0)]
    public async Task NetStandard20_UsesTargetFrameworkIdentitiesAndRespectsMarshallingMode(string mode, bool optIn, int expectedCallbacks)
    {
        using InteropGeneratorRunner runner = new();
        string output = await runner.GenerateAsync("standard", marshallingMode: mode, optInNetStandard: optIn);
        (RuntimeContext context, ModuleDefinition module) = runner.LoadOutput(output);
        List<Association> associations = ReadAssociations(module, context);

        Assert.AreEqual(expectedCallbacks, associations.Count(association =>
            association.Group.EndsWith("WindowsRuntimeComWrappersTypeMapGroup") &&
            association.Source.FullName == "StandardInput.Callback"));

        foreach (Association association in associations)
        {
            foreach (TypeSignature type in InteropGeneratorRunner.EnumerateTypes(association.Source))
            {
                Assert.AreNotEqual("netstandard", type.Scope?.GetAssembly()?.Name?.ToString(),
                    $"The portable scope must not leak into the generated type maps for '{type}'.");
            }
        }
    }

    [TestMethod]
    public async Task IncompatibleFrameworkReferenceDeclarations_ReportAnExplicitError()
    {
        using InteropGeneratorRunner runner = new(useFrameworkImplementations: true, ambiguousFrameworkReferences: true);
        await runner.AssertAmbiguousReferencesFailAsync(reverseInputs: false);
        await runner.AssertAmbiguousReferencesFailAsync(reverseInputs: true);
    }

    private static List<Association> ReadAssociations(ModuleDefinition module, RuntimeContext context)
    {
        List<Association> result = [];

        foreach (CustomAttribute attribute in module.Assembly!.CustomAttributes)
        {
            if (attribute.Constructor?.DeclaringType is not TypeSpecification { Signature: GenericInstanceTypeSignature generic } ||
                generic.GenericType.Name != "TypeMapAssociationAttribute`1")
            {
                continue;
            }

            Assert.AreEqual(1, generic.TypeArguments.Count);
            TypeSignature group = generic.TypeArguments[0];
            Assert.IsNotNull(attribute.Signature);
            Assert.AreEqual(2, attribute.Signature.FixedArguments.Count);
            TypeSignature source = (TypeSignature)attribute.Signature.FixedArguments[0].Element!;
            TypeSignature target = (TypeSignature)attribute.Signature.FixedArguments[1].Element!;
            result.Add(new(group.FullName, source, target, InteropGeneratorRunner.ResolvedTypeKey(source, context)));
        }

        Assert.IsGreaterThan(0, result.Count, "An empty map cannot satisfy the uniqueness regression.");
        return result;
    }

    private sealed record Association(string Group, TypeSignature Source, TypeSignature Target, string ResolvedSource);
}
