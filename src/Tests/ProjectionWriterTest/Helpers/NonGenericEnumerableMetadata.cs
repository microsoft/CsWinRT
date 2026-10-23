// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace ProjectionWriterTest.Helpers;

internal static class NonGenericEnumerableMetadata
{
    private const string RequiredItemsIid = "417d2345-6a80-4a44-bff7-6abe92c5c141";

    public static string Create(string directory, string fileStem, bool includeBindableRequiredItems = false)
    {
        ModuleDefinition module = new("EnumerableTests.winmd") { RuntimeVersion = "WindowsRuntime 1.4" };
        _ = new AssemblyDefinition("EnumerableTests", new Version(255, 255, 255, 255))
        {
            Modules = { module },
            Attributes = AssemblyAttributes.ContentWindowsRuntime
        };
        AssemblyReference corlib = (AssemblyReference)module.CorLibTypeFactory.CorLibScope;
        corlib.Version = new Version(255, 255, 255, 255);
        AssemblyReference foundation = new("Windows.Foundation.FoundationContract", new Version(255, 255, 255, 255))
        {
            Attributes = AssemblyAttributes.ContentWindowsRuntime
        };
        TypeReference guidAttribute = new(module, foundation, "Windows.Foundation.Metadata", "GuidAttribute");
        MemberReference guidConstructor = new(guidAttribute, ".ctor", MethodSignature.CreateInstance(
            module.CorLibTypeFactory.Void,
            [module.CorLibTypeFactory.UInt32, module.CorLibTypeFactory.UInt16,
                module.CorLibTypeFactory.UInt16, .. Enumerable.Repeat(module.CorLibTypeFactory.Byte, 8)]));

        TypeDefinition Interface(string ns, string name, string iid, params string[] parameters)
        {
            TypeDefinition type = new(ns, name,
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Interface | TypeAttributes.WindowsRuntime);
            module.TopLevelTypes.Add(type);
            foreach (string parameter in parameters)
            {
                type.GenericParameters.Add(new GenericParameter(parameter));
            }
            byte[] bytes = new Guid(iid).ToByteArray();
            type.CustomAttributes.Add(new CustomAttribute(guidConstructor, new CustomAttributeSignature(
            [
                new(module.CorLibTypeFactory.UInt32, BitConverter.ToUInt32(bytes, 0)),
                new(module.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(bytes, 4)),
                new(module.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(bytes, 6)),
                .. bytes[8..].Select(value => new CustomAttributeArgument(module.CorLibTypeFactory.Byte, value))
            ])));
            return type;
        }

        GenericInstanceTypeSignature Generic(TypeDefinition type, params TypeSignature[] arguments) => new(type, false, arguments);
        GenericParameterSignature first = new(GenericParameterType.Type, 0);
        GenericParameterSignature second = new(GenericParameterType.Type, 1);
        TypeDefinition iterable = Interface("Windows.Foundation.Collections", "IIterable`1", "faa585ea-6214-4217-afda-7f46de5869b3", "T");
        TypeDefinition pair = Interface("Windows.Foundation.Collections", "IKeyValuePair`2", "02b51929-c1c4-4a7e-8940-0312b5c18500", "K", "V");
        TypeDefinition vector = Interface("Windows.Foundation.Collections", "IVector`1", "913337e9-11a1-4345-a3a2-4e7f956e222d", "T");
        TypeDefinition vectorView = Interface("Windows.Foundation.Collections", "IVectorView`1", "bbe1fa4c-b0e3-4583-baef-1f1b2e483e56", "T");
        TypeDefinition map = Interface("Windows.Foundation.Collections", "IMap`2", "3c2925fe-8519-45c1-aa79-197b6718c1c1", "K", "V");
        TypeDefinition mapView = Interface("Windows.Foundation.Collections", "IMapView`2", "e480ce40-a338-4ada-adcf-272272e48cb9", "K", "V");
        TypeDefinition observableVector = Interface("Windows.Foundation.Collections", "IObservableVector`1", "5917eb53-50b4-4a0d-b309-65862b3f1dbc", "T");
        TypeDefinition observableMap = Interface("Windows.Foundation.Collections", "IObservableMap`2", "65df2bf5-bf39-41b5-aebc-5a9d865e472b", "K", "V");
        foreach (TypeDefinition collection in new[] { vector, vectorView })
        {
            collection.Interfaces.Add(new InterfaceImplementation(Generic(iterable, first).ToTypeDefOrRef()));
        }
        map.Interfaces.Add(new InterfaceImplementation(Generic(iterable, Generic(pair, first, second)).ToTypeDefOrRef()));
        mapView.Interfaces.Add(new InterfaceImplementation(Generic(iterable, Generic(pair, first, second)).ToTypeDefOrRef()));
        observableVector.Interfaces.Add(new InterfaceImplementation(Generic(vector, first).ToTypeDefOrRef()));
        observableMap.Interfaces.Add(new InterfaceImplementation(Generic(map, first, second).ToTypeDefOrRef()));
        TypeDefinition required = Interface("Contoso", "IRequiredItems", RequiredItemsIid);
        required.Interfaces.Add(new InterfaceImplementation(Generic(observableVector, module.CorLibTypeFactory.Int32).ToTypeDefOrRef()));
        TypeDefinition api = Interface("Contoso", "ICollectionSource", "20ad659c-7a8c-4e53-8347-f9c2242fb901");
        TypeReference defaultAttribute = new(module, foundation, "Windows.Foundation.Metadata", "DefaultAttribute");
        TypeDefinition source = new("Contoso", "CollectionSource",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime, module.CorLibTypeFactory.Object.Type);
        source.Interfaces.Add(new InterfaceImplementation(api)
        {
            CustomAttributes =
            {
                new CustomAttribute(new MemberReference(defaultAttribute, ".ctor",
                    MethodSignature.CreateInstance(module.CorLibTypeFactory.Void)))
            }
        });
        module.TopLevelTypes.Add(source);

        void Property(string name, TypeSignature result)
        {
            MethodDefinition getter = new($"get_{name}",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                MethodSignature.CreateInstance(result));
            api.Methods.Add(getter);
            PropertyDefinition property = new(name, PropertyAttributes.None, PropertySignature.CreateInstance(result));
            property.Semantics.Add(new MethodSemantics(getter, MethodSemanticsAttributes.Getter));
            api.Properties.Add(property);
        }

        TypeSignature integer = module.CorLibTypeFactory.Int32;
        TypeSignature text = module.CorLibTypeFactory.String;
        Property("Items", Generic(vector, integer));
        Property("ReadOnlyItems", Generic(vectorView, integer));
        Property("Names", Generic(vector, text));
        Property("Pairs", Generic(vector, Generic(pair, integer, text)));
        Property("Dictionary", Generic(map, integer, text));
        Property("ReadOnlyDictionary", Generic(mapView, integer, text));
        Property("ObservableItems", Generic(observableVector, integer));
        Property("ObservableDictionary", Generic(observableMap, integer, text));
        Property("RequiredItems", required.ToTypeSignature(false));
        TypeDefinition requiredMap = Interface("Contoso", "IRequiredDictionary", "417d2345-6a80-4a44-bff7-6abe92c5c145");
        requiredMap.Interfaces.Add(new InterfaceImplementation(Generic(observableMap, integer, text).ToTypeDefOrRef()));
        Property("RequiredDictionary", requiredMap.ToTypeSignature(false));

        // The required-interface traversal must deduplicate diamonds and decline ambiguous element types.
        TypeDefinition diamond = Interface("Contoso", "IDiamondItems", "417d2345-6a80-4a44-bff7-6abe92c5c142");
        diamond.Interfaces.Add(new InterfaceImplementation(required));
        diamond.Interfaces.Add(new InterfaceImplementation(Generic(iterable, integer).ToTypeDefOrRef()));
        TypeDefinition ambiguous = Interface("Contoso", "IAmbiguousItems", "417d2345-6a80-4a44-bff7-6abe92c5c143");
        ambiguous.Interfaces.Add(new InterfaceImplementation(Generic(iterable, integer).ToTypeDefOrRef()));
        ambiguous.Interfaces.Add(new InterfaceImplementation(Generic(iterable, text).ToTypeDefOrRef()));
        Property("AmbiguousItems", ambiguous.ToTypeSignature(false));
        TypeDefinition bindable = Interface("Windows.UI.Xaml.Interop", "IBindableIterable", "036d2c08-df29-41af-8aa2-d774be62ba6f");
        TypeDefinition both = Interface("Contoso", "IBindableItems", "417d2345-6a80-4a44-bff7-6abe92c5c144");
        both.Interfaces.Add(new InterfaceImplementation(bindable));
        if (includeBindableRequiredItems)
        {
            both.Interfaces.Add(new InterfaceImplementation(required));
        }

        string path = Path.Combine(directory, fileStem + ".winmd");
        module.Write(path);
        return path;
    }
}
