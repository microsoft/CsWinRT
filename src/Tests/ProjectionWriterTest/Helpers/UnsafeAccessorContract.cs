// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace ProjectionWriterTest.Helpers;

internal static class UnsafeAccessorContract
{
    public readonly record struct Target(string Type, string Method);

    public static Target[] Verify(ModuleDefinition projection, ModuleDefinition interop)
    {
        Dictionary<string, TypeDefinition> definitions = interop.GetAllTypes()
            .ToDictionary(type => type.FullName, StringComparer.Ordinal);
        List<Target> targets = [];

        foreach (MethodDefinition method in projection.GetAllTypes().SelectMany(type => type.Methods))
        {
            foreach (ParameterDefinition parameter in method.ParameterDefinitions)
            {
                foreach (CustomAttribute attribute in parameter.CustomAttributes.Where(
                    attribute => attribute.Constructor?.DeclaringType?.FullName == typeof(UnsafeAccessorTypeAttribute).FullName))
                {
                    string qualifiedName = attribute.Signature!.FixedArguments[0].Element!.ToString()!;
                    const string assemblySuffix = ", WinRT.Interop";
                    Assert.IsTrue(qualifiedName.EndsWith(assemblySuffix, StringComparison.Ordinal), qualifiedName);
                    string typeName = qualifiedName[..^assemblySuffix.Length];
                    Assert.IsTrue(definitions.TryGetValue(typeName, out TypeDefinition? target),
                        $"'{method.FullName}' references missing TypeDef '{typeName}' (ordinal lookup).");

                    CustomAttribute accessor = method.CustomAttributes.Single(
                        attribute => attribute.Constructor?.DeclaringType?.FullName == typeof(UnsafeAccessorAttribute).FullName);
                    UnsafeAccessorKind kind = (UnsafeAccessorKind)Convert.ToInt32(accessor.Signature!.FixedArguments[0].Element);
                    bool constructor = kind == UnsafeAccessorKind.Constructor;
                    Assert.IsTrue(constructor || kind == UnsafeAccessorKind.StaticMethod, method.FullName);
                    Assert.AreEqual(constructor ? 0 : 1, parameter.Sequence, method.FullName);
                    string name = constructor ? ".ctor" : accessor.Signature.NamedArguments
                        .Single(argument => argument.MemberName == "Name").Argument.Element!.ToString()!;
                    TypeSignature[] parameters = method.Signature!.ParameterTypes.Skip(constructor ? 0 : 1).ToArray();
                    MethodDefinition[] matches = target!.Methods.Where(candidate =>
                        candidate.Name == name
                        && candidate.IsStatic == !constructor
                        && candidate.Signature!.GenericParameterCount == method.Signature.GenericParameterCount
                        && candidate.Signature.ParameterTypes.SequenceEqual(parameters, SignatureComparer.Default)
                        && SignatureComparer.Default.Equals(candidate.Signature.ReturnType,
                            constructor ? interop.CorLibTypeFactory.Void : method.Signature.ReturnType)).ToArray();

                    Assert.HasCount(1, matches,
                        $"'{method.FullName}' does not resolve uniquely to '{typeName}.{name}' with the same signature. " +
                        $"Candidates: {string.Join("; ", target.Methods.Where(candidate => candidate.Name == name).Select(candidate => candidate.FullName))}");
                    targets.Add(new Target(typeName, name));
                }
            }
        }

        Assert.IsGreaterThanOrEqualTo(30, targets.Count, "The fixture must exercise marshallers, methods, arrays, constructors, and IIDs.");
        Assert.IsGreaterThanOrEqualTo(2, targets.Count(target => target.Method == ".ctor"));
        Assert.IsGreaterThanOrEqualTo(2, targets.Count(target => target.Method.StartsWith("get_IID_", StringComparison.Ordinal)));
        return targets.ToArray();
    }
}
