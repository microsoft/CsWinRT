// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.WinMDGenerator.Discovery;
using WindowsRuntime.WinMDGenerator.Models;
using FieldAttributes = AsmResolver.PE.DotNet.Metadata.Tables.FieldAttributes;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodAttributes;
using MethodImplAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodImplAttributes;
using MethodSemanticsAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodSemanticsAttributes;
using ParameterAttributes = AsmResolver.PE.DotNet.Metadata.Tables.ParameterAttributes;
using TypeAttributes = AsmResolver.PE.DotNet.Metadata.Tables.TypeAttributes;

namespace WindowsRuntime.WinMDGenerator.Generation;

internal sealed partial class WinmdWriter
{
    private void AddEnumType(TypeDefinition inputType)
    {
        string qualifiedName = AssemblyAnalyzer.GetQualifiedName(inputType);

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            (TypeAttributes)0x4000 | // WindowsRuntime
            TypeAttributes.AutoLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Sealed;

        TypeReference baseType = GetOrCreateTypeReference("System", "Enum", "mscorlib");

        TypeDefinition outputType = new(
            AssemblyAnalyzer.GetEffectiveNamespace(inputType),
            inputType.Name!.Value,
            typeAttributes,
            baseType);

        // Add the value__ field
        TypeSignature underlyingType = GetEnumUnderlyingType(inputType);
        FieldDefinition valueField = new(
            "value__",
            FieldAttributes.Private | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName,
            new FieldSignature(underlyingType));
        outputType.Fields.Add(valueField);

        _outputModule.TopLevelTypes.Add(outputType);

        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;

        // Enum literal fields use the enum type itself (not the underlying type)
        TypeSignature enumTypeSignature = new TypeDefOrRefSignature(outputType, isValueType: true);

        // Add enum members
        foreach (FieldDefinition field in inputType.Fields)
        {
            if (field.IsSpecialName)
            {
                continue; // Skip value__
            }

            if (!field.IsPublic)
            {
                continue;
            }

            FieldDefinition outputField = new(
                field.Name!.Value,
                FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault,
                new FieldSignature(enumTypeSignature));

            if (field.Constant != null)
            {
                outputField.Constant = new Constant(field.Constant.Type, new DataBlobSignature(field.Constant.Value!.Data));
            }

            outputType.Fields.Add(outputField);
        }
    }

    private TypeSignature GetEnumUnderlyingType(TypeDefinition enumType)
    {
        foreach (FieldDefinition field in enumType.Fields)
        {
            if (field.IsSpecialName && field.Name?.Value == "value__")
            {
                return MapTypeSignatureToOutput(field.Signature!.FieldType);
            }
        }

        // Default to Int32
        return _outputModule.CorLibTypeFactory.Int32;
    }

    private void AddDelegateType(TypeDefinition inputType)
    {
        string qualifiedName = AssemblyAnalyzer.GetQualifiedName(inputType);

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            (TypeAttributes)0x4000 | // WindowsRuntime
            TypeAttributes.AutoLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Sealed;

        TypeReference baseType = GetOrCreateTypeReference("System", "MulticastDelegate", "mscorlib");

        TypeDefinition outputType = new(
            AssemblyAnalyzer.GetEffectiveNamespace(inputType),
            inputType.Name!.Value,
            typeAttributes,
            baseType);

        // Register early so self-referencing signatures can find this type
        _outputModule.TopLevelTypes.Add(outputType);
        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;

        // Add .ctor(object, IntPtr) — private per WinRT delegate convention
        MethodDefinition ctor = new(
            ".ctor",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
            MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                _outputModule.CorLibTypeFactory.Object,
                _outputModule.CorLibTypeFactory.IntPtr))
        {
            ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
        };
        ctor.ParameterDefinitions.Add(new ParameterDefinition(1, "object", 0));
        ctor.ParameterDefinitions.Add(new ParameterDefinition(2, "method", 0));
        outputType.Methods.Add(ctor);

        // Add Invoke method
        MethodDefinition? inputInvoke = inputType.Methods.FirstOrDefault(m => m.Name?.Value == "Invoke");
        if (inputInvoke != null)
        {
            TypeSignature returnType = inputInvoke.Signature!.ReturnType is CorLibTypeSignature { ElementType: ElementType.Void }
                ? _outputModule.CorLibTypeFactory.Void
                : MapTypeSignatureToOutput(inputInvoke.Signature.ReturnType);

            TypeSignature[] parameterTypes = [.. inputInvoke.Signature.ParameterTypes
                .Select(MapTypeSignatureToOutput)];

            MethodDefinition invoke = new(
                "Invoke",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.NewSlot,
                MethodSignature.CreateInstance(returnType, parameterTypes))
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
            };

            // Add parameter names with [In] attribute
            int paramIndex = 1;
            foreach (ParameterDefinition inputParam in inputInvoke.ParameterDefinitions)
            {
                invoke.ParameterDefinitions.Add(new ParameterDefinition(
                    (ushort)paramIndex++,
                    inputParam.Name!.Value,
                    ParameterAttributes.In));
            }

            outputType.Methods.Add(invoke);
        }

        // Add GUID attribute
        AddGuidAttribute(outputType, inputType);
    }

    private void AddInterfaceType(TypeDefinition inputType)
    {
        string qualifiedName = AssemblyAnalyzer.GetQualifiedName(inputType);

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            (TypeAttributes)0x4000 | // WindowsRuntime
            TypeAttributes.AutoLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Interface |
            TypeAttributes.Abstract;

        TypeDefinition outputType = new(
            AssemblyAnalyzer.GetEffectiveNamespace(inputType),
            inputType.Name!.Value,
            typeAttributes);

        // Register early so self-referencing signatures can find this type
        _outputModule.TopLevelTypes.Add(outputType);
        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;

        // Add methods (skip property/event accessors — they're added by property/event processing below)
        foreach (MethodDefinition method in inputType.Methods)
        {
            if (!method.IsPublic || method.IsSpecialName)
            {
                continue;
            }

            AddMethodToInterface(outputType, method);
        }

        // Add properties
        foreach (PropertyDefinition property in inputType.Properties)
        {
            AddPropertyToType(outputType, property, isInterfaceParent: true);
        }

        // Add events
        foreach (EventDefinition evt in inputType.Events)
        {
            AddEventToType(outputType, evt, isInterfaceParent: true);
        }

        // Add interface implementations
        foreach (InterfaceImplementation impl in inputType.Interfaces)
        {
            if (impl.Interface != null)
            {
                ITypeDefOrRef outputInterfaceRef = ImportTypeReference(impl.Interface);
                outputType.Interfaces.Add(new InterfaceImplementation(outputInterfaceRef));
            }
        }

        // Add GUID attribute
        AddGuidAttribute(outputType, inputType);
    }

    private void AddStructType(TypeDefinition inputType)
    {
        string qualifiedName = AssemblyAnalyzer.GetQualifiedName(inputType);

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            (TypeAttributes)0x4000 | // WindowsRuntime
            TypeAttributes.SequentialLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Sealed;

        TypeReference baseType = GetOrCreateTypeReference("System", "ValueType", "mscorlib");

        TypeDefinition outputType = new(
            AssemblyAnalyzer.GetEffectiveNamespace(inputType),
            inputType.Name!.Value,
            typeAttributes,
            baseType);

        // Register early so self-referencing signatures can find this type
        _outputModule.TopLevelTypes.Add(outputType);
        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;

        // Add public fields
        foreach (FieldDefinition field in inputType.Fields)
        {
            if (!field.IsPublic || field.IsStatic)
            {
                continue;
            }

            FieldDefinition outputField = new(
                field.Name!.Value,
                FieldAttributes.Public,
                new FieldSignature(MapTypeSignatureToOutput(field.Signature!.FieldType)));
            outputType.Fields.Add(outputField);
        }
    }

    private void AddClassType(TypeDefinition inputType)
    {
        string qualifiedName = AssemblyAnalyzer.GetQualifiedName(inputType);

        TypeAttributes typeAttributes =
            TypeAttributes.Public |
            (TypeAttributes)0x4000 | // WindowsRuntime
            TypeAttributes.AutoLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.Class |
            TypeAttributes.BeforeFieldInit;

        // Sealed for: sealed classes and static classes (abstract+sealed in metadata)
        // WinRT doesn't support abstract base classes, so non-static abstract classes
        // are treated as regular unsealed runtime classes
        if (inputType.IsSealed)
        {
            typeAttributes |= TypeAttributes.Sealed;
        }

        // In C#, static classes are both abstract and sealed in metadata
        if (inputType.IsAbstract && inputType.IsSealed)
        {
            typeAttributes |= TypeAttributes.Abstract;
        }

        // Determine base type
        ITypeDefOrRef? baseType;
        if (inputType.BaseType != null && inputType.BaseType.FullName != "System.Object")
        {
            // Check if the base type is abstract; WinRT doesn't support projecting abstract classes
            TypeDefinition? baseTypeDef = inputType.BaseType.Resolve();
            baseType = baseTypeDef != null && baseTypeDef.IsAbstract
                ? GetOrCreateTypeReference("System", "Object", "mscorlib")
                : ImportTypeReference(inputType.BaseType);
        }
        else
        {
            baseType = GetOrCreateTypeReference("System", "Object", "mscorlib");
        }

        TypeDefinition outputType = new(
            AssemblyAnalyzer.GetEffectiveNamespace(inputType),
            inputType.Name!.Value,
            typeAttributes,
            baseType);

        // Register in the mapping early so self-referencing method signatures can find it
        _outputModule.TopLevelTypes.Add(outputType);
        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;

        bool hasConstructor = false;
        bool hasDefaultConstructor = false;
        bool hasAtLeastOneNonPublicConstructor = false;
        bool isStaticClass = inputType.IsAbstract && inputType.IsSealed;

        // Collect members from custom mapped interfaces and unmapped interfaces to exclude from the class
        HashSet<string> customMappedMembers = CollectCustomMappedMemberNames(inputType);

        // Add methods (non-property/event accessors)
        foreach (MethodDefinition method in inputType.Methods)
        {
            if (method.IsConstructor)
            {
                if (!method.IsPublic)
                {
                    hasAtLeastOneNonPublicConstructor = true;
                    continue;
                }

                hasConstructor = true;
                hasDefaultConstructor |= method.Parameters.Count == 0;
                AddMethodToClass(outputType, method);
            }
            else if (method.IsPublic && !method.IsSpecialName)
            {
                // Skip methods that belong to custom mapped or unmapped interfaces
                if (customMappedMembers.Contains(method.Name?.Value ?? ""))
                {
                    continue;
                }

                AddMethodToClass(outputType, method);
            }
        }

        // Add properties
        foreach (PropertyDefinition property in inputType.Properties)
        {
            // Skip properties that belong to custom mapped or unmapped interfaces
            if (customMappedMembers.Contains(property.Name?.Value ?? ""))
            {
                continue;
            }

            // Only add if at least one accessor is public
            bool hasPublicGetter = property.GetMethod?.IsPublic == true;
            bool hasPublicSetter = property.SetMethod?.IsPublic == true;
            if (hasPublicGetter || hasPublicSetter)
            {
                AddPropertyToType(outputType, property, isInterfaceParent: false);
            }
        }

        // Add events
        foreach (EventDefinition evt in inputType.Events)
        {
            // Skip events that belong to custom mapped or unmapped interfaces
            if (customMappedMembers.Contains(evt.Name?.Value ?? ""))
            {
                continue;
            }

            bool hasPublicAdder = evt.AddMethod?.IsPublic == true;
            bool hasPublicRemover = evt.RemoveMethod?.IsPublic == true;
            if (hasPublicAdder || hasPublicRemover)
            {
                AddEventToType(outputType, evt, isInterfaceParent: false);
            }
        }

        // Implicit constructor if none defined
        if (!hasConstructor && !hasAtLeastOneNonPublicConstructor && !isStaticClass)
        {
            MethodDefinition defaultCtor = new(
                ".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
                MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void))
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
            };
            outputType.Methods.Add(defaultCtor);
            hasDefaultConstructor = true;
        }

        // Add interface implementations (excluding mapped and unmappable interfaces)
        foreach (InterfaceImplementation impl in inputType.Interfaces)
        {
            if (impl.Interface == null || !IsPubliclyAccessible(impl.Interface))
            {
                continue;
            }

            string interfaceName = GetInterfaceQualifiedName(impl.Interface);

            // Skip interfaces that have a WinRT mapping — they'll be added as their
            // mapped equivalents by ProcessCustomMappedInterfaces below
            if (_mapper.HasMappingForType(interfaceName))
            {
                continue;
            }

            // Skip .NET interfaces that have no WinRT equivalent
            if (TypeMapper.ImplementedInterfacesWithoutMapping.Contains(interfaceName))
            {
                continue;
            }

            ITypeDefOrRef outputInterfaceRef = ImportTypeReference(impl.Interface);
            outputType.Interfaces.Add(new InterfaceImplementation(outputInterfaceRef));
        }

        // Add activatable attribute if it has a default constructor
        if (hasDefaultConstructor)
        {
            int version = GetVersion(inputType);
            AddActivatableAttribute(outputType, (uint)version, null);
        }

        // Process custom mapped interfaces (IList -> IVector, IDisposable -> IClosable, etc.)
        ProcessCustomMappedInterfaces(inputType, outputType);

        // Add synthesized interfaces (IFooClass, IFooFactory, IFooStatic)
        AddSynthesizedInterfaces(inputType, outputType, declaration);

        // Add explicit interface implementation methods (private methods with qualified names)
        AddExplicitInterfaceImplementations(inputType, outputType);

        // If no default synthesized interface was created but the class implements
        // user interfaces, mark the first interface implementation as [Default]
        if (declaration.DefaultInterface == null && outputType.Interfaces.Count > 0)
        {
            AddDefaultAttribute(outputType.Interfaces[0]);
        }
    }

    /// <summary>
    /// Adds explicit interface implementation methods from the input class.
    /// These are private methods with qualified names (e.g., "AuthoringTest.IDouble.GetDouble").
    /// </summary>
    private void AddExplicitInterfaceImplementations(TypeDefinition inputType, TypeDefinition outputType)
    {
        foreach (MethodDefinition method in inputType.Methods)
        {
            // Explicit implementations in compiled IL are private methods with dots in the name
            if (method.IsPublic || method.Name?.Value?.Contains('.') != true)
            {
                continue;
            }

            string fullMethodName = method.Name.Value;

            // Extract interface name and method name (e.g., "AuthoringTest.IDouble.GetDouble" -> "AuthoringTest.IDouble" + "GetDouble")
            int lastDot = fullMethodName.LastIndexOf('.');
            if (lastDot <= 0)
            {
                continue;
            }

            string interfaceQualName = fullMethodName[..lastDot];
            string shortMethodName = fullMethodName[(lastDot + 1)..];

            // Check if this interface is one we've processed (skip mapped/unmapped interfaces)
            if (!_typeDefinitionMapping.ContainsKey(interfaceQualName))
            {
                continue;
            }

            // Create the output method
            TypeSignature returnType = method.Signature!.ReturnType is CorLibTypeSignature { ElementType: ElementType.Void }
                ? _outputModule.CorLibTypeFactory.Void
                : MapTypeSignatureToOutput(method.Signature.ReturnType);

            TypeSignature[] parameterTypes = [.. method.Signature.ParameterTypes
                .Select(MapTypeSignatureToOutput)];

            MethodAttributes attrs =
                MethodAttributes.Private |
                MethodAttributes.Final |
                MethodAttributes.Virtual |
                MethodAttributes.HideBySig |
                MethodAttributes.NewSlot;

            if (method.IsSpecialName)
            {
                attrs |= MethodAttributes.SpecialName;
            }

            MethodDefinition outputMethod = new(
                fullMethodName,
                attrs,
                MethodSignature.CreateInstance(returnType, parameterTypes))
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
            };

            int paramIndex = 1;
            foreach (ParameterDefinition inputParam in method.ParameterDefinitions)
            {
                outputMethod.ParameterDefinitions.Add(new ParameterDefinition(
                    (ushort)paramIndex++,
                    inputParam.Name!.Value,
                    ParameterAttributes.In));
            }

            outputType.Methods.Add(outputMethod);

            // Also add properties/events for accessor methods
            if (shortMethodName.StartsWith("get_", StringComparison.Ordinal) || shortMethodName.StartsWith("put_", StringComparison.Ordinal))
            {
                string propName = $"{interfaceQualName}.{shortMethodName[4..]}";
                if (!outputType.Properties.Any(p => p.Name?.Value == propName))
                {
                    PropertyDefinition prop = new(propName, 0, PropertySignature.CreateInstance(
                        shortMethodName.StartsWith("get_", StringComparison.Ordinal) ? returnType : parameterTypes[0]));

                    if (shortMethodName.StartsWith("get_", StringComparison.Ordinal))
                    {
                        prop.Semantics.Add(new MethodSemantics(outputMethod, MethodSemanticsAttributes.Getter));
                    }
                    else
                    {
                        prop.Semantics.Add(new MethodSemantics(outputMethod, MethodSemanticsAttributes.Setter));
                    }

                    outputType.Properties.Add(prop);
                }
                else
                {
                    // Add setter to existing property
                    PropertyDefinition existing = outputType.Properties.First(p => p.Name?.Value == propName);
                    existing.Semantics.Add(new MethodSemantics(outputMethod, MethodSemanticsAttributes.Setter));
                }
            }
            else if (shortMethodName.StartsWith("add_", StringComparison.Ordinal))
            {
                string evtName = $"{interfaceQualName}.{shortMethodName[4..]}";
                // Find the event handler type from the parameter
                ITypeDefOrRef eventType = parameterTypes.Length > 0 && parameterTypes[0] is TypeDefOrRefSignature tdrs
                    ? tdrs.Type
                    : GetOrCreateTypeReference("Windows.Foundation", "EventHandler`1", "Windows.Foundation.FoundationContract");

                EventDefinition evt = new(evtName, 0, eventType);
                evt.Semantics.Add(new MethodSemantics(outputMethod, MethodSemanticsAttributes.AddOn));
                outputType.Events.Add(evt);
            }
            else if (shortMethodName.StartsWith("remove_", StringComparison.Ordinal))
            {
                string evtName = $"{interfaceQualName}.{shortMethodName[7..]}";
                EventDefinition? existingEvt = outputType.Events.FirstOrDefault(e => e.Name?.Value == evtName);
                existingEvt?.Semantics.Add(new MethodSemantics(outputMethod, MethodSemanticsAttributes.RemoveOn));
            }

            // Add MethodImpl pointing to the interface method
            TypeDeclaration interfaceDecl = _typeDefinitionMapping[interfaceQualName];
            if (interfaceDecl.OutputType != null)
            {
                MemberReference interfaceMethodRef = new(interfaceDecl.OutputType, shortMethodName,
                    MethodSignature.CreateInstance(returnType, parameterTypes));
                outputType.MethodImplementations.Add(new MethodImplementation(interfaceMethodRef, outputMethod));
            }
        }
    }
}