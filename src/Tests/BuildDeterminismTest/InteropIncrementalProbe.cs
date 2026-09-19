// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using static System.Reflection.Metadata.PEReaderExtensions;

/// <summary>
/// Creates ordinary managed types with stable, resolvable WinRT usage, without rewriting application assemblies.
/// </summary>
internal static class InteropIncrementalProbe
{
    internal const string AssemblyName = "CsWinRT.IncrementalReplayProbe";
    internal static readonly string[] ScopedAssemblyNames = [AssemblyName + ".ScopeA", AssemblyName + ".ScopeB"];
    private const string Namespace = "CsWinRT.IncrementalReplay";
    private const string TypeName = "Stringable";
    private const string MarkerMethod = "BodyMarker";
    private const int OriginalMarker = 0x12345678;
    private const int ChangedMarker = 0x23456789;

    internal static void Create(string path, string systemRuntimePath, string winrtRuntimePath)
    {
        ModuleDefinition systemRuntime = ReadModule(systemRuntimePath);
        ModuleDefinition winrtRuntime = ReadModule(winrtRuntimePath);

        Require(systemRuntime.Assembly?.Name == "System.Runtime", "The probe requires the input System.Runtime implementation.");
        Require(winrtRuntime.Assembly?.Name == "WinRT.Runtime", "The probe requires the input WinRT.Runtime implementation.");
        Require(winrtRuntime.TopLevelTypes.Any(type => type.FullName == "Windows.Foundation.IStringable") &&
                winrtRuntime.TopLevelTypes.Any(type => type.FullName == "WindowsRuntime.WindowsRuntimeClassNameAttribute"),
            "WinRT.Runtime must define IStringable and WindowsRuntimeClassNameAttribute.");

        ModuleDefinition module = new(AssemblyName + ".dll", CreateReference(systemRuntime.Assembly!))
        {
            Mvid = new Guid("8E5764EF-F078-414D-A073-606449C4015C")
        };
        AssemblyDefinition assembly = new(AssemblyName, new Version(1, 0, 0, 0)) { Modules = { module } };
        TypeReference targetFrameworkAttribute = new(module, module.CorLibTypeFactory.CorLibScope,
            "System.Runtime.Versioning", "TargetFrameworkAttribute");
        assembly.CustomAttributes.Add(new CustomAttribute(
            new MemberReference(targetFrameworkAttribute, ".ctor",
                MethodSignature.CreateInstance(module.CorLibTypeFactory.Void, [module.CorLibTypeFactory.String])),
            new CustomAttributeSignature(new CustomAttributeArgument(module.CorLibTypeFactory.String, ".NETCoreApp,Version=v10.0"))));
        AssemblyReference winrtReference = CreateReference(winrtRuntime.Assembly!);
        module.AssemblyReferences.Add(winrtReference);

        TypeDefinition type = AddStringable(module, winrtReference, TypeName);
        AddMarkerMethod(module, type, OriginalMarker);

        TypeReference list = new(module, module.CorLibTypeFactory.CorLibScope, "System.Collections.Generic", "IList`1");
        GenericInstanceTypeSignature listOfString = new(list, false, [module.CorLibTypeFactory.String]);
        MethodDefinition root = new("RootList",
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            MethodSignature.CreateStatic(listOfString))
        {
            CilMethodBody = new CilMethodBody()
        };
        // An explicit TypeSpec makes this root independent of how method-signature discovery evolves.
        root.CilMethodBody.Instructions.Add(CilOpCodes.Ldtoken, new TypeSpecification(listOfString));
        root.CilMethodBody.Instructions.Add(CilOpCodes.Pop);
        root.CilMethodBody.Instructions.Add(CilOpCodes.Ldnull);
        root.CilMethodBody.Instructions.Add(CilOpCodes.Ret);
        type.Methods.Add(root);
        AddScopedGenericRoots(module, type, winrtReference, systemRuntime.Assembly!, Path.GetDirectoryName(path)!);
        module.Write(path);

        ModuleDefinition written = ReadModule(path);
        Require(written.Assembly?.Name == AssemblyName &&
                written.CorLibTypeFactory.CorLibScope.Name == "System.Runtime" &&
                written.AssemblyReferences.Any(reference => reference.Name == "WinRT.Runtime") &&
                written.AssemblyReferences.All(reference => reference.Attributes == 0),
            "The generated probe lost its assembly references.");
        Require(GetProbeType(written).Interfaces.Single().Interface?.FullName == "Windows.Foundation.IStringable",
            "The generated probe must explicitly implement IStringable.");
        Require(ReadMarkerBody(path).AsSpan().SequenceEqual(new byte[] { 0x20, 0x78, 0x56, 0x34, 0x12, 0x2A }),
            "The probe marker must have the expected real IL bytes.");
        Require(TargetRuntimeProber.TryGetLikelyTargetRuntime(PEImage.FromBytes(File.ReadAllBytes(path)), out DotNetRuntimeInfo runtime) &&
                runtime.IsNetCoreApp && runtime.Version >= new Version(10, 0),
            "The probe must be a valid .NET 10+ output assembly for standalone replay.");
        TypeSignature[] scopedRoots = GetProbeType(written).Methods.Single(method => method.Name == "RootScopedCarriers")
            .CilMethodBody!.Instructions.Where(instruction => instruction.OpCode == CilOpCodes.Ldtoken)
            .Select(instruction => ((TypeSpecification)instruction.Operand!).Signature!).ToArray();
        Require(scopedRoots.Length == 6 && scopedRoots.GroupBy(signature => signature.FullName).Count() == 3 &&
                scopedRoots.GroupBy(signature => signature.FullName).All(group => group.Count() == 2),
            "The scoped generic roots must have identical display names in pairs, distinguished only by assembly scope.");
    }

    internal static string[] GetSupportingPaths(string probePath) =>
        ScopedAssemblyNames.Select(name => Path.Combine(Path.GetDirectoryName(probePath)!, name + ".dll")).ToArray();

    internal static void VerifyGeneratedProxies(string outputPath)
    {
        ModuleDefinition module = ReadModule(outputPath);
        string prefix = $"<{AssemblyName.Replace('.', '-')}>";
        string carrier = prefix + "ScopedCarrier'1";
        string qualifiedCarrier = prefix + Namespace.Replace('.', '-') + "-ScopedCarrier'1";

        void RequireProxy(string name)
        {
            Require(module.TopLevelTypes.Any(type => type.Namespace == "ABI." + Namespace && type.Name == name),
                $"The seed did not generate the expected probe proxy 'ABI.{Namespace}.{name}'.");
        }

        // User type keys can be encoded solely in attribute blobs, without any AssemblyRef/TypeRef rows.
        RequireProxy(prefix + TypeName);
        foreach (string scope in ScopedAssemblyNames)
        {
            string argument = $"<{scope.Replace('.', '-')}>{Namespace.Replace('.', '-')}-ScopedValue";
            RequireProxy($"{carrier}<{argument}>");
            RequireProxy($"{carrier}<<{argument}>Array>");
            RequireProxy($"{carrier}<{qualifiedCarrier}<{argument}>>");
        }
    }

    private static void AddScopedGenericRoots(
        ModuleDefinition module,
        TypeDefinition rootType,
        AssemblyReference winrtReference,
        AssemblyDefinition systemRuntime,
        string directory)
    {
        TypeDefinition carrier = AddStringable(module, winrtReference, "ScopedCarrier`1");
        carrier.GenericParameters.Add(new GenericParameter("T"));
        MethodDefinition root = new("RootScopedCarriers",
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            MethodSignature.CreateStatic(module.CorLibTypeFactory.Void))
        {
            CilMethodBody = new CilMethodBody()
        };
        rootType.Methods.Add(root);
        Guid[] mvids = [new("94AF6022-EAC8-419F-9280-002C4B841AA1"), new("94AF6022-EAC8-419F-9280-002C4B841AA2")];

        for (int i = 0; i < ScopedAssemblyNames.Length; i++)
        {
            string name = ScopedAssemblyNames[i];
            ModuleDefinition scopedModule = new(name + ".dll", CreateReference(systemRuntime))
            {
                Mvid = mvids[i]
            };
            AssemblyDefinition scopedAssembly = new(name, new Version(1, 0, 0, 0)) { Modules = { scopedModule } };
            TypeDefinition argumentType = new(Namespace, "ScopedValue", TypeAttributes.Public | TypeAttributes.Sealed,
                scopedModule.CorLibTypeFactory.Object.Type);
            scopedModule.TopLevelTypes.Add(argumentType);
            AddConstructor(scopedModule, argumentType);
            scopedModule.Write(Path.Combine(directory, name + ".dll"));

            AssemblyReference scope = CreateReference(scopedAssembly);
            module.AssemblyReferences.Add(scope);
            TypeSignature argument = new TypeReference(module, scope, Namespace, "ScopedValue").ToTypeSignature(false);

            // These pairs share every displayed type name and outer assembly. Sorting must recurse into
            // generic arguments, including array element types, to distinguish ScopeA from ScopeB.
            foreach (TypeSignature type in new TypeSignature[]
            {
                new GenericInstanceTypeSignature(carrier, false, [argument]),
                new GenericInstanceTypeSignature(carrier, false, [new SzArrayTypeSignature(argument)]),
                new GenericInstanceTypeSignature(carrier, false, [new GenericInstanceTypeSignature(carrier, false, [argument])])
            })
            {
                root.CilMethodBody.Instructions.Add(CilOpCodes.Ldtoken, new TypeSpecification(type));
                root.CilMethodBody.Instructions.Add(CilOpCodes.Pop);
            }
        }
        root.CilMethodBody.Instructions.Add(CilOpCodes.Ret);
    }

    private static AssemblyReference CreateReference(AssemblyDefinition assembly)
    {
        AssemblyReference reference = assembly.ToAssemblyReference();
        // Framework definitions can carry architecture flags (for example 0x70). C# references use
        // just the identity and public key token; copying those flags changes signature comparisons.
        reference.Attributes &= ~AssemblyAttributes.FullMask;
        return reference;
    }

    internal static void Mutate(string path, string scenario)
    {
        byte[] originalBytes = File.ReadAllBytes(path);
        ModuleDefinition module = ModuleDefinition.FromBytes(originalBytes);
        Guid? originalMvid = module.Mvid;
        int originalTypeCount = module.GetAllTypes().Count();
        byte[] originalBody = ReadMarkerBody(path);
        TypeDefinition type = GetProbeType(module);

        switch (scenario)
        {
            case "method-body":
                type.Methods.Single(method => method.Name == MarkerMethod).CilMethodBody!.Instructions[0] =
                    new CilInstruction(CilOpCodes.Ldc_I4, ChangedMarker);
                break;
            case "unrelated-type":
                TypeDefinition unrelated = new(Namespace, "ManagedOnlyAddition",
                    TypeAttributes.Public | TypeAttributes.Sealed, module.CorLibTypeFactory.Object.Type);
                module.TopLevelTypes.Add(unrelated);
                AddMarkerMethod(module, unrelated, OriginalMarker);
                break;
            case "type-identity":
                type.Name = "RenamedStringable";
                break;
            case "assembly-identity":
                module.Assembly!.Name = AssemblyName + ".Renamed";
                break;
            case "assembly-version":
                module.Assembly!.Version = new Version(1, 0, 0, 1);
                break;
            case "runtime-class-name":
                TypeReference attributeType = new(module, GetWinrtReference(module),
                    "WindowsRuntime", "WindowsRuntimeClassNameAttribute");
                MemberReference constructor = new(attributeType, ".ctor",
                    MethodSignature.CreateInstance(module.CorLibTypeFactory.Void, [module.CorLibTypeFactory.String]));
                type.CustomAttributes.Add(new CustomAttribute(constructor,
                    new CustomAttributeSignature(new CustomAttributeArgument(module.CorLibTypeFactory.String, Namespace + ".CustomName"))));
                break;
            case "interop-usage":
                _ = AddStringable(module, GetWinrtReference(module), "AdditionalStringable");
                break;
            default:
                throw new ArgumentException($"Unknown probe mutation '{scenario}'.", nameof(scenario));
        }

        module.Mvid = new Guid("A6015123-753F-4376-8B46-846D653FC4EF");
        module.Write(path);

        ModuleDefinition changed = ReadModule(path);
        Require(!originalBytes.AsSpan().SequenceEqual(File.ReadAllBytes(path)), $"'{scenario}' did not change the probe bytes.");
        Require(changed.Mvid != originalMvid, $"'{scenario}' did not change the probe MVID.");

        switch (scenario)
        {
            case "method-body":
                byte[] changedBody = ReadMarkerBody(path);
                Require(!originalBody.AsSpan().SequenceEqual(changedBody) &&
                        changedBody.AsSpan().SequenceEqual(new byte[] { 0x20, 0x89, 0x67, 0x45, 0x23, 0x2A }),
                    "The method-body mutation must change actual IL, not just the MVID.");
                Require(changed.GetAllTypes().Count() == originalTypeCount,
                    "The method-body mutation unexpectedly changed the type set.");
                break;
            case "unrelated-type":
                TypeDefinition unrelated = changed.TopLevelTypes.Single(candidate => candidate.Name == "ManagedOnlyAddition");
                Require(changed.GetAllTypes().Count() == originalTypeCount + 1 &&
                        unrelated.Interfaces.Count == 0 && unrelated.GenericParameters.Count == 0 &&
                        unrelated.CustomAttributes.Count == 0 && originalBody.AsSpan().SequenceEqual(ReadMarkerBody(path)),
                    "The unrelated addition must be a plain managed type, with no new marshalled generic or interface.");
                break;
            case "type-identity":
                Require(changed.TopLevelTypes.Any(candidate => candidate.Name == "RenamedStringable") &&
                        changed.TopLevelTypes.All(candidate => candidate.Name != TypeName),
                    "The exposed type was not renamed.");
                break;
            case "assembly-identity":
                Require(changed.Assembly!.Name == AssemblyName + ".Renamed", "The assembly was not renamed.");
                break;
            case "assembly-version":
                Require(changed.Assembly!.Version == new Version(1, 0, 0, 1), "The assembly version was not changed.");
                break;
            case "runtime-class-name":
                CustomAttribute attribute = GetProbeType(changed).CustomAttributes.Single();
                Require(attribute.Constructor?.DeclaringType?.FullName == "WindowsRuntime.WindowsRuntimeClassNameAttribute" &&
                        attribute.Signature?.FixedArguments.Single().Element?.ToString() == Namespace + ".CustomName",
                    "The runtime class name attribute did not round-trip.");
                break;
            case "interop-usage":
                Require(changed.GetAllTypes().Count() == originalTypeCount + 1 &&
                        changed.TopLevelTypes.Single(candidate => candidate.Name == "AdditionalStringable")
                            .Interfaces.Single().Interface?.FullName == "Windows.Foundation.IStringable",
                    "The interop addition must introduce a new, explicitly exposed managed type.");
                break;
        }
    }

    internal static void ChangePeBytesKeepingMvid(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Guid mvid = ReadMvid(path);
        using (PEReader reader = new(new MemoryStream(bytes, writable: false)))
        {
            // Mutate a real PE byte outside metadata without invalidating any type, IL, or assembly identity.
            bytes[reader.PEHeaders.CoffHeaderStartOffset + 4] ^= 1;
        }
        File.WriteAllBytes(path, bytes);
        Require(ReadMvid(path) == mvid, "The PE-byte mutation unexpectedly changed the MVID.");
    }

    internal static Guid ReadMvid(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using PEReader reader = new(stream);
        var metadata = reader.GetMetadataReader();
        return metadata.GetGuid(metadata.GetModuleDefinition().Mvid);
    }

    internal static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static ModuleDefinition ReadModule(string path) => ModuleDefinition.FromBytes(File.ReadAllBytes(path));

    private static TypeDefinition GetProbeType(ModuleDefinition module) =>
        module.TopLevelTypes.Single(type => type.Namespace == Namespace && type.Name == TypeName);

    private static AssemblyReference GetWinrtReference(ModuleDefinition module) =>
        module.AssemblyReferences.Single(reference => reference.Name == "WinRT.Runtime");

    private static TypeDefinition AddStringable(ModuleDefinition module, AssemblyReference winrtReference, string name)
    {
        TypeDefinition type = new(Namespace, name, TypeAttributes.Public | TypeAttributes.Sealed,
            module.CorLibTypeFactory.Object.Type);
        type.Interfaces.Add(new InterfaceImplementation(new TypeReference(module, winrtReference, "Windows.Foundation", "IStringable")));
        module.TopLevelTypes.Add(type);
        AddConstructor(module, type);

        MethodDefinition toString = new("ToString",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
            MethodSignature.CreateInstance(module.CorLibTypeFactory.String))
        {
            CilMethodBody = new CilMethodBody()
        };
        toString.CilMethodBody.Instructions.Add(CilOpCodes.Ldstr, "Incremental replay probe");
        toString.CilMethodBody.Instructions.Add(CilOpCodes.Ret);
        type.Methods.Add(toString);

        return type;
    }

    private static void AddConstructor(ModuleDefinition module, TypeDefinition type)
    {
        MethodDefinition constructor = new(".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
            MethodSignature.CreateInstance(module.CorLibTypeFactory.Void))
        {
            CilMethodBody = new CilMethodBody()
        };
        constructor.CilMethodBody.Instructions.Add(CilOpCodes.Ldarg_0);
        constructor.CilMethodBody.Instructions.Add(CilOpCodes.Call,
            new MemberReference(module.CorLibTypeFactory.Object.Type, ".ctor", MethodSignature.CreateInstance(module.CorLibTypeFactory.Void)));
        constructor.CilMethodBody.Instructions.Add(CilOpCodes.Ret);
        type.Methods.Add(constructor);
    }

    private static void AddMarkerMethod(ModuleDefinition module, TypeDefinition type, int value)
    {
        MethodDefinition method = new(MarkerMethod,
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            MethodSignature.CreateStatic(module.CorLibTypeFactory.Int32))
        {
            CilMethodBody = new CilMethodBody()
        };
        method.CilMethodBody.Instructions.Add(CilOpCodes.Ldc_I4, value);
        method.CilMethodBody.Instructions.Add(CilOpCodes.Ret);
        type.Methods.Add(method);
    }

    private static byte[] ReadMarkerBody(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using PEReader reader = new(stream);
        var metadata = reader.GetMetadataReader();
        var type = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition)
            .Single(definition => metadata.GetString(definition.Namespace) == Namespace && metadata.GetString(definition.Name) == TypeName);
        var method = type.GetMethods().Select(metadata.GetMethodDefinition)
            .Single(definition => metadata.GetString(definition.Name) == MarkerMethod);
        return reader.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()
            ?? throw new InvalidDataException("The probe marker has no IL.");
    }
}
