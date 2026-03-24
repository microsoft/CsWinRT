// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.WinMDGenerator.Discovery;
using WindowsRuntime.WinMDGenerator.Errors;
using WindowsRuntime.WinMDGenerator.Models;

using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodAttributes;
using MethodImplAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodImplAttributes;
using MethodSemanticsAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodSemanticsAttributes;
using ParameterAttributes = AsmResolver.PE.DotNet.Metadata.Tables.ParameterAttributes;
using FieldAttributes = AsmResolver.PE.DotNet.Metadata.Tables.FieldAttributes;
using TypeAttributes = AsmResolver.PE.DotNet.Metadata.Tables.TypeAttributes;
using AssemblyAttributes = AsmResolver.PE.DotNet.Metadata.Tables.AssemblyAttributes;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <summary>
/// Writes a WinMD file from analyzed assembly types using AsmResolver.
/// </summary>
internal sealed class WinmdWriter
{
    private readonly string _assemblyName;
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
        _assemblyName = assemblyName;
        _version = version;
        _mapper = mapper;
        _inputModule = inputModule;

        // Create the output WinMD module
        _outputModule = new ModuleDefinition(assemblyName + ".winmd");

        // Create the output assembly with WindowsRuntime flag (keep reference alive via module)
        _ = new AssemblyDefinition(assemblyName, new Version(version))
        {
            Modules = { _outputModule },
            Attributes = AssemblyAttributes.ContentWindowsRuntime // WindowsRuntime
        };

        // Add the <Module> type
        _outputModule.TopLevelTypes.Add(new TypeDefinition(null, "<Module>", 0));
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
        else if (AssemblyAnalyzer.IsDelegate(inputType))
        {
            AddDelegateType(inputType);
        }
        else if (inputType.IsInterface)
        {
            AddInterfaceType(inputType);
        }
        else if (inputType.IsValueType && !inputType.IsEnum)
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

            // Add MethodImpls for implemented interfaces
            foreach (InterfaceImplementation classInterfaceImpl in classOutputType.Interfaces)
            {
                if (classInterfaceImpl.Interface == null)
                {
                    continue;
                }

                TypeDefinition? interfaceDef = classInterfaceImpl.Interface.Resolve();
                if (interfaceDef == null)
                {
                    continue;
                }

                foreach (MethodDefinition interfaceMethod in interfaceDef.Methods)
                {
                    // Find the corresponding method on the class
                    MethodDefinition? classMethod = FindMatchingMethod(classOutputType, interfaceMethod);
                    if (classMethod != null)
                    {
                        MemberReference interfaceMethodRef = new(classInterfaceImpl.Interface, interfaceMethod.Name!.Value, interfaceMethod.Signature);
                        classOutputType.MethodImplementations.Add(new MethodImplementation(classMethod, interfaceMethodRef));
                    }
                }
            }

            // Add MethodImpls for default synthesized interface
            if (declaration.DefaultInterface != null &&
                _typeDefinitionMapping.TryGetValue(declaration.DefaultInterface, out TypeDeclaration? defaultInterfaceDecl) &&
                defaultInterfaceDecl.OutputType != null)
            {
                TypeDefinition defaultInterface = defaultInterfaceDecl.OutputType;
                ITypeDefOrRef interfaceRef = GetOrCreateTypeReference(
                    defaultInterface.Namespace?.Value ?? "",
                    defaultInterface.Name!.Value,
                    _assemblyName);

                foreach (MethodDefinition interfaceMethod in defaultInterface.Methods)
                {
                    MethodDefinition? classMethod = FindMatchingMethod(classOutputType, interfaceMethod);
                    if (classMethod != null)
                    {
                        MemberReference interfaceMethodRef = new(interfaceRef, interfaceMethod.Name!.Value, interfaceMethod.Signature);
                        classOutputType.MethodImplementations.Add(new MethodImplementation(classMethod, interfaceMethodRef));
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
                AddVersionAttribute(declaration.OutputType, defaultVersion);
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

    #region Enum Types

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

    #endregion

    #region Delegate Types

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

    #endregion

    #region Interface Types

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

    #endregion

    #region Struct Types

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

    #endregion

    #region Class Types

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

    #endregion

    #region Synthesized Interfaces

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

                // Add interface implementation on the class
                TypeReference interfaceRef = GetOrCreateTypeReference(ns, interfaceName, _assemblyName);
                InterfaceImplementation interfaceImpl = new(interfaceRef);
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

    #endregion

    #region Custom Mapped Interfaces

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

    #endregion

    #region Method/Property/Event Helpers

    private void AddMethodToInterface(TypeDefinition outputType, MethodDefinition inputMethod)
    {
        TypeSignature returnType = inputMethod.Signature!.ReturnType is CorLibTypeSignature { ElementType: ElementType.Void }
            ? _outputModule.CorLibTypeFactory.Void
            : MapTypeSignatureToOutput(inputMethod.Signature.ReturnType);

        TypeSignature[] parameterTypes = [.. inputMethod.Signature.ParameterTypes
            .Select(MapTypeSignatureToOutput)];

        MethodAttributes attrs =
            MethodAttributes.Public |
            MethodAttributes.HideBySig |
            MethodAttributes.Abstract |
            MethodAttributes.Virtual |
            MethodAttributes.NewSlot;

        if (inputMethod.IsSpecialName)
        {
            attrs |= MethodAttributes.SpecialName;
        }

        MethodDefinition outputMethod = new(
            inputMethod.Name!.Value,
            attrs,
            MethodSignature.CreateInstance(returnType, parameterTypes));

        // Add parameter definitions
        int paramIndex = 1;
        foreach (ParameterDefinition inputParam in inputMethod.ParameterDefinitions)
        {
            outputMethod.ParameterDefinitions.Add(new ParameterDefinition(
                (ushort)paramIndex++,
                inputParam.Name!.Value,
                inputParam.Attributes));
        }

        outputType.Methods.Add(outputMethod);

        // Copy custom attributes from the input method
        CopyCustomAttributes(inputMethod, outputMethod);
    }

    private void AddMethodToClass(TypeDefinition outputType, MethodDefinition inputMethod)
    {
        TypeSignature returnType = inputMethod.Signature!.ReturnType is CorLibTypeSignature { ElementType: ElementType.Void }
            ? _outputModule.CorLibTypeFactory.Void
            : MapTypeSignatureToOutput(inputMethod.Signature.ReturnType);

        TypeSignature[] parameterTypes = [.. inputMethod.Signature.ParameterTypes
            .Select(MapTypeSignatureToOutput)];

        bool isConstructor = inputMethod.IsConstructor;
        MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.HideBySig;

        if (isConstructor)
        {
            attrs |= MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName;
        }
        else if (inputMethod.IsStatic)
        {
            attrs |= MethodAttributes.Static;
        }
        else
        {
            attrs |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
        }

        if (inputMethod.IsSpecialName && !isConstructor)
        {
            attrs |= MethodAttributes.SpecialName;
        }

        MethodSignature signature = isConstructor || !inputMethod.IsStatic
            ? MethodSignature.CreateInstance(returnType, parameterTypes)
            : MethodSignature.CreateStatic(returnType, parameterTypes);

        MethodDefinition outputMethod = new(
            inputMethod.Name!.Value,
            attrs,
            signature)
        {
            ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
        };

        // Add parameter definitions
        int paramIndex = 1;
        foreach (ParameterDefinition inputParam in inputMethod.ParameterDefinitions)
        {
            outputMethod.ParameterDefinitions.Add(new ParameterDefinition(
                (ushort)paramIndex++,
                inputParam.Name!.Value,
                inputParam.Attributes));
        }

        outputType.Methods.Add(outputMethod);

        // Copy custom attributes from the input method
        CopyCustomAttributes(inputMethod, outputMethod);
    }

    private void AddPropertyToType(TypeDefinition outputType, PropertyDefinition inputProperty, bool isInterfaceParent)
    {
        TypeSignature propertyType = MapTypeSignatureToOutput(inputProperty.Signature!.ReturnType);

        PropertyDefinition outputProperty = new(
            inputProperty.Name!.Value,
            0,
            PropertySignature.CreateInstance(propertyType));

        bool isStatic = inputProperty.GetMethod?.IsStatic == true || inputProperty.SetMethod?.IsStatic == true;

        // Add getter
        if (inputProperty.GetMethod != null)
        {
            MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
            if (isInterfaceParent)
            {
                attrs |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            }
            else if (isStatic)
            {
                attrs |= MethodAttributes.Static;
            }
            else
            {
                attrs |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
            }

            MethodSignature getSignature = isStatic
                ? MethodSignature.CreateStatic(propertyType)
                : MethodSignature.CreateInstance(propertyType);

            MethodDefinition getter = new("get_" + inputProperty.Name.Value, attrs, getSignature);
            if (!isInterfaceParent)
            {
                getter.ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
            }
            outputType.Methods.Add(getter);
            outputProperty.Semantics.Add(new MethodSemantics(getter, MethodSemanticsAttributes.Getter));
        }

        // Add setter (WinRT uses "put_" prefix)
        if (inputProperty.SetMethod != null && inputProperty.SetMethod.IsPublic)
        {
            MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
            if (isInterfaceParent)
            {
                attrs |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            }
            else if (isStatic)
            {
                attrs |= MethodAttributes.Static;
            }
            else
            {
                attrs |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
            }

            MethodSignature setSignature = isStatic
                ? MethodSignature.CreateStatic(_outputModule.CorLibTypeFactory.Void, propertyType)
                : MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, propertyType);

            MethodDefinition setter = new("put_" + inputProperty.Name.Value, attrs, setSignature);
            if (!isInterfaceParent)
            {
                setter.ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
            }

            // Add parameter
            setter.ParameterDefinitions.Add(new ParameterDefinition(1, "value", ParameterAttributes.In));

            outputType.Methods.Add(setter);
            outputProperty.Semantics.Add(new MethodSemantics(setter, MethodSemanticsAttributes.Setter));
        }

        outputType.Properties.Add(outputProperty);

        // Copy custom attributes from the input property
        CopyCustomAttributes(inputProperty, outputProperty);
    }

    private void AddEventToType(TypeDefinition outputType, EventDefinition inputEvent, bool isInterfaceParent)
    {
        ITypeDefOrRef eventType = ImportTypeReference(inputEvent.EventType!);
        TypeReference eventRegistrationTokenType = GetOrCreateTypeReference(
            "Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract");

        EventDefinition outputEvent = new(inputEvent.Name!.Value, 0, eventType);

        bool isStatic = inputEvent.AddMethod?.IsStatic == true;

        // Add method
        {
            MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
            if (isInterfaceParent)
            {
                attrs |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            }
            else if (isStatic)
            {
                attrs |= MethodAttributes.Static;
            }
            else
            {
                attrs |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
            }

            TypeSignature handlerSig = eventType.ToTypeSignature();
            TypeSignature tokenSig = eventRegistrationTokenType.ToTypeSignature();

            MethodSignature addSignature = isStatic
                ? MethodSignature.CreateStatic(tokenSig, handlerSig)
                : MethodSignature.CreateInstance(tokenSig, handlerSig);

            MethodDefinition adder = new("add_" + inputEvent.Name.Value, attrs, addSignature);
            if (!isInterfaceParent)
            {
                adder.ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
            }
            adder.ParameterDefinitions.Add(new ParameterDefinition(1, "handler", ParameterAttributes.In));
            outputType.Methods.Add(adder);
            outputEvent.Semantics.Add(new MethodSemantics(adder, MethodSemanticsAttributes.AddOn));
        }

        // Remove method
        {
            MethodAttributes attrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
            if (isInterfaceParent)
            {
                attrs |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            }
            else if (isStatic)
            {
                attrs |= MethodAttributes.Static;
            }
            else
            {
                attrs |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
            }

            TypeSignature tokenSig = eventRegistrationTokenType.ToTypeSignature();

            MethodSignature removeSignature = isStatic
                ? MethodSignature.CreateStatic(_outputModule.CorLibTypeFactory.Void, tokenSig)
                : MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, tokenSig);

            MethodDefinition remover = new("remove_" + inputEvent.Name.Value, attrs, removeSignature);
            if (!isInterfaceParent)
            {
                remover.ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
            }
            remover.ParameterDefinitions.Add(new ParameterDefinition(1, "token", ParameterAttributes.In));
            outputType.Methods.Add(remover);
            outputEvent.Semantics.Add(new MethodSemantics(remover, MethodSemanticsAttributes.RemoveOn));
        }

        outputType.Events.Add(outputEvent);

        // Copy custom attributes from the input event
        CopyCustomAttributes(inputEvent, outputEvent);
    }

    #endregion

    #region Type Mapping

    /// <summary>
    /// Maps a type signature from the input module to the output module.
    /// </summary>
    private TypeSignature MapTypeSignatureToOutput(TypeSignature inputSig)
    {
        // Handle CorLib types
        if (inputSig is CorLibTypeSignature corLib)
        {
#pragma warning disable IDE0072 // Switch already has default case handling all other element types
            return corLib.ElementType switch
            {
                ElementType.Boolean => _outputModule.CorLibTypeFactory.Boolean,
                ElementType.Char => _outputModule.CorLibTypeFactory.Char,
                ElementType.I1 => _outputModule.CorLibTypeFactory.SByte,
                ElementType.U1 => _outputModule.CorLibTypeFactory.Byte,
                ElementType.I2 => _outputModule.CorLibTypeFactory.Int16,
                ElementType.U2 => _outputModule.CorLibTypeFactory.UInt16,
                ElementType.I4 => _outputModule.CorLibTypeFactory.Int32,
                ElementType.U4 => _outputModule.CorLibTypeFactory.UInt32,
                ElementType.I8 => _outputModule.CorLibTypeFactory.Int64,
                ElementType.U8 => _outputModule.CorLibTypeFactory.UInt64,
                ElementType.R4 => _outputModule.CorLibTypeFactory.Single,
                ElementType.R8 => _outputModule.CorLibTypeFactory.Double,
                ElementType.String => _outputModule.CorLibTypeFactory.String,
                ElementType.Object => _outputModule.CorLibTypeFactory.Object,
                ElementType.I => _outputModule.CorLibTypeFactory.IntPtr,
                ElementType.U => _outputModule.CorLibTypeFactory.UIntPtr,
                ElementType.Void => _outputModule.CorLibTypeFactory.Void,
                _ => _outputModule.CorLibTypeFactory.Object
            };
#pragma warning restore IDE0072
        }

        // Handle SZArray (single-dimensional zero-based arrays)
        if (inputSig is SzArrayTypeSignature szArray)
        {
            return new SzArrayTypeSignature(MapTypeSignatureToOutput(szArray.BaseType));
        }

        // Handle generic instance types
        if (inputSig is GenericInstanceTypeSignature genericInst)
        {
            ITypeDefOrRef importedType = ImportTypeReference(genericInst.GenericType);
            TypeSignature[] mappedArgs = [.. genericInst.TypeArguments
                .Select(MapTypeSignatureToOutput)];
            return new GenericInstanceTypeSignature(importedType, genericInst.IsValueType, mappedArgs);
        }

        // Handle generic method/type parameters
        if (inputSig is GenericParameterSignature genericParam)
        {
            return new GenericParameterSignature(_outputModule, genericParam.ParameterType, genericParam.Index);
        }

        // Handle ByRef
        if (inputSig is ByReferenceTypeSignature byRef)
        {
            return new ByReferenceTypeSignature(MapTypeSignatureToOutput(byRef.BaseType));
        }

        // Handle TypeDefOrRefSignature
        if (inputSig is TypeDefOrRefSignature typeDefOrRef)
        {
            ITypeDefOrRef importedRef = ImportTypeReference(typeDefOrRef.Type);
            return new TypeDefOrRefSignature(importedRef, typeDefOrRef.IsValueType);
        }

        // Fallback: import the type
        return _outputModule.CorLibTypeFactory.Object;
    }

    /// <summary>
    /// Imports a type reference from the input module to the output module.
    /// </summary>
    private ITypeDefOrRef ImportTypeReference(ITypeDefOrRef type)
    {
        if (type is TypeDefinition typeDef)
        {
            // Check if we've already processed this type into the output module
            string qualifiedName = AssemblyAnalyzer.GetQualifiedName(typeDef);
            if (_typeDefinitionMapping.TryGetValue(qualifiedName, out TypeDeclaration? declaration) && declaration.OutputType != null)
            {
                return declaration.OutputType;
            }

            // Otherwise create a type reference
            return GetOrCreateTypeReference(
                typeDef.Namespace?.Value ?? "",
                typeDef.Name!.Value,
                _inputModule.Assembly?.Name?.Value ?? "mscorlib");
        }

        if (type is TypeReference typeRef)
        {
            string ns = typeRef.Namespace?.Value ?? "";
            string name = typeRef.Name!.Value;
            string assembly = GetAssemblyNameFromScope(typeRef.Scope);
            return GetOrCreateTypeReference(ns, name, assembly);
        }

        if (type is TypeSpecification typeSpec)
        {
            // For type specs, we need to create a new TypeSpecification in the output
            TypeSignature mappedSig = MapTypeSignatureToOutput(typeSpec.Signature!);
            return new TypeSpecification(mappedSig);
        }

        return GetOrCreateTypeReference("System", "Object", "mscorlib");
    }

    private static string GetAssemblyNameFromScope(IResolutionScope? scope)
    {
        return scope switch
        {
            AssemblyReference asmRef => asmRef.Name?.Value ?? "mscorlib",
            ModuleDefinition mod => mod.Assembly?.Name?.Value ?? "mscorlib",
            _ => "mscorlib"
        };
    }

    #endregion

    #region Type References

    private TypeReference GetOrCreateTypeReference(string @namespace, string name, string assemblyName)
    {
        string fullName = string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";

        if (_typeReferenceCache.TryGetValue(fullName, out TypeReference? cached))
        {
            return cached;
        }

        AssemblyReference assemblyRef = GetOrCreateAssemblyReference(assemblyName);
        TypeReference typeRef = new(_outputModule, assemblyRef, @namespace, name);
        _typeReferenceCache[fullName] = typeRef;
        return typeRef;
    }

    private AssemblyReference GetOrCreateAssemblyReference(string assemblyName)
    {
        if (_assemblyReferenceCache.TryGetValue(assemblyName, out AssemblyReference? cached))
        {
            return cached;
        }

        AssemblyAttributes flags = string.CompareOrdinal(assemblyName, "mscorlib") == 0
            ? 0
            : AssemblyAttributes.ContentWindowsRuntime;

        AssemblyReference asmRef = new(assemblyName, new Version(0xFF, 0xFF, 0xFF, 0xFF))
        {
            Attributes = flags,
        };

        if (string.CompareOrdinal(assemblyName, "mscorlib") == 0)
        {
            asmRef.PublicKeyOrToken = [0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89];
        }

        _outputModule.AssemblyReferences.Add(asmRef);
        _assemblyReferenceCache[assemblyName] = asmRef;
        return asmRef;
    }

    #endregion

    #region Attributes

    private void AddGuidAttribute(TypeDefinition outputType, TypeDefinition inputType)
    {
        // Check if the input type has a GuidAttribute
        foreach (CustomAttribute attr in inputType.CustomAttributes)
        {
            if (attr.Constructor?.DeclaringType?.FullName == "System.Runtime.InteropServices.GuidAttribute" &&
                attr.Signature?.FixedArguments.Count > 0 &&
                attr.Signature.FixedArguments[0].Element is string guidString &&
                Guid.TryParse(guidString, out Guid guid))
            {
                AddGuidAttribute(outputType, guid);
                return;
            }
        }

        // Generate a GUID from the type name using SHA1
        string typeName = AssemblyAnalyzer.GetQualifiedName(inputType);
        AddGuidAttributeFromName(outputType, typeName);
    }

    private void AddGuidAttributeFromName(TypeDefinition outputType, string name)
    {
        Guid guid;
        // CodeQL [SM02196] WinRT uses UUID v5 SHA1 to generate Guids for parameterized types.
#pragma warning disable CA5350
        using (SHA1 sha = SHA1.Create())
        {
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(name));
            guid = EncodeGuid(hash);
        }
#pragma warning restore CA5350

        AddGuidAttribute(outputType, guid);
    }

    private void AddGuidAttribute(TypeDefinition outputType, Guid guid)
    {
        // Create a reference to the GuidAttribute constructor in Windows.Foundation.Metadata
        TypeReference guidAttrType = GetOrCreateTypeReference(
            "Windows.Foundation.Metadata", "GuidAttribute", "Windows.Foundation.FoundationContract");

        byte[] guidBytes = guid.ToByteArray();

        // The GuidAttribute constructor takes (uint, ushort, ushort, byte, byte, byte, byte, byte, byte, byte, byte)
        MemberReference guidCtor = new(guidAttrType, ".ctor",
            MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                _outputModule.CorLibTypeFactory.UInt32,
                _outputModule.CorLibTypeFactory.UInt16,
                _outputModule.CorLibTypeFactory.UInt16,
                _outputModule.CorLibTypeFactory.Byte,
                _outputModule.CorLibTypeFactory.Byte,
                _outputModule.CorLibTypeFactory.Byte,
                _outputModule.CorLibTypeFactory.Byte,
                _outputModule.CorLibTypeFactory.Byte,
                _outputModule.CorLibTypeFactory.Byte,
                _outputModule.CorLibTypeFactory.Byte,
                _outputModule.CorLibTypeFactory.Byte));

        CustomAttributeSignature sig = new();
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, BitConverter.ToUInt32(guidBytes, 0)));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(guidBytes, 4)));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(guidBytes, 6)));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[8]));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[9]));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[10]));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[11]));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[12]));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[13]));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[14]));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[15]));

        outputType.CustomAttributes.Add(new CustomAttribute(guidCtor, sig));
    }

    private void AddVersionAttribute(TypeDefinition outputType, int version)
    {
        TypeReference versionAttrType = GetOrCreateTypeReference(
            "Windows.Foundation.Metadata", "VersionAttribute", "Windows.Foundation.FoundationContract");

        MemberReference versionCtor = new(versionAttrType, ".ctor",
            MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                _outputModule.CorLibTypeFactory.UInt32));

        CustomAttributeSignature sig = new();
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, (uint)version));

        outputType.CustomAttributes.Add(new CustomAttribute(versionCtor, sig));
    }

    private void AddActivatableAttribute(TypeDefinition outputType, uint version, string? factoryInterface)
    {
        TypeReference activatableAttrType = GetOrCreateTypeReference(
            "Windows.Foundation.Metadata", "ActivatableAttribute", "Windows.Foundation.FoundationContract");

        if (factoryInterface != null)
        {
            // Constructor: ActivatableAttribute(Type, uint)
            TypeReference systemType = GetOrCreateTypeReference("System", "Type", "mscorlib");
            MemberReference ctor = new(activatableAttrType, ".ctor",
                MethodSignature.CreateInstance(
                    _outputModule.CorLibTypeFactory.Void,
                    systemType.ToTypeSignature(),
                    _outputModule.CorLibTypeFactory.UInt32));

            CustomAttributeSignature sig = new();
            sig.FixedArguments.Add(new CustomAttributeArgument(systemType.ToTypeSignature(), factoryInterface));
            sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, version));

            outputType.CustomAttributes.Add(new CustomAttribute(ctor, sig));
        }
        else
        {
            // Constructor: ActivatableAttribute(uint)
            MemberReference ctor = new(activatableAttrType, ".ctor",
                MethodSignature.CreateInstance(
                    _outputModule.CorLibTypeFactory.Void,
                    _outputModule.CorLibTypeFactory.UInt32));

            CustomAttributeSignature sig = new();
            sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, version));

            outputType.CustomAttributes.Add(new CustomAttribute(ctor, sig));
        }
    }

    private void AddStaticAttribute(TypeDefinition classOutputType, uint version, string staticInterfaceName)
    {
        TypeReference staticAttrType = GetOrCreateTypeReference(
            "Windows.Foundation.Metadata", "StaticAttribute", "Windows.Foundation.FoundationContract");

        TypeReference systemType = GetOrCreateTypeReference("System", "Type", "mscorlib");
        MemberReference ctor = new(staticAttrType, ".ctor",
            MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                systemType.ToTypeSignature(),
                _outputModule.CorLibTypeFactory.UInt32));

        CustomAttributeSignature sig = new();
        sig.FixedArguments.Add(new CustomAttributeArgument(systemType.ToTypeSignature(), staticInterfaceName));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, version));

        classOutputType.CustomAttributes.Add(new CustomAttribute(ctor, sig));
    }

    private void AddExclusiveToAttribute(TypeDefinition interfaceType, string className)
    {
        TypeReference exclusiveToAttrType = GetOrCreateTypeReference(
            "Windows.Foundation.Metadata", "ExclusiveToAttribute", "Windows.Foundation.FoundationContract");

        TypeReference systemType = GetOrCreateTypeReference("System", "Type", "mscorlib");
        MemberReference ctor = new(exclusiveToAttrType, ".ctor",
            MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                systemType.ToTypeSignature()));

        CustomAttributeSignature sig = new();
        sig.FixedArguments.Add(new CustomAttributeArgument(systemType.ToTypeSignature(), className));

        interfaceType.CustomAttributes.Add(new CustomAttribute(ctor, sig));
    }

    private void AddDefaultAttribute(InterfaceImplementation interfaceImpl)
    {
        TypeReference defaultAttrType = GetOrCreateTypeReference(
            "Windows.Foundation.Metadata", "DefaultAttribute", "Windows.Foundation.FoundationContract");

        MemberReference ctor = new(defaultAttrType, ".ctor",
            MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void));

        interfaceImpl.CustomAttributes.Add(new CustomAttribute(ctor, new CustomAttributeSignature()));
    }

    private static bool HasVersionAttribute(TypeDefinition type)
    {
        return type.CustomAttributes.Any(
            attr => attr.Constructor?.DeclaringType?.Name?.Value == "VersionAttribute");
    }

    private int GetVersion(TypeDefinition type)
    {
        foreach (CustomAttribute attr in type.CustomAttributes)
        {
            if (attr.Constructor?.DeclaringType?.Name?.Value == "VersionAttribute" &&
                attr.Signature?.FixedArguments.Count > 0 &&
                attr.Signature.FixedArguments[0].Element is uint version)
            {
                return (int)version;
            }
        }

        return Version.Parse(_version).Major;
    }

    /// <summary>
    /// Copies custom attributes from a source metadata element to a target metadata element,
    /// filtering out attributes that are handled separately by the generator or not meaningful for WinMD.
    /// </summary>
    private void CopyCustomAttributes(IHasCustomAttribute source, IHasCustomAttribute target)
    {
        foreach (CustomAttribute attr in source.CustomAttributes)
        {
            if (!ShouldCopyAttribute(attr))
            {
                continue;
            }

            MemberReference? importedCtor = ImportAttributeConstructor(attr.Constructor);
            if (importedCtor == null)
            {
                continue;
            }

            CustomAttributeSignature clonedSig = CloneAttributeSignature(attr.Signature);
            target.CustomAttributes.Add(new CustomAttribute(importedCtor, clonedSig));
        }
    }

    /// <summary>
    /// Determines whether a custom attribute should be copied to the output WinMD.
    /// </summary>
    private static bool ShouldCopyAttribute(CustomAttribute attr)
    {
        string? attrTypeName = attr.Constructor?.DeclaringType?.FullName;

        if (attrTypeName is null)
        {
            return false;
        }

        // Skip attributes already handled separately by the generator
        if (attrTypeName is
            "System.Runtime.InteropServices.GuidAttribute" or
            "WinRT.GeneratedBindableCustomPropertyAttribute" or
            "Windows.Foundation.Metadata.VersionAttribute")
        {
            return false;
        }

        // Skip compiler-generated attributes not meaningful for WinMD
        if (attrTypeName.StartsWith("System.Runtime.CompilerServices.", StringComparison.Ordinal))
        {
            return false;
        }

        // Skip non-public attribute types (if resolvable)
        TypeDefinition? attrTypeDef = attr.Constructor?.DeclaringType?.Resolve();
        if (attrTypeDef != null && !attrTypeDef.IsPublic && !attrTypeDef.IsNestedPublic)
        {
            return false;
        }

        // Skip attributes with unreadable signatures
        return attr.Signature != null;
    }

    /// <summary>
    /// Imports an attribute constructor reference into the output module.
    /// </summary>
    private MemberReference? ImportAttributeConstructor(ICustomAttributeType? ctor)
    {
        if (ctor?.DeclaringType == null || ctor.Signature is not MethodSignature methodSig)
        {
            return null;
        }

        ITypeDefOrRef importedType = ImportTypeReference(ctor.DeclaringType);

        TypeSignature[] mappedParams = [.. methodSig.ParameterTypes
            .Select(MapTypeSignatureToOutput)];

        MethodSignature importedSig = MethodSignature.CreateInstance(
            _outputModule.CorLibTypeFactory.Void,
            mappedParams);

        return new MemberReference(importedType, ".ctor", importedSig);
    }

    /// <summary>
    /// Clones a custom attribute signature, remapping type references to the output module.
    /// </summary>
    private CustomAttributeSignature CloneAttributeSignature(CustomAttributeSignature? inputSig)
    {
        if (inputSig == null)
        {
            return new CustomAttributeSignature();
        }

        CustomAttributeSignature outputSig = new();

        foreach (CustomAttributeArgument arg in inputSig.FixedArguments)
        {
            outputSig.FixedArguments.Add(CloneAttributeArgument(arg));
        }

        foreach (CustomAttributeNamedArgument namedArg in inputSig.NamedArguments)
        {
            TypeSignature mappedArgType = MapTypeSignatureToOutput(namedArg.Argument.ArgumentType);
            CustomAttributeArgument clonedInnerArg = CloneAttributeArgument(namedArg.Argument);

            outputSig.NamedArguments.Add(new CustomAttributeNamedArgument(
                namedArg.MemberType,
                namedArg.MemberName,
                mappedArgType,
                clonedInnerArg));
        }

        return outputSig;
    }

    /// <summary>
    /// Clones a single custom attribute argument, remapping type references.
    /// </summary>
    private CustomAttributeArgument CloneAttributeArgument(CustomAttributeArgument arg)
    {
        TypeSignature mappedType = MapTypeSignatureToOutput(arg.ArgumentType);
        CustomAttributeArgument clonedArg = new(mappedType);

        if (arg.IsNullArray)
        {
            clonedArg.IsNullArray = true;
        }
        else
        {
            foreach (object? element in arg.Elements)
            {
                // Type-valued elements are stored as TypeSignature and need remapping
                clonedArg.Elements.Add(element is TypeSignature typeSig
                    ? MapTypeSignatureToOutput(typeSig)
                    : element);
            }
        }

        return clonedArg;
    }

    #endregion

    #region Helpers

    private static bool IsPubliclyAccessible(ITypeDefOrRef type)
    {
        if (type is TypeDefinition typeDef)
        {
            return typeDef.IsPublic || typeDef.IsNestedPublic;
        }

        // For type references and specs, assume accessible
        return true;
    }

    /// <summary>
    /// Encodes a GUID from a SHA1 hash (UUID v5 format).
    /// </summary>
    private static Guid EncodeGuid(byte[] hash)
    {
        byte[] guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);

        // Set version to 5 (SHA1)
        guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x50);
        // Set variant to RFC 4122
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        return new Guid(guidBytes);
    }

    #endregion
}

/// <summary>
/// Tracks the input and output type definitions for a given type.
/// </summary>
internal sealed class TypeDeclaration
{
    /// <summary>The source type from the input assembly.</summary>
    public TypeDefinition? InputType { get; }

    /// <summary>The generated type in the output WinMD.</summary>
    public TypeDefinition? OutputType { get; }

    /// <summary>Whether this type is a component type (authored by the user).</summary>
    public bool IsComponentType { get; }

    /// <summary>The name of the default synthesized interface.</summary>
    public string? DefaultInterface { get; set; }

    /// <summary>The name of the static synthesized interface.</summary>
    public string? StaticInterface { get; set; }

    public TypeDeclaration(TypeDefinition? inputType, TypeDefinition? outputType, bool isComponentType = false)
    {
        InputType = inputType;
        OutputType = outputType;
        IsComponentType = isComponentType;
    }
}
