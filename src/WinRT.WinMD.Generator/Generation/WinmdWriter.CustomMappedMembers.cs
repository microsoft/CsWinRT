// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.WinMDGenerator.Discovery;
using WindowsRuntime.WinMDGenerator.Models;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodAttributes;
using MethodImplAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodImplAttributes;
using MethodSemanticsAttributes = AsmResolver.PE.DotNet.Metadata.Tables.MethodSemanticsAttributes;
using ParameterAttributes = AsmResolver.PE.DotNet.Metadata.Tables.ParameterAttributes;

namespace WindowsRuntime.WinMDGenerator.Generation;

internal sealed partial class WinmdWriter
{
    /// <summary>
    /// Processes custom mapped interfaces for a class type. This maps .NET collection interfaces,
    /// IDisposable, INotifyPropertyChanged, etc. to their WinRT equivalents and adds the
    /// required explicit implementation methods and MethodImpl records.
    /// </summary>
    private void ProcessCustomMappedInterfaces(TypeDefinition inputType, TypeDefinition outputType)
    {
        // Gather all interfaces from the type and its base types (matching old generator's GetInterfaces)
        List<InterfaceImplementation> allInterfaces = GatherAllInterfaces(inputType);

        // Collect all mapped interfaces and determine if they are publicly or explicitly implemented
        List<(InterfaceImplementation impl, string interfaceName, MappedType mapping, bool isPublic)> mappedInterfaces = [];

        foreach (InterfaceImplementation impl in allInterfaces)
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

            // Determine if the interface is publicly implemented.
            // Check if the class has public methods that match the .NET interface members.
            // For mapped interfaces, the .NET method names differ from WinRT names
            // (e.g., Add vs Append), so we check the .NET interface's members.
            bool isPublic = IsInterfacePubliclyImplemented(classType: inputType, impl, _runtimeContext);

            mappedInterfaces.Add((impl, interfaceName, mapping, isPublic));
        }

        // If generic IEnumerable<T> (IIterable) is present, skip non-generic IEnumerable (IBindableIterable)
        bool hasGenericEnumerable = allInterfaces.Any(i =>
            i.Interface != null && GetInterfaceQualifiedName(i.Interface) == "System.Collections.Generic.IEnumerable`1");

        foreach ((InterfaceImplementation impl, string interfaceName, MappedType mapping, bool isPublic) in mappedInterfaces)
        {
            (string mappedNs, string mappedName, string mappedAssembly, _, _) = mapping.GetMapping();

            // Skip non-generic IEnumerable when generic IEnumerable<T> is also implemented
            if (hasGenericEnumerable && interfaceName == "System.Collections.IEnumerable")
            {
                continue;
            }

            // Add the mapped interface as an implementation on the output type
            TypeReference mappedInterfaceRef = GetOrCreateTypeReference(mappedNs, mappedName, mappedAssembly);

            // Check if the output type already implements this mapped interface
            // (e.g., IObservableVector<T> already brings IVector<T>)
            string mappedFullName = string.IsNullOrEmpty(mappedNs) ? mappedName : $"{mappedNs}.{mappedName}";
            bool alreadyImplemented = outputType.Interfaces.Any(i =>
            {
                string? existingName = i.Interface is TypeSpecification existingTs && existingTs.Signature is GenericInstanceTypeSignature existingGits
                    ? existingGits.GenericType.FullName
                    : i.Interface?.FullName;
                return existingName == mappedFullName;
            });

            if (alreadyImplemented)
            {
                continue;
            }

            ITypeDefOrRef mappedInterfaceTypeRef;

            // For generic interfaces, handle type arguments (mapping KeyValuePair -> IKeyValuePair, etc.)
            if (impl.Interface is TypeSpecification typeSpec && typeSpec.Signature is GenericInstanceTypeSignature genericInst)
            {
                TypeSignature[] mappedArgs = [.. genericInst.TypeArguments
                    .Select(MapCustomMappedTypeArgument)];
                TypeSpecification mappedSpec = new(new GenericInstanceTypeSignature(mappedInterfaceRef, false, mappedArgs));
                outputType.Interfaces.Add(new InterfaceImplementation(mappedSpec));
                mappedInterfaceTypeRef = mappedSpec;
            }
            else
            {
                outputType.Interfaces.Add(new InterfaceImplementation(mappedInterfaceRef));
                mappedInterfaceTypeRef = mappedInterfaceRef;
            }

            // Add explicit implementation methods for the mapped interface
            AddCustomMappedTypeMembers(outputType, mappedName, mappedInterfaceTypeRef, isPublic);
        }
    }

    /// <summary>
    /// Maps a type argument for custom mapped interfaces, transforming mapped types
    /// like KeyValuePair to IKeyValuePair.
    /// </summary>
    private TypeSignature MapCustomMappedTypeArgument(TypeSignature arg)
    {
        TypeSignature mapped = MapTypeSignatureToOutput(arg);

        // Check if the mapped type itself has a WinRT mapping (e.g. KeyValuePair -> IKeyValuePair)
        if (mapped is TypeDefOrRefSignature tdrs)
        {
            string typeName = AssemblyAnalyzer.GetQualifiedName(tdrs.Type);
            if (_mapper.HasMappingForType(typeName))
            {
                MappedType innerMapping = _mapper.GetMappedType(typeName);
                (string ns, string name, string asm, _, _) = innerMapping.GetMapping();
                TypeReference innerRef = GetOrCreateTypeReference(ns, name, asm);
                return innerRef.ToTypeSignature(tdrs.IsValueType);
            }
        }

        // For generic instances, recursively map type arguments
        if (mapped is GenericInstanceTypeSignature gits)
        {
            string typeName = AssemblyAnalyzer.GetQualifiedName(gits.GenericType);
            if (_mapper.HasMappingForType(typeName))
            {
                MappedType innerMapping = _mapper.GetMappedType(typeName);
                (string ns, string name, string asm, _, _) = innerMapping.GetMapping();
                TypeReference innerRef = GetOrCreateTypeReference(ns, name, asm);
                TypeSignature[] innerArgs = [.. gits.TypeArguments.Select(MapCustomMappedTypeArgument)];
                return new GenericInstanceTypeSignature(innerRef, false, innerArgs);
            }
        }

        return mapped;
    }

    /// <summary>
    /// Adds the explicit implementation methods for a specific mapped WinRT interface.
    /// </summary>
    private void AddCustomMappedTypeMembers(
        TypeDefinition outputType,
        string mappedTypeName,
        ITypeDefOrRef mappedInterfaceRef,
        bool isPublic)
    {
        // Build the qualified prefix for explicit implementation method names.
        // For generic types, use short type names (e.g. "IMap`2<String, Int32>" not "IMap`2<System.String, System.Int32>")
        string qualifiedPrefix = FormatQualifiedInterfaceName(mappedInterfaceRef);

        // Store parent interface generic type arguments for MethodImpl signature conversion.
        // MethodImpl declarations on generic interfaces should reference methods using !0, !1 etc. (not resolved types).
        TypeSignature[]? parentGenericArgs = null;
        if (mappedInterfaceRef is TypeSpecification mts2 && mts2.Signature is GenericInstanceTypeSignature mgits)
        {
            parentGenericArgs = [.. mgits.TypeArguments];
        }

        // Look up a type signature in the parent interface's generic arguments by identity.
        // Returns the corresponding GenericParameterSignature (!0, !1) or null if not found.
        GenericParameterSignature? FindParentGenericParam(TypeSignature sig)
        {
            if (parentGenericArgs == null)
            {
                return null;
            }

            string sigFullName = sig.FullName;
            for (int i = 0; i < parentGenericArgs.Length; i++)
            {
                if (parentGenericArgs[i].FullName == sigFullName)
                {
                    return new GenericParameterSignature(_outputModule, GenericParameterType.Type, i);
                }
            }

            return null;
        }

        // Convert a resolved type signature to use generic parameters (!0, !1) for MethodImpl declarations.
        // For parent interface generic args, substitutes resolved types back to !0, !1.
        // For all GenericInstanceTypeSignature in signatures, converts to open form
        // (e.g., EventHandler`1<Object> -> EventHandler`1<!0>) matching WinRT metadata conventions.
        TypeSignature ToGenericParam(TypeSignature sig)
        {
            if (parentGenericArgs != null)
            {
                GenericParameterSignature? gps = FindParentGenericParam(sig);
                if (gps != null)
                {
                    return gps;
                }
            }

            return sig switch
            {
                GenericInstanceTypeSignature innerGits => ToOpenGenericForm(innerGits),
                SzArrayTypeSignature szArray => new SzArrayTypeSignature(ToGenericParam(szArray.BaseType)),
                ByReferenceTypeSignature byRef => new ByReferenceTypeSignature(ToGenericParam(byRef.BaseType)),
                _ => sig
            };
        }

        // Convert a GenericInstanceTypeSignature to its open form for MethodImpl declarations.
        // E.g., KeyValuePair<String, Int32> -> KeyValuePair<!0, !1> when those are parent interface args.
        GenericInstanceTypeSignature ToOpenGenericForm(GenericInstanceTypeSignature gits)
        {
            TypeSignature[] openArgs = new TypeSignature[gits.TypeArguments.Count];
            for (int i = 0; i < gits.TypeArguments.Count; i++)
            {
                TypeSignature arg = gits.TypeArguments[i];
                // Try parent interface generic arg substitution (first match wins for duplicate args)
                GenericParameterSignature? parentGps = FindParentGenericParam(arg);
                if (parentGps != null)
                {
                    openArgs[i] = parentGps;
                }
                else if (arg is GenericInstanceTypeSignature nestedGits)
                {
                    openArgs[i] = ToOpenGenericForm(nestedGits);
                }
                else
                {
                    // Use the generic type's own parameter
                    openArgs[i] = new GenericParameterSignature(_outputModule, GenericParameterType.Type, i);
                }
            }

            return new GenericInstanceTypeSignature(gits.GenericType, gits.IsValueType, openArgs);
        }

        void AddMappedMethod(string name, (string name, TypeSignature type, ParameterAttributes attrs)[]? parameters, TypeSignature? returnType)
        {
            string methodName = isPublic ? name : $"{qualifiedPrefix}.{name}";

            MethodAttributes attrs = isPublic
                ? (MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot)
                : (MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot);

            TypeSignature[] paramTypes = parameters?.Select(p => p.type).ToArray() ?? [];
            MethodSignature sig = MethodSignature.CreateInstance(returnType ?? _outputModule.CorLibTypeFactory.Void, paramTypes);

            MethodDefinition method = new(methodName, attrs, sig)
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
            };

            if (parameters != null)
            {
                int idx = 1;
                foreach ((string name, TypeSignature type, ParameterAttributes attrs) p in parameters)
                {
                    method.ParameterDefinitions.Add(new ParameterDefinition((ushort)idx++, p.name, p.attrs));
                }
            }

            outputType.Methods.Add(method);

            // Add MethodImpl pointing to the mapped interface method (use generic params !0, !1 for declaration signature)
            TypeSignature[] implParamTypes = parameters?.Select(p => ToGenericParam(p.type)).ToArray() ?? [];
            TypeSignature implReturnType = ToGenericParam(returnType ?? _outputModule.CorLibTypeFactory.Void);
            MemberReference interfaceMethodRef = new(mappedInterfaceRef, name, MethodSignature.CreateInstance(implReturnType, implParamTypes));
            outputType.MethodImplementations.Add(new MethodImplementation(interfaceMethodRef, method));
        }

        void AddMappedProperty(string name, TypeSignature propertyType, bool hasSetter)
        {
            string propName = isPublic ? name : $"{qualifiedPrefix}.{name}";
            string getMethodName = isPublic ? $"get_{name}" : $"{qualifiedPrefix}.get_{name}";

            // Getter
            MethodAttributes getAttrs = isPublic
                ? (MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName)
                : (MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName);

            MethodDefinition getter = new(getMethodName, getAttrs, MethodSignature.CreateInstance(propertyType))
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
            };
            outputType.Methods.Add(getter);

            // Property
            PropertyDefinition prop = new(propName, 0, PropertySignature.CreateInstance(propertyType));
            prop.Semantics.Add(new MethodSemantics(getter, MethodSemanticsAttributes.Getter));

            // MethodImpl for getter (use generic params for declaration signature)
            TypeSignature implPropertyType = ToGenericParam(propertyType);
            MemberReference getterRef = new(mappedInterfaceRef, $"get_{name}", MethodSignature.CreateInstance(implPropertyType, []));
            outputType.MethodImplementations.Add(new MethodImplementation(getterRef, getter));

            if (hasSetter)
            {
                string putMethodName = isPublic ? $"put_{name}" : $"{qualifiedPrefix}.put_{name}";
                MethodDefinition setter = new(putMethodName, getAttrs, MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [propertyType]))
                {
                    ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
                };
                setter.ParameterDefinitions.Add(new ParameterDefinition(1, "value", ParameterAttributes.In));
                outputType.Methods.Add(setter);
                prop.Semantics.Add(new MethodSemantics(setter, MethodSemanticsAttributes.Setter));

                MemberReference setterRef = new(mappedInterfaceRef, $"put_{name}", MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [implPropertyType]));
                outputType.MethodImplementations.Add(new MethodImplementation(setterRef, setter));
            }

            outputType.Properties.Add(prop);
        }

        // Helper to get generic type arguments from the mapped interface
        TypeSignature GetGenericArg(int index)
        {
            return mappedInterfaceRef is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gits && index < gits.TypeArguments.Count
                ? gits.TypeArguments[index]
                : _outputModule.CorLibTypeFactory.Object;
        }

        TypeSignature uint32Sig = _outputModule.CorLibTypeFactory.UInt32;
        TypeSignature boolSig = _outputModule.CorLibTypeFactory.Boolean;
        TypeSignature objectSig = _outputModule.CorLibTypeFactory.Object;
        TypeSignature stringSig = _outputModule.CorLibTypeFactory.String;

        TypeSignature GetTypeRef(string ns, string name, string asm, bool isValueType = false) =>
            GetOrCreateTypeReference(ns, name, asm).ToTypeSignature(isValueType);

        TypeSignature GetGenericTypeRef(string ns, string name, string asm, params TypeSignature[] args) =>
            new GenericInstanceTypeSignature(GetOrCreateTypeReference(ns, name, asm), false, args);

        // Generate members for each known mapped type
        switch (mappedTypeName)
        {
            case "IClosable":
                AddMappedMethod("Close", null, null);
                break;

            case "IIterable`1":
                AddMappedMethod("First", null,
                    GetGenericTypeRef("Windows.Foundation.Collections", "IIterator`1", "Windows.Foundation.FoundationContract", GetGenericArg(0)));
                break;

            case "IMap`2":
                AddMappedMethod("Clear", null, null);
                AddMappedMethod("GetView", null,
                    GetGenericTypeRef("Windows.Foundation.Collections", "IMapView`2", "Windows.Foundation.FoundationContract", GetGenericArg(0), GetGenericArg(1)));
                AddMappedMethod("HasKey",
                    [("key", GetGenericArg(0), ParameterAttributes.In)], boolSig);
                AddMappedMethod("Insert",
                    [("key", GetGenericArg(0), ParameterAttributes.In), ("value", GetGenericArg(1), ParameterAttributes.In)], boolSig);
                AddMappedMethod("Lookup",
                    [("key", GetGenericArg(0), ParameterAttributes.In)], GetGenericArg(1));
                AddMappedMethod("Remove",
                    [("key", GetGenericArg(0), ParameterAttributes.In)], null);
                AddMappedProperty("Size", uint32Sig, false);
                break;

            case "IMapView`2":
                AddMappedMethod("HasKey",
                    [("key", GetGenericArg(0), ParameterAttributes.In)], boolSig);
                AddMappedMethod("Lookup",
                    [("key", GetGenericArg(0), ParameterAttributes.In)], GetGenericArg(1));
                AddMappedMethod("Split",
                    [("first", new ByReferenceTypeSignature(GetGenericTypeRef("Windows.Foundation.Collections", "IMapView`2", "Windows.Foundation.FoundationContract", GetGenericArg(0), GetGenericArg(1))), ParameterAttributes.Out),
                     ("second", new ByReferenceTypeSignature(GetGenericTypeRef("Windows.Foundation.Collections", "IMapView`2", "Windows.Foundation.FoundationContract", GetGenericArg(0), GetGenericArg(1))), ParameterAttributes.Out)], null);
                AddMappedProperty("Size", uint32Sig, false);
                break;

            case "IVector`1":
                AddMappedMethod("Append",
                    [("value", GetGenericArg(0), ParameterAttributes.In)], null);
                AddMappedMethod("Clear", null, null);
                AddMappedMethod("GetAt",
                    [("index", uint32Sig, ParameterAttributes.In)], GetGenericArg(0));
                AddMappedMethod("GetMany",
                    [("startIndex", uint32Sig, ParameterAttributes.In), ("items", new SzArrayTypeSignature(GetGenericArg(0)), ParameterAttributes.In)], uint32Sig);
                AddMappedMethod("GetView", null,
                    GetGenericTypeRef("Windows.Foundation.Collections", "IVectorView`1", "Windows.Foundation.FoundationContract", GetGenericArg(0)));
                AddMappedMethod("IndexOf",
                    [("value", GetGenericArg(0), ParameterAttributes.In), ("index", new ByReferenceTypeSignature(uint32Sig), ParameterAttributes.Out)], boolSig);
                AddMappedMethod("InsertAt",
                    [("index", uint32Sig, ParameterAttributes.In), ("value", GetGenericArg(0), ParameterAttributes.In)], null);
                AddMappedMethod("RemoveAt",
                    [("index", uint32Sig, ParameterAttributes.In)], null);
                AddMappedMethod("RemoveAtEnd", null, null);
                AddMappedMethod("ReplaceAll",
                    [("items", new SzArrayTypeSignature(GetGenericArg(0)), ParameterAttributes.In)], null);
                AddMappedMethod("SetAt",
                    [("index", uint32Sig, ParameterAttributes.In), ("value", GetGenericArg(0), ParameterAttributes.In)], null);
                AddMappedProperty("Size", uint32Sig, false);
                break;

            case "IVectorView`1":
                AddMappedMethod("GetAt",
                    [("index", uint32Sig, ParameterAttributes.In)], GetGenericArg(0));
                AddMappedMethod("GetMany",
                    [("startIndex", uint32Sig, ParameterAttributes.In), ("items", new SzArrayTypeSignature(GetGenericArg(0)), ParameterAttributes.In)], uint32Sig);
                AddMappedMethod("IndexOf",
                    [("value", GetGenericArg(0), ParameterAttributes.In), ("index", new ByReferenceTypeSignature(uint32Sig), ParameterAttributes.Out)], boolSig);
                AddMappedProperty("Size", uint32Sig, false);
                break;

            case "IBindableIterable":
                AddMappedMethod("First", null,
                    GetTypeRef("Microsoft.UI.Xaml.Interop", "IBindableIterator", "Microsoft.UI"));
                break;

            case "IBindableVector":
                AddMappedMethod("Append",
                    [("value", objectSig, ParameterAttributes.In)], null);
                AddMappedMethod("Clear", null, null);
                AddMappedMethod("GetAt",
                    [("index", uint32Sig, ParameterAttributes.In)], objectSig);
                AddMappedMethod("GetView", null,
                    GetTypeRef("Microsoft.UI.Xaml.Interop", "IBindableVectorView", "Microsoft.UI"));
                AddMappedMethod("IndexOf",
                    [("value", objectSig, ParameterAttributes.In), ("index", new ByReferenceTypeSignature(uint32Sig), ParameterAttributes.Out)], boolSig);
                AddMappedMethod("InsertAt",
                    [("index", uint32Sig, ParameterAttributes.In), ("value", objectSig, ParameterAttributes.In)], null);
                AddMappedMethod("RemoveAt",
                    [("index", uint32Sig, ParameterAttributes.In)], null);
                AddMappedMethod("RemoveAtEnd", null, null);
                AddMappedMethod("SetAt",
                    [("index", uint32Sig, ParameterAttributes.In), ("value", objectSig, ParameterAttributes.In)], null);
                AddMappedProperty("Size", uint32Sig, false);
                break;

            case "INotifyPropertyChanged":
                // Event: PropertyChanged
                AddMappedEvent(outputType, "PropertyChanged",
                    GetTypeRef("Microsoft.UI.Xaml.Data", "PropertyChangedEventHandler", "Microsoft.UI"),
                    mappedInterfaceRef, isPublic);
                break;

            case "ICommand":
                AddMappedEvent(outputType, "CanExecuteChanged",
                    GetGenericTypeRef("Windows.Foundation", "EventHandler`1", "Windows.Foundation.FoundationContract", objectSig),
                    mappedInterfaceRef, isPublic);
                AddMappedMethod("CanExecute",
                    [("parameter", objectSig, ParameterAttributes.In)], boolSig);
                AddMappedMethod("Execute",
                    [("parameter", objectSig, ParameterAttributes.In)], null);
                break;

            case "INotifyCollectionChanged":
                AddMappedEvent(outputType, "CollectionChanged",
                    GetTypeRef("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler", "Microsoft.UI"),
                    mappedInterfaceRef, isPublic);
                break;

            case "INotifyDataErrorInfo":
                AddMappedProperty("HasErrors", boolSig, false);
                AddMappedEvent(outputType, "ErrorsChanged",
                    GetGenericTypeRef("Windows.Foundation", "EventHandler`1", "Windows.Foundation.FoundationContract",
                        GetTypeRef("Microsoft.UI.Xaml.Data", "DataErrorsChangedEventArgs", "Microsoft.UI")),
                    mappedInterfaceRef, isPublic);
                AddMappedMethod("GetErrors",
                    [("propertyName", stringSig, ParameterAttributes.In)],
                    GetGenericTypeRef("Windows.Foundation.Collections", "IIterable`1", "Windows.Foundation.FoundationContract", objectSig));
                break;

            case "IXamlServiceProvider":
                AddMappedMethod("GetService",
                    [("type", GetTypeRef("Windows.UI.Xaml.Interop", "TypeName", "Windows.Foundation.UniversalApiContract", isValueType: true), ParameterAttributes.In)],
                    objectSig);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Adds a mapped event with add/remove methods and MethodImpl records.
    /// </summary>
    private void AddMappedEvent(
        TypeDefinition outputType,
        string eventName,
        TypeSignature handlerType,
        ITypeDefOrRef mappedInterfaceRef,
        bool isPublic)
    {
        string qualifiedPrefix = mappedInterfaceRef.FullName ?? "";
        TypeReference tokenType = GetOrCreateTypeReference("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract");
        TypeSignature tokenSig = tokenType.ToTypeSignature(true);

        ITypeDefOrRef handlerTypeRef = handlerType is TypeDefOrRefSignature tdrs ? tdrs.Type : (handlerType is GenericInstanceTypeSignature gits ? new TypeSpecification(gits) : tokenType);

        string addName = isPublic ? $"add_{eventName}" : $"{qualifiedPrefix}.add_{eventName}";
        string removeName = isPublic ? $"remove_{eventName}" : $"{qualifiedPrefix}.remove_{eventName}";

        MethodAttributes attrs = isPublic
            ? (MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName)
            : (MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName);

        // Add method
        MethodDefinition adder = new(addName, attrs, MethodSignature.CreateInstance(tokenSig, [handlerType]))
        {
            ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
        };
        adder.ParameterDefinitions.Add(new ParameterDefinition(1, "handler", ParameterAttributes.In));
        outputType.Methods.Add(adder);

        // Remove method
        MethodDefinition remover = new(removeName, attrs, MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [tokenSig]))
        {
            ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed
        };
        remover.ParameterDefinitions.Add(new ParameterDefinition(1, "token", ParameterAttributes.In));
        outputType.Methods.Add(remover);

        // Event
        EventDefinition evt = new(isPublic ? eventName : $"{qualifiedPrefix}.{eventName}", 0, handlerTypeRef);
        evt.Semantics.Add(new MethodSemantics(adder, MethodSemanticsAttributes.AddOn));
        evt.Semantics.Add(new MethodSemantics(remover, MethodSemanticsAttributes.RemoveOn));
        outputType.Events.Add(evt);

        // MethodImpls — use open generic form for handler type in declaration signatures
        // to match WinRT metadata conventions (e.g., EventHandler`1<!0> not EventHandler`1<Object>)
        TypeSignature implHandlerType = handlerType is GenericInstanceTypeSignature handlerGits
            ? ToOpenGenericFormStatic(handlerGits, _outputModule)
            : handlerType;
        MemberReference addRef = new(mappedInterfaceRef, $"add_{eventName}", MethodSignature.CreateInstance(tokenSig, [implHandlerType]));
        outputType.MethodImplementations.Add(new MethodImplementation(addRef, adder));
        MemberReference removeRef = new(mappedInterfaceRef, $"remove_{eventName}", MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [tokenSig]));
        outputType.MethodImplementations.Add(new MethodImplementation(removeRef, remover));
    }

    /// <summary>
    /// Gathers all interfaces from a type and its base type chain, including interfaces
    /// inherited from interfaces. Resolves generic type parameters through the base class chain.
    /// </summary>
    private List<InterfaceImplementation> GatherAllInterfaces(TypeDefinition type)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        List<InterfaceImplementation> result = [];

        void CollectFromType(TypeDefinition typeDef, TypeSignature[]? genericArgs)
        {
            foreach (InterfaceImplementation impl in typeDef.Interfaces)
            {
                if (impl.Interface == null)
                {
                    continue;
                }

                // If we have generic args, substitute them in the interface reference
                ITypeDefOrRef resolvedInterface = impl.Interface;
                if (genericArgs != null && impl.Interface is TypeSpecification ts &&
                    ts.Signature is GenericInstanceTypeSignature gits)
                {
                    // Resolve generic parameters in type arguments (recursively for nested generics)
                    TypeSignature[] resolvedArgs = [.. gits.TypeArguments.Select(arg => ResolveGenericArg(arg, genericArgs))];
                    resolvedInterface = new TypeSpecification(new GenericInstanceTypeSignature(gits.GenericType, gits.IsValueType, resolvedArgs));
                }

                string name = resolvedInterface.FullName ?? "";
                if (!seen.Add(name))
                {
                    continue;
                }

                if (IsPubliclyAccessible(resolvedInterface))
                {
                    result.Add(new InterfaceImplementation(resolvedInterface));
                }

                // Also collect interfaces inherited by this interface
                TypeDefinition? interfaceDef = resolvedInterface is TypeSpecification ts2
                    ? SafeResolve((ts2.Signature as GenericInstanceTypeSignature)?.GenericType)
                    : SafeResolve(resolvedInterface);

                if (interfaceDef != null)
                {
                    // Pass the resolved interface's generic args down
                    TypeSignature[]? innerArgs = resolvedInterface is TypeSpecification ts3 &&
                        ts3.Signature is GenericInstanceTypeSignature innerGits
                        ? [.. innerGits.TypeArguments]
                        : null;
                    CollectFromType(interfaceDef, innerArgs);
                }
            }
        }

        // Collect from the type itself
        CollectFromType(type, null);

        // Walk base types, resolving generic arguments
        ITypeDefOrRef? baseTypeRef = type.BaseType;
        while (baseTypeRef != null)
        {
            TypeDefinition? baseDef = SafeResolve(baseTypeRef);
            if (baseDef == null)
            {
                break;
            }

            // Get generic arguments from the base type reference
            TypeSignature[]? baseGenericArgs = baseTypeRef is TypeSpecification baseTs &&
                baseTs.Signature is GenericInstanceTypeSignature baseGits
                ? [.. baseGits.TypeArguments]
                : null;

            CollectFromType(baseDef, baseGenericArgs);
            baseTypeRef = baseDef.BaseType;
        }

        return result;
    }

    /// <summary>
    /// Recursively resolves generic parameters in a type signature using the provided generic arguments.
    /// Handles nested generic instances like KeyValuePair&lt;!0, !1&gt;.
    /// </summary>
    private static TypeSignature ResolveGenericArg(TypeSignature arg, TypeSignature[] genericArgs)
    {
        if (arg is GenericParameterSignature gps && gps.Index < genericArgs.Length)
        {
            return genericArgs[gps.Index];
        }

        if (arg is GenericInstanceTypeSignature nestedGits)
        {
            TypeSignature[] resolvedInnerArgs = [.. nestedGits.TypeArguments.Select(a => ResolveGenericArg(a, genericArgs))];
            return new GenericInstanceTypeSignature(nestedGits.GenericType, nestedGits.IsValueType, resolvedInnerArgs);
        }

        return arg;
    }

    /// <summary>
    /// Converts a GenericInstanceTypeSignature to its open form for MethodImpl declarations.
    /// E.g., EventHandler`1&lt;Object&gt; → EventHandler`1&lt;!0&gt;
    /// </summary>
    private static GenericInstanceTypeSignature ToOpenGenericFormStatic(GenericInstanceTypeSignature gits, ModuleDefinition module)
    {
        TypeSignature[] openArgs = new TypeSignature[gits.TypeArguments.Count];
        for (int i = 0; i < gits.TypeArguments.Count; i++)
        {
            openArgs[i] = new GenericParameterSignature(module, GenericParameterType.Type, i);
        }

        return new GenericInstanceTypeSignature(gits.GenericType, gits.IsValueType, openArgs);
    }

    /// <summary>
    /// Determines if a mapped interface is publicly implemented on the class.
    /// Checks if the class declares public methods whose names match the .NET interface members.
    /// For inherited interfaces, walks up the class hierarchy.
    /// </summary>
    private static bool IsInterfacePubliclyImplemented(TypeDefinition classType, InterfaceImplementation impl, RuntimeContext? runtimeContext)
    {
        TypeDefinition? interfaceDef = impl.Interface is TypeSpecification ts
            ? (ts.Signature as GenericInstanceTypeSignature)?.GenericType.Resolve(runtimeContext)
            : impl.Interface?.Resolve(runtimeContext);

        if (interfaceDef == null)
        {
            return false;
        }

        // Walk the class hierarchy to find public implementations
        TypeDefinition? current = classType;
        while (current != null)
        {
            foreach (MethodDefinition interfaceMethod in interfaceDef.Methods)
            {
                string methodName = interfaceMethod.Name?.Value ?? "";
                if (current.Methods.Any(m => m.IsPublic && m.Name?.Value == methodName))
                {
                    return true;
                }
            }

            current = current.BaseType?.Resolve(runtimeContext);
        }

        return false;
    }

    /// <summary>
    /// Formats a qualified interface name for use in explicit method names.
    /// Uses short type names for generic arguments (e.g., "String" not "System.String").
    /// </summary>
    private static string FormatQualifiedInterfaceName(ITypeDefOrRef typeRef)
    {
        if (typeRef is TypeSpecification spec && spec.Signature is GenericInstanceTypeSignature gits)
        {
            string baseName = gits.GenericType.FullName ?? "";
            string args = string.Join(", ", gits.TypeArguments.Select(FormatShortTypeName));
            return $"{baseName}<{args}>";
        }

        return typeRef.FullName ?? "";
    }

    /// <summary>
    /// Formats a type signature using short names (e.g., "String" instead of "System.String").
    /// </summary>
    private static string FormatShortTypeName(TypeSignature sig)
    {
        if (sig is GenericInstanceTypeSignature gits)
        {
            string baseName = gits.GenericType.FullName ?? "";
            string args = string.Join(", ", gits.TypeArguments.Select(FormatShortTypeName));
            return $"{baseName}<{args}>";
        }

        return sig is CorLibTypeSignature corLib
            ? corLib.Type.Name?.Value ?? sig.FullName
            : sig.FullName;
    }

    /// <summary>
    /// Collects the names of members that belong to custom mapped or unmapped .NET interfaces.
    /// These members should be excluded from the WinMD class definition since they're replaced
    /// by the WinRT mapped interface members.
    /// </summary>
    private HashSet<string> CollectCustomMappedMemberNames(TypeDefinition inputType)
    {
        HashSet<string> memberNames = new(StringComparer.Ordinal);

        List<InterfaceImplementation> allInterfaces = GatherAllInterfaces(inputType);

        foreach (InterfaceImplementation impl in allInterfaces)
        {
            if (impl.Interface == null)
            {
                continue;
            }

            string interfaceName = GetInterfaceQualifiedName(impl.Interface);

            // Include members from both mapped interfaces and unmapped interfaces
            if (!_mapper.HasMappingForType(interfaceName) &&
                !TypeMapper.ImplementedInterfacesWithoutMapping.Contains(interfaceName))
            {
                continue;
            }

            TypeDefinition? interfaceDef = impl.Interface is TypeSpecification ts
                ? SafeResolve((ts.Signature as GenericInstanceTypeSignature)?.GenericType)
                : SafeResolve(impl.Interface);

            if (interfaceDef == null)
            {
                continue;
            }

            // Add all method names from this interface
            foreach (MethodDefinition method in interfaceDef.Methods)
            {
                string methodName = method.Name?.Value ?? "";
                _ = memberNames.Add(methodName);

                // For property accessors, also add the property name
                if (methodName.StartsWith("get_", StringComparison.Ordinal) || methodName.StartsWith("set_", StringComparison.Ordinal))
                {
                    _ = memberNames.Add(methodName[4..]);
                }
            }

            // Add property names
            foreach (PropertyDefinition prop in interfaceDef.Properties)
            {
                _ = memberNames.Add(prop.Name?.Value ?? "");
            }

            // Add event names
            foreach (EventDefinition evt in interfaceDef.Events)
            {
                _ = memberNames.Add(evt.Name?.Value ?? "");
            }
        }

        return memberNames;
    }
}