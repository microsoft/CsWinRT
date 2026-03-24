// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.WinMDGenerator.Discovery;
using WindowsRuntime.WinMDGenerator.Models;
using FieldAttributes = AsmResolver.PE.DotNet.Metadata.Tables.FieldAttributes;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodAttributes;
using MethodImplAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodImplAttributes;
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
                new FieldSignature(underlyingType));

            if (field.Constant != null)
            {
                outputField.Constant = new Constant(field.Constant.Type, new DataBlobSignature(field.Constant.Value!.Data));
            }

            outputType.Fields.Add(outputField);
        }

        _outputModule.TopLevelTypes.Add(outputType);

        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;
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

        // Add .ctor(object, IntPtr)
        MethodDefinition ctor = new(
            ".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
            MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                _outputModule.CorLibTypeFactory.Object,
                _outputModule.CorLibTypeFactory.IntPtr))
        {
            ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
        };
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

            // Add parameter names
            int paramIndex = 1;
            foreach (ParameterDefinition inputParam in inputInvoke.ParameterDefinitions)
            {
                invoke.ParameterDefinitions.Add(new ParameterDefinition(
                    (ushort)paramIndex++,
                    inputParam.Name!.Value,
                    inputParam.Attributes));
            }

            outputType.Methods.Add(invoke);
        }

        _outputModule.TopLevelTypes.Add(outputType);

        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;

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

        // Add methods
        foreach (MethodDefinition method in inputType.Methods)
        {
            if (!method.IsPublic)
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

        _outputModule.TopLevelTypes.Add(outputType);

        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;

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

        _outputModule.TopLevelTypes.Add(outputType);

        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;
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

        bool hasConstructor = false;
        bool hasDefaultConstructor = false;
        bool hasAtLeastOneNonPublicConstructor = false;
        bool isStaticClass = inputType.IsAbstract && inputType.IsSealed;

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
                AddMethodToClass(outputType, method);
            }
        }

        // Add properties
        foreach (PropertyDefinition property in inputType.Properties)
        {
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

        _outputModule.TopLevelTypes.Add(outputType);

        TypeDeclaration declaration = new(inputType, outputType, isComponentType: true);
        _typeDefinitionMapping[qualifiedName] = declaration;

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
    }
}