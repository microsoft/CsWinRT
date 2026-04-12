// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.WinMDGenerator.Discovery;
using WindowsRuntime.WinMDGenerator.Errors;
using WindowsRuntime.WinMDGenerator.Models;
using AssemblyAttributes = AsmResolver.PE.DotNet.Metadata.Tables.AssemblyAttributes;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <summary>
/// Writes a WinMD file from analyzed assembly types using AsmResolver.
/// </summary>
internal sealed partial class WinmdWriter
{
    private readonly string _version;
    private readonly TypeMapper _mapper;
    private readonly ModuleDefinition _inputModule;

    // Output WinMD module and assembly
    private readonly ModuleDefinition _outputModule;

    // Tracking for type definitions in the output WinMD
    private readonly Dictionary<string, TypeDeclaration> _typeDefinitionMapping = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AssemblyReference> _assemblyReferenceCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TypeReference> _typeReferenceCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Creates a new <see cref="WinmdWriter"/> instance.
    /// </summary>
    public WinmdWriter(
        string assemblyName,
        string version,
        TypeMapper mapper,
        ModuleDefinition inputModule)
    {
        _version = version;
        _mapper = mapper;
        _inputModule = inputModule;

        // Create the output WinMD module
        _outputModule = new ModuleDefinition(assemblyName + ".winmd")
        {
            RuntimeVersion = "WindowsRuntime 1.4"
        };

        // Replace the default mscorlib reference with the WinMD-style one (v255.255.255.255 with PKT)
        AssemblyReference defaultCorLib = (AssemblyReference)_outputModule.CorLibTypeFactory.CorLibScope;
        defaultCorLib.Version = new Version(0xFF, 0xFF, 0xFF, 0xFF);
        defaultCorLib.PublicKeyOrToken = [0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89];
        _assemblyReferenceCache["mscorlib"] = defaultCorLib;

        // Create the output assembly with WindowsRuntime flag (keep reference alive via module)
        _ = new AssemblyDefinition(assemblyName, new Version(version))
        {
            Modules = { _outputModule },
            Attributes = AssemblyAttributes.ContentWindowsRuntime,
            HashAlgorithm = AsmResolver.PE.DotNet.Metadata.Tables.AssemblyHashAlgorithm.Sha1
        };
    }

    /// <summary>
    /// Processes a public type from the input assembly and adds it to the WinMD.
    /// </summary>
    public void ProcessType(TypeDefinition inputType)
    {
        string qualifiedName = AssemblyAnalyzer.GetQualifiedName(inputType);

        if (_typeDefinitionMapping.ContainsKey(qualifiedName))
        {
            return;
        }

        if (inputType.IsEnum)
        {
            AddEnumType(inputType);
        }
        else if (inputType.IsDelegate)
        {
            AddDelegateType(inputType);
        }
        else if (inputType.IsInterface)
        {
            AddInterfaceType(inputType);
        }
        else if (inputType.IsValueType)
        {
            AddStructType(inputType);
        }
        else if (inputType.IsClass)
        {
            AddClassType(inputType);
        }
    }

    /// <summary>
    /// Finalizes the WinMD generation by adding MethodImpls, version attributes, and custom attributes.
    /// </summary>
    public void FinalizeGeneration()
    {
        // Phase 1: Add MethodImpl fixups for classes
        foreach ((string qualifiedName, TypeDeclaration declaration) in _typeDefinitionMapping)
        {
            if (declaration.OutputType == null || declaration.InputType == null || !declaration.IsComponentType)
            {
                continue;
            }

            TypeDefinition classOutputType = declaration.OutputType;
            TypeDefinition classInputType = declaration.InputType;

            // Add MethodImpls for implemented interfaces (excluding the default synthesized interface, handled below)
            foreach (InterfaceImplementation classInterfaceImpl in classOutputType.Interfaces)
            {
                // Resolve the interface — handle TypeSpecification (generic instances) by resolving the GenericType
                TypeDefinition? interfaceDef = classInterfaceImpl.Interface is TypeSpecification ts
                    && ts.Signature is GenericInstanceTypeSignature gits
                    ? gits.GenericType.Resolve()
                    : classInterfaceImpl.Interface?.Resolve();

                // If the output interface can't be resolved (WinRT contract assemblies),
                // find the matching interface from the INPUT type which points to resolvable projection assemblies
                if (interfaceDef == null)
                {
                    string outputIfaceName = GetInterfaceQualifiedName(classInterfaceImpl.Interface!);
                    foreach (InterfaceImplementation inputImpl in classInputType.Interfaces)
                    {
                        if (inputImpl.Interface != null && GetInterfaceQualifiedName(inputImpl.Interface) == outputIfaceName)
                        {
                            interfaceDef = inputImpl.Interface is TypeSpecification its
                                && its.Signature is GenericInstanceTypeSignature igits
                                ? igits.GenericType.Resolve()
                                : inputImpl.Interface.Resolve();
                            break;
                        }
                    }
                }

                if (interfaceDef == null)
                {
                    // Still unresolvable — MethodImpls for mapped interfaces are already
                    // created by AddCustomMappedTypeMembers, so this is expected for those.
                    continue;
                }

                // Skip the default synthesized interface — it's handled separately below
                string interfaceQualName = AssemblyAnalyzer.GetQualifiedName(interfaceDef);
                if (interfaceQualName == declaration.DefaultInterface)
                {
                    continue;
                }

                foreach (MethodDefinition interfaceMethod in interfaceDef.Methods)
                {
                    // Find the corresponding method on the class (by name or explicit implementation pattern)
                    MethodDefinition? classMethod = FindMatchingMethod(classOutputType, interfaceMethod);
                    if (classMethod == null)
                    {
                        // Try matching explicit implementation: look for "Namespace.IFoo.MethodName" pattern
                        // Also verify parameter count and types match to avoid wrong overload matches
                        string explicitName = $"{interfaceQualName}.{interfaceMethod.Name?.Value}";
                        int paramCount = interfaceMethod.Signature?.ParameterTypes.Count ?? 0;
                        classMethod = classOutputType.Methods.FirstOrDefault(m =>
                        {
                            if (m.Name?.Value != explicitName)
                            {
                                return false;
                            }

                            if ((m.Signature?.ParameterTypes.Count ?? 0) != paramCount)
                            {
                                return false;
                            }

                            for (int i = 0; i < paramCount; i++)
                            {
                                if (m.Signature!.ParameterTypes[i].FullName != interfaceMethod.Signature!.ParameterTypes[i].FullName)
                                {
                                    return false;
                                }
                            }

                            return true;
                        });
                    }

                    if (classMethod != null)
                    {
                        MemberReference interfaceMethodRef = new(classInterfaceImpl.Interface, interfaceMethod.Name!.Value, interfaceMethod.Signature);
                        classOutputType.MethodImplementations.Add(new MethodImplementation(interfaceMethodRef, classMethod));
                    }
                }
            }

            // Add MethodImpls for default synthesized interface
            if (declaration.DefaultInterface != null &&
                _typeDefinitionMapping.TryGetValue(declaration.DefaultInterface, out TypeDeclaration? defaultInterfaceDecl) &&
                defaultInterfaceDecl.OutputType != null)
            {
                TypeDefinition defaultInterface = defaultInterfaceDecl.OutputType;

                foreach (MethodDefinition interfaceMethod in defaultInterface.Methods)
                {
                    MethodDefinition? classMethod = FindMatchingMethod(classOutputType, interfaceMethod);
                    if (classMethod != null)
                    {
                        MemberReference interfaceMethodRef = new(defaultInterface, interfaceMethod.Name!.Value, interfaceMethod.Signature);
                        classOutputType.MethodImplementations.Add(new MethodImplementation(interfaceMethodRef, classMethod));
                    }
                }
            }
        }

        // Phase 2: Add default version attributes for types that don't have one
        int defaultVersion = Version.Parse(_version).Major;

        foreach ((string _, TypeDeclaration declaration) in _typeDefinitionMapping)
        {
            if (declaration.OutputType == null)
            {
                continue;
            }

            if (!HasVersionAttribute(declaration.OutputType))
            {
                // Use the version from the input type if available, otherwise use the default
                int version = declaration.InputType != null ? GetVersion(declaration.InputType) : defaultVersion;
                AddVersionAttribute(declaration.OutputType, version);
            }
        }

        // Phase 3: Add custom attributes from input types to output types
        foreach ((string _, TypeDeclaration declaration) in _typeDefinitionMapping)
        {
            if (declaration.OutputType == null || declaration.InputType == null || !declaration.IsComponentType)
            {
                continue;
            }

            CopyCustomAttributes(declaration.InputType, declaration.OutputType);
        }

        // Phase 4: Add overload attributes for methods with the same name
        foreach ((string _, TypeDeclaration declaration) in _typeDefinitionMapping)
        {
            if (declaration.OutputType == null)
            {
                continue;
            }

            AddOverloadAttributesForType(declaration.OutputType);
        }
    }

    private static MethodDefinition? FindMatchingMethod(TypeDefinition classType, MethodDefinition interfaceMethod)
    {
        string methodName = interfaceMethod.Name?.Value ?? "";

        foreach (MethodDefinition classMethod in classType.Methods)
        {
            if (classMethod.Name?.Value != methodName)
            {
                continue;
            }

            // Match parameter count
            if (classMethod.Signature?.ParameterTypes.Count != interfaceMethod.Signature?.ParameterTypes.Count)
            {
                continue;
            }

            // Match parameter types
            bool parametersMatch = true;
            for (int i = 0; i < (classMethod.Signature?.ParameterTypes.Count ?? 0); i++)
            {
                if (classMethod.Signature!.ParameterTypes[i].FullName != interfaceMethod.Signature!.ParameterTypes[i].FullName)
                {
                    parametersMatch = false;
                    break;
                }
            }

            if (!parametersMatch)
            {
                continue;
            }

            return classMethod;
        }

        return null;
    }

    private void AddOverloadAttributesForType(TypeDefinition type)
    {
        // Group methods by name to find overloaded methods
        IEnumerable<IGrouping<string, MethodDefinition>> methodGroups = type.Methods
            .Where(m => !m.IsConstructor && !m.IsSpecialName)
            .GroupBy(m => m.Name?.Value ?? "")
            .Where(g => g.Count() > 1);

        foreach (IGrouping<string, MethodDefinition> group in methodGroups)
        {
            int overloadIndex = 1;
            foreach (MethodDefinition method in group.Skip(1))
            {
                overloadIndex++;
                string overloadName = $"{group.Key}{overloadIndex}";
                AddOverloadAttribute(method, overloadName);
            }
        }
    }

    private void AddOverloadAttribute(MethodDefinition method, string overloadName)
    {
        TypeReference overloadAttrType = GetOrCreateTypeReference(
            "Windows.Foundation.Metadata", "OverloadAttribute", "Windows.Foundation.FoundationContract");

        MemberReference ctor = new(overloadAttrType, ".ctor",
            MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                _outputModule.CorLibTypeFactory.String));

        CustomAttributeSignature sig = new();
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.String, overloadName));

        method.CustomAttributes.Add(new CustomAttribute(ctor, sig));
    }

    /// <summary>
    /// Writes the WinMD to the specified path.
    /// </summary>
    public void Write(string outputPath)
    {
        try
        {
            _outputModule.Write(outputPath);
        }
        catch (Exception e)
        {
            throw WellKnownWinMDExceptions.WinMDWriteError(e);
        }
    }
}