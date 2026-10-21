// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using InteropGeneratorTest.Helpers;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_NonGenericEnumerable
{
    [TestMethod]
    public async Task CollectionImplementations_HaveTypedEnumerableCallbacks()
    {
        using InteropGeneratorRunner runner = new(useFrameworkImplementations: true, enumerableProviders: true);
        string output = await runner.GenerateAsync("enumerable-callbacks");
        (RuntimeContext context, ModuleDefinition module) = runner.LoadOutput(output);
        HashSet<string> implementations = [];
        Dictionary<string, TypeDefinition> providers = [];

        foreach (TypeDefinition type in module.TopLevelTypes.Where(type => type.IsInterface))
        {
            GenericInstanceTypeSignature? enumerable = type.Interfaces
                .Select(implementation => (implementation.Interface as TypeSpecification)?.Signature)
                .OfType<GenericInstanceTypeSignature>()
                .FirstOrDefault(signature => signature.GenericType.FullName == "System.Collections.Generic.IEnumerable`1");

            if (enumerable is null)
            {
                continue;
            }

            CustomAttribute attribute = type.CustomAttributes.Single(IsEnumerableProvider);
            Assert.IsInstanceOfType<TypeDefinition>(attribute.Constructor!.DeclaringType);
            TypeDefinition provider = (TypeDefinition)attribute.Constructor.DeclaringType;
            Assert.IsTrue(provider.IsSealed);
            Assert.AreEqual(0, provider.GenericParameters.Count, "Specialized attributes must not be generic types.");
            Assert.AreEqual(0, attribute.Signature!.FixedArguments.Count, "No Type-valued activation metadata is needed.");
            Assert.AreEqual(2, provider.Methods.Count, "Emit only the constructor and one typed override.");
            MethodDefinition get = provider.Methods.Single(method => method.Name == "TryGetEnumeratorCore");
            Assert.IsTrue(get.IsFamily);
            Assert.IsTrue(get.CilMethodBody!.Instructions.Any(instruction =>
                instruction.OpCode == CilOpCodes.Isinst &&
                instruction.Operand is TypeSpecification { Signature: GenericInstanceTypeSignature signature } &&
                signature.FullName == enumerable.FullName));
            Assert.IsFalse(get.CilMethodBody.Instructions.Any(instruction => instruction.OpCode == CilOpCodes.Castclass));
            Assert.IsTrue(get.CilMethodBody!.Instructions.Any(instruction =>
                instruction.OpCode == CilOpCodes.Callvirt &&
                instruction.Operand is IMethodDescriptor method &&
                method.Name == "GetEnumerator" && method.DeclaringType!.FullName == enumerable.FullName));
            string element = InteropGeneratorRunner.ResolvedTypeKey(enumerable.TypeArguments[0], context);
            if (providers.TryGetValue(element, out TypeDefinition? existing))
            {
                Assert.AreSame(existing, provider, "Collection families must share one attribute for the same element type.");
            }
            else
            {
                providers.Add(element, provider);
            }
            foreach (GenericInstanceTypeSignature implemented in type.Interfaces
                .Select(implementation => (implementation.Interface as TypeSpecification)?.Signature)
                .OfType<GenericInstanceTypeSignature>())
            {
                implementations.Add(implemented.GenericType.FullName);
            }
        }

        Assert.AreEqual(providers.Count, module.TopLevelTypes.Count(type =>
            type.BaseType?.FullName == "WindowsRuntime.InteropServices.DynamicInterfaceCastableEnumerableAttribute"));
        foreach (string name in new[]
        {
            "System.Collections.Generic.IEnumerable`1", "System.Collections.Generic.IList`1", "System.Collections.Generic.IReadOnlyList`1",
            "System.Collections.Generic.ICollection`1", "System.Collections.Generic.IReadOnlyCollection`1",
            "System.Collections.Generic.IDictionary`2", "System.Collections.Generic.IReadOnlyDictionary`2",
            "Windows.Foundation.Collections.IObservableVector`1", "Windows.Foundation.Collections.IObservableMap`2"
        })
        {
            Assert.IsTrue(implementations.Contains(name), $"Missing provider coverage for '{name}'.");
        }

        HashSet<string> mapped = [];
        foreach (CustomAttribute attribute in module.Assembly!.CustomAttributes)
        {
            if (attribute.Constructor?.DeclaringType is not TypeSpecification { Signature: GenericInstanceTypeSignature group } ||
                group.GenericType.Name != "TypeMapAssociationAttribute`1" ||
                group.TypeArguments[0].Name != "DynamicInterfaceCastableImplementationTypeMapGroup" ||
                attribute.Signature?.FixedArguments[0].Element is not GenericInstanceTypeSignature source)
            {
                continue;
            }

            string sourceName = source.GenericType.Name!.ToString();
            if (sourceName is not ("IList`1" or "IReadOnlyList`1" or "ICollection`1" or "IReadOnlyCollection`1" or "IDictionary`2" or "IReadOnlyDictionary`2"))
            {
                continue;
            }

            TypeSignature target = (TypeSignature)attribute.Signature.FixedArguments[1].Element!;
            Assert.IsTrue(target.TryResolve(context, out TypeDefinition? implementation));
            Assert.IsTrue(implementation!.CustomAttributes.Any(IsEnumerableProvider));
            mapped.Add(sourceName);
        }

        CollectionAssert.AreEquivalent(
            new[] { "IList`1", "IReadOnlyList`1", "ICollection`1", "IReadOnlyCollection`1", "IDictionary`2", "IReadOnlyDictionary`2" },
            mapped.ToArray());
    }

    [TestMethod]
    public async Task InternalElement_ProviderMethodsCompile()
    {
        using InteropGeneratorRunner runner = new(useFrameworkImplementations: true, enumerableProviders: true);
        string output = await runner.GenerateAsync("internal-enumerable-provider");
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        TypeDefinition implementation = module.TopLevelTypes.Single(type => type.IsInterface &&
            type.Interfaces.Any(implemented =>
                implemented.Interface is TypeSpecification
                {
                    Signature: GenericInstanceTypeSignature { TypeArguments: [TypeSignature { FullName: "InternalElement" }] } generic
                } && generic.GenericType.Name == "IList`1"));
        AssemblyLoadContext context = new("InternalEnumerableProvider", isCollectible: true);
        context.Resolving += (_, name) =>
        {
            if (name.Name == "WinRT.Runtime")
            {
                return typeof(Windows.Foundation.IStringable).Assembly;
            }
            string dependency = Path.Combine(runner.Root, name.Name + ".dll");
            return File.Exists(dependency) ? Load(dependency) : null;
        };
        try
        {
            Assembly generated = Load(output);
            Type loaded = generated.GetType(implementation.FullName, throwOnError: true)!;
            object marker = loaded.GetCustomAttributes(inherit: false).Single(attribute =>
                attribute.GetType().BaseType?.FullName == "WindowsRuntime.InteropServices.DynamicInterfaceCastableEnumerableAttribute");
            Assert.IsFalse(marker.GetType().IsGenericType);
            Assert.IsTrue(marker.GetType().IsSealed);
            MethodInfo? method = marker.GetType().GetMethod("TryGetEnumeratorCore",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.IsNotNull(method);
            RuntimeHelpers.PrepareMethod(method.MethodHandle);
        }
        finally
        {
            context.Unload();
        }

        Assembly Load(string path)
        {
            using MemoryStream image = new(File.ReadAllBytes(path));
            return context.LoadFromStream(image);
        }
    }

    private static bool IsEnumerableProvider(CustomAttribute attribute)
    {
        return attribute.Constructor?.DeclaringType is TypeDefinition
        {
            BaseType.FullName: "WindowsRuntime.InteropServices.DynamicInterfaceCastableEnumerableAttribute"
        };
    }
}
