// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.WinMDGenerator.Discovery;
using WindowsRuntime.WinMDGenerator.Models;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodAttributes;
using TypeAttributes = AsmResolver.PE.DotNet.Metadata.Tables.TypeAttributes;

namespace WindowsRuntime.WinMDGenerator.Generation;

internal sealed partial class WinmdWriter
{
    private enum SynthesizedInterfaceType
    {
        Static,
        Factory,
        Default
    }

    private static string GetSynthesizedInterfaceName(string className, SynthesizedInterfaceType type)
    {
        return "I" + className + type switch
        {
            SynthesizedInterfaceType.Default => "Class",
            SynthesizedInterfaceType.Factory => "Factory",
            SynthesizedInterfaceType.Static => "Static",
            _ => "",
        };
    }

    private void AddSynthesizedInterfaces(TypeDefinition inputType, TypeDefinition classOutputType, TypeDeclaration classDeclaration)
    {
        // Static vs non-static member filtering is handled below per-member

        // Collect members that come from interface implementations
        HashSet<string> membersFromInterfaces = [];
        foreach (InterfaceImplementation impl in inputType.Interfaces)
        {
            TypeDefinition? interfaceDef = impl.Interface?.Resolve();
            if (interfaceDef == null)
            {
                continue;
            }

            foreach (MethodDefinition interfaceMethod in interfaceDef.Methods)
            {
                // Find the class member that implements this interface member
                string methodName = interfaceMethod.Name?.Value ?? "";
                _ = membersFromInterfaces.Add(methodName);
            }
        }

        AddSynthesizedInterface(inputType, classOutputType, classDeclaration, SynthesizedInterfaceType.Static, membersFromInterfaces);
        AddSynthesizedInterface(inputType, classOutputType, classDeclaration, SynthesizedInterfaceType.Factory, membersFromInterfaces);
        AddSynthesizedInterface(inputType, classOutputType, classDeclaration, SynthesizedInterfaceType.Default, membersFromInterfaces);
    }

    private void AddSynthesizedInterface(
        TypeDefinition inputType,
        TypeDefinition classOutputType,
        TypeDeclaration classDeclaration,
        SynthesizedInterfaceType interfaceType,
        HashSet<string> membersFromInterfaces)
    {
        bool hasMembers = false;
        string ns = AssemblyAnalyzer.GetEffectiveNamespace(inputType) ?? "";
        string className = inputType.Name!.Value;
        string interfaceName = GetSynthesizedInterfaceName(className, interfaceType);

        TypeAttributes typeAttributes =
            TypeAttributes.NotPublic |
            (TypeAttributes)0x4000 | // WindowsRuntime
            TypeAttributes.AutoLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Interface |
            TypeAttributes.Abstract;

        TypeDefinition synthesizedInterface = new(ns, interfaceName, typeAttributes);

        // Add members to the synthesized interface
        foreach (MethodDefinition method in inputType.Methods)
        {
            if (!method.IsPublic)
            {
                continue;
            }

            if (interfaceType == SynthesizedInterfaceType.Factory &&
                method.IsConstructor &&
                method.Parameters.Count > 0)
            {
                // Factory methods: parameterized constructors become Create methods
                hasMembers = true;
                AddFactoryMethod(synthesizedInterface, inputType, method);
            }
            else if (interfaceType == SynthesizedInterfaceType.Static && method.IsStatic && !method.IsConstructor && !method.IsSpecialName)
            {
                hasMembers = true;
                AddMethodToInterface(synthesizedInterface, method);
            }
            else if (interfaceType == SynthesizedInterfaceType.Default && !method.IsStatic && !method.IsConstructor && !method.IsSpecialName)
            {
                // Only include members not already from an interface
                if (!membersFromInterfaces.Contains(method.Name?.Value ?? ""))
                {
                    hasMembers = true;
                    AddMethodToInterface(synthesizedInterface, method);
                }
            }
        }

        // Add properties
        foreach (PropertyDefinition property in inputType.Properties)
        {
            bool isStatic = property.GetMethod?.IsStatic == true || property.SetMethod?.IsStatic == true;
            bool isPublic = property.GetMethod?.IsPublic == true || property.SetMethod?.IsPublic == true;

            if (!isPublic)
            {
                continue;
            }

            if ((interfaceType == SynthesizedInterfaceType.Static && isStatic) ||
                (interfaceType == SynthesizedInterfaceType.Default && !isStatic))
            {
                // For default interface, skip properties already provided by an implemented interface
                if (interfaceType == SynthesizedInterfaceType.Default)
                {
                    string getterName = "get_" + property.Name!.Value;
                    if (membersFromInterfaces.Contains(getterName))
                    {
                        continue;
                    }
                }

                hasMembers = true;
                AddPropertyToType(synthesizedInterface, property, isInterfaceParent: true);
            }
        }

        // Add events
        foreach (EventDefinition evt in inputType.Events)
        {
            bool isStatic = evt.AddMethod?.IsStatic == true;
            bool isPublic = evt.AddMethod?.IsPublic == true || evt.RemoveMethod?.IsPublic == true;

            if (!isPublic)
            {
                continue;
            }

            if ((interfaceType == SynthesizedInterfaceType.Static && isStatic) ||
                (interfaceType == SynthesizedInterfaceType.Default && !isStatic))
            {
                // For default interface, skip events already provided by an implemented interface
                if (interfaceType == SynthesizedInterfaceType.Default)
                {
                    string adderName = "add_" + evt.Name!.Value;
                    if (membersFromInterfaces.Contains(adderName))
                    {
                        continue;
                    }
                }

                hasMembers = true;
                AddEventToType(synthesizedInterface, evt, isInterfaceParent: true);
            }
        }

        // Only emit the interface if it has members, or if it's the default and the class has no other interfaces
        if (hasMembers || (interfaceType == SynthesizedInterfaceType.Default && inputType.Interfaces.Count == 0))
        {
            _outputModule.TopLevelTypes.Add(synthesizedInterface);

            string qualifiedInterfaceName = string.IsNullOrEmpty(ns) ? interfaceName : $"{ns}.{interfaceName}";

            TypeDeclaration interfaceDeclaration = new(null, synthesizedInterface, isComponentType: false);
            _typeDefinitionMapping[qualifiedInterfaceName] = interfaceDeclaration;

            int version = GetVersion(inputType);

            if (interfaceType == SynthesizedInterfaceType.Default)
            {
                classDeclaration.DefaultInterface = qualifiedInterfaceName;

                // Add interface implementation on the class (use the TypeDefinition directly since it's in the same module)
                InterfaceImplementation interfaceImpl = new(synthesizedInterface);
                classOutputType.Interfaces.Add(interfaceImpl);

                // Add DefaultAttribute on the interface implementation
                AddDefaultAttribute(interfaceImpl);
            }

            // Add version attribute
            AddVersionAttribute(synthesizedInterface, version);

            // Add GUID attribute
            AddGuidAttributeFromName(synthesizedInterface, interfaceName);

            // Add ExclusiveTo attribute
            AddExclusiveToAttribute(synthesizedInterface, AssemblyAnalyzer.GetQualifiedName(inputType));

            if (interfaceType == SynthesizedInterfaceType.Factory)
            {
                AddActivatableAttribute(classOutputType, (uint)version, qualifiedInterfaceName);
            }
            else if (interfaceType == SynthesizedInterfaceType.Static)
            {
                classDeclaration.StaticInterface = qualifiedInterfaceName;
                AddStaticAttribute(classOutputType, (uint)version, qualifiedInterfaceName);
            }
        }
    }

    private void AddFactoryMethod(TypeDefinition synthesizedInterface, TypeDefinition classType, MethodDefinition constructor)
    {
        TypeSignature returnType = ImportTypeReference(classType).ToTypeSignature();

        TypeSignature[] parameterTypes = [.. constructor.Signature!.ParameterTypes
            .Select(MapTypeSignatureToOutput)];

        MethodDefinition factoryMethod = new(
            "Create" + classType.Name!.Value,
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot,
            MethodSignature.CreateInstance(returnType, parameterTypes));

        // Add parameter names
        int paramIndex = 1;
        foreach (ParameterDefinition inputParam in constructor.ParameterDefinitions)
        {
            factoryMethod.ParameterDefinitions.Add(new ParameterDefinition(
                (ushort)paramIndex++,
                inputParam.Name!.Value,
                inputParam.Attributes));
        }

        synthesizedInterface.Methods.Add(factoryMethod);
    }

    /// <summary>
    /// Processes custom mapped interfaces for a class type. This maps .NET collection interfaces,
    /// IDisposable, INotifyPropertyChanged, etc. to their WinRT equivalents.
    /// </summary>
    private void ProcessCustomMappedInterfaces(TypeDefinition inputType, TypeDefinition outputType)
    {
        foreach (InterfaceImplementation impl in inputType.Interfaces)
        {
            if (impl.Interface == null)
            {
                continue;
            }

            string interfaceName = GetInterfaceQualifiedName(impl.Interface);

            if (!_mapper.HasMappingForType(interfaceName))
            {
                continue;
            }

            MappedType mapping = _mapper.GetMappedType(interfaceName);
            (string mappedNs, string mappedName, string mappedAssembly, _, _) = mapping.GetMapping();

            // Add the mapped interface as an implementation on the output type
            TypeReference mappedInterfaceRef = GetOrCreateTypeReference(mappedNs, mappedName, mappedAssembly);

            // For generic interfaces, we need to handle type arguments
            if (impl.Interface is TypeSpecification typeSpec && typeSpec.Signature is GenericInstanceTypeSignature genericInst)
            {
                TypeSignature[] mappedArgs = [.. genericInst.TypeArguments
                    .Select(MapTypeSignatureToOutput)];
                TypeSpecification mappedSpec = new(new GenericInstanceTypeSignature(mappedInterfaceRef, false, mappedArgs));
                outputType.Interfaces.Add(new InterfaceImplementation(mappedSpec));
            }
            else
            {
                outputType.Interfaces.Add(new InterfaceImplementation(mappedInterfaceRef));
            }
        }
    }

    private static string GetInterfaceQualifiedName(ITypeDefOrRef type)
    {
        return type is TypeSpecification typeSpec && typeSpec.Signature is GenericInstanceTypeSignature genericInst
            ? AssemblyAnalyzer.GetQualifiedName(genericInst.GenericType)
            : AssemblyAnalyzer.GetQualifiedName(type);
    }
}