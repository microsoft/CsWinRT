// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace ProjectionWriterTest.Helpers;

internal static class GenericMarshallerMetadata
{
    public const string TileCollectionIid = "20ad659c-7a8c-4e53-8347-f9c2242fb901";
    public const string RegistrationIid = "20ad659c-7a8c-4e53-8347-f9c2242fb902";

    public static string Create(string directory, string fileStem)
    {
        // Neither the assembly nor module name changes when the input file is renamed.
        ModuleDefinition module = new("WindowsUdk.winmd") { RuntimeVersion = "WindowsRuntime 1.4" };
        _ = new AssemblyDefinition("WindowsUdk", new Version(255, 255, 255, 255))
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
        TypeSignature guid = new TypeReference(module, corlib, "System", "Guid").ToTypeSignature(true);

        CustomAttribute Attribute(string name, params CustomAttributeArgument[] arguments)
        {
            TypeReference type = new(module, foundation, "Windows.Foundation.Metadata", name);
            MemberReference constructor = new(type, ".ctor", MethodSignature.CreateInstance(
                module.CorLibTypeFactory.Void, arguments.Select(argument => argument.ArgumentType).ToArray()));
            return new CustomAttribute(constructor, new CustomAttributeSignature(arguments));
        }

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
            type.CustomAttributes.Add(Attribute("GuidAttribute",
            [
                new(module.CorLibTypeFactory.UInt32, BitConverter.ToUInt32(bytes, 0)),
                new(module.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(bytes, 4)),
                new(module.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(bytes, 6)),
                .. bytes[8..].Select(value => new CustomAttributeArgument(module.CorLibTypeFactory.Byte, value))
            ]));
            return type;
        }

        MethodDefinition Method(TypeDefinition type, string name, TypeSignature result, params TypeSignature[] parameters)
        {
            MethodDefinition method = new(name,
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                MethodSignature.CreateInstance(result, parameters));
            type.Methods.Add(method);
            for (int i = 0; i < parameters.Length; i++)
            {
                method.ParameterDefinitions.Add(new ParameterDefinition((ushort)(i + 1), $"value{i}", ParameterAttributes.In));
            }
            return method;
        }

        void Property(TypeDefinition type, string name, TypeSignature value)
        {
            MethodDefinition getter = Method(type, $"get_{name}", value);
            getter.IsSpecialName = true;
            PropertyDefinition property = new(name, PropertyAttributes.None, PropertySignature.CreateInstance(value));
            property.Semantics.Add(new MethodSemantics(getter, MethodSemanticsAttributes.Getter));
            type.Properties.Add(property);
        }

        TypeDefinition Class(string name, ITypeDefOrRef defaultInterface)
        {
            TypeDefinition type = new("Contoso", name,
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime, module.CorLibTypeFactory.Object.Type);
            type.Interfaces.Add(new InterfaceImplementation(defaultInterface)
            {
                CustomAttributes = { Attribute("DefaultAttribute") }
            });
            module.TopLevelTypes.Add(type);
            return type;
        }

        GenericInstanceTypeSignature Generic(TypeDefinition type, params TypeSignature[] arguments) => new(type, false, arguments);

        TypeDefinition asyncOperation = Interface("Windows.Foundation", "IAsyncOperation`1", "9fc2b0bb-e446-44e2-aa61-9cab8f636af2", "T");
        TypeDefinition iterable = Interface("Windows.Foundation.Collections", "IIterable`1", "faa585ea-6214-4217-afda-7f46de5869b3", "T");
        TypeDefinition pair = Interface("Windows.Foundation.Collections", "IKeyValuePair`2", "02b51929-c1c4-4a7e-8940-0312b5c18500", "K", "V");
        TypeDefinition map = Interface("Windows.Foundation.Collections", "IMapView`2", "e480ce40-a338-4ada-adcf-272272e48cb9", "K", "V");
        map.Interfaces.Add(new InterfaceImplementation(Generic(iterable, Generic(pair,
            new GenericParameterSignature(GenericParameterType.Type, 0),
            new GenericParameterSignature(GenericParameterType.Type, 1))).ToTypeDefOrRef()));

        TypeDefinition handler = Interface("Windows.Foundation", "EventHandler`1", "9de1c535-6ae1-11e0-84e1-18a905bcc53f", "T");
        TypeDefinition typedHandler = Interface("Windows.Foundation", "TypedEventHandler`2", "9de1c534-6ae1-11e0-84e1-18a905bcc53f", "TSender", "TResult");
        foreach (TypeDefinition type in new[] { handler, typedHandler })
        {
            type.Attributes = TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime;
            type.BaseType = new TypeReference(module, corlib, "System", "MulticastDelegate");
            MethodDefinition invoke = Method(type, "Invoke", module.CorLibTypeFactory.Void,
                type == handler ? module.CorLibTypeFactory.Object : new GenericParameterSignature(GenericParameterType.Type, 0),
                new GenericParameterSignature(GenericParameterType.Type, type.GenericParameters.Count - 1));
            invoke.IsAbstract = false;
            invoke.ImplAttributes = MethodImplAttributes.Runtime;
        }

        TypeDefinition token = new("Windows.Foundation", "EventRegistrationToken",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.WindowsRuntime,
            new TypeReference(module, corlib, "System", "ValueType"));
        token.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, module.CorLibTypeFactory.Int64));
        module.TopLevelTypes.Add(token);

        TypeDefinition registration = Interface("Contoso", "ITaskRegistration", RegistrationIid);
        Property(registration, "Id", guid);
        TypeDefinition api = Interface("Contoso", "ITileCollection", TileCollectionIid);
        TypeDefinition tiles = Class("TileCollection", api);
        GenericInstanceTypeSignature operation = Generic(asyncOperation, tiles.ToTypeSignature());
        GenericInstanceTypeSignature allTasks = Generic(map, guid, registration.ToTypeSignature());
        GenericInstanceTypeSignature guidHandler = Generic(handler, guid);
        TypeDefinition registrationMap = Class("RegistrationMap", allTasks.ToTypeDefOrRef());
        registrationMap.Interfaces.Add(new InterfaceImplementation(Generic(iterable,
            Generic(pair, guid, registration.ToTypeSignature())).ToTypeDefOrRef()));

        _ = Method(api, "GetTilesAsync", operation);
        MethodDefinition overload = Method(api, "GetTilesAsync", operation, module.CorLibTypeFactory.Int32);
        overload.CustomAttributes.Add(Attribute("OverloadAttribute",
            new CustomAttributeArgument(module.CorLibTypeFactory.String, "GetTilesWithOptionsAsync")));
        Property(api, "AllTasks", allTasks);
        Property(api, "NestedTasks", Generic(map, guid, allTasks));
        Property(api, "NestedHandler", Generic(handler, allTasks));

        void Event(string name, TypeSignature eventType)
        {
            MethodDefinition add = Method(api, $"add_{name}", token.ToTypeSignature(true), eventType);
            MethodDefinition remove = Method(api, $"remove_{name}", module.CorLibTypeFactory.Void, token.ToTypeSignature(true));
            add.IsSpecialName = true;
            remove.IsSpecialName = true;
            EventDefinition definition = new(name, EventAttributes.None, eventType.ToTypeDefOrRef());
            definition.Semantics.Add(new MethodSemantics(add, MethodSemanticsAttributes.AddOn));
            definition.Semantics.Add(new MethodSemantics(remove, MethodSemanticsAttributes.RemoveOn));
            api.Events.Add(definition);
        }

        Event("Changed", Generic(typedHandler, tiles.ToTypeSignature(), guid));
        Event("GuidChanged", guidHandler);
        foreach ((string name, TypeSignature element) in new (string, TypeSignature)[]
        {
            ("Ids", guid), ("Tiles", tiles.ToTypeSignature()), ("Maps", allTasks), ("Handlers", guidHandler)
        })
        {
            SzArrayTypeSignature array = new(element);
            _ = name is "Maps" or "Handlers"
                ? Method(api, $"Accept{name}", module.CorLibTypeFactory.Void, array)
                : Method(api, $"Echo{name}", array, array);
        }

        string path = Path.Combine(directory, fileStem + ".winmd");
        module.Write(path);
        return path;
    }
}
