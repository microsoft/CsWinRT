// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace ProjectionWriterTest.Helpers;

/// <summary>
/// Creates synthetic metadata whose excluded types collide with a selected runtime class's name.
/// </summary>
internal static class TypeFilteringMetadata
{
    public static string Create(string directory)
    {
        ModuleDefinition module = new("Contoso.winmd")
        {
            RuntimeVersion = "WindowsRuntime 1.4"
        };

        _ = new AssemblyDefinition("Contoso", new Version(255, 255, 255, 255))
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

        TypeSignature systemType = new TypeReference(module, corlib, "System", "Type").ToTypeSignature(false);

        CustomAttribute Attribute(string name, params CustomAttributeArgument[] arguments)
        {
            TypeReference attributeType = new(module, foundation, "Windows.Foundation.Metadata", name);
            MemberReference constructor = new(attributeType, ".ctor", MethodSignature.CreateInstance(
                module.CorLibTypeFactory.Void, arguments.Select(static argument => argument.ArgumentType).ToArray()));

            return new CustomAttribute(constructor, new CustomAttributeSignature(arguments));
        }

        void AddGuid(TypeDefinition type, Guid iid)
        {
            byte[] bytes = iid.ToByteArray();

            type.CustomAttributes.Add(Attribute("GuidAttribute",
                new(module.CorLibTypeFactory.UInt32, BitConverter.ToUInt32(bytes, 0)),
                new(module.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(bytes, 4)),
                new(module.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(bytes, 6)),
                new(module.CorLibTypeFactory.Byte, bytes[8]),
                new(module.CorLibTypeFactory.Byte, bytes[9]),
                new(module.CorLibTypeFactory.Byte, bytes[10]),
                new(module.CorLibTypeFactory.Byte, bytes[11]),
                new(module.CorLibTypeFactory.Byte, bytes[12]),
                new(module.CorLibTypeFactory.Byte, bytes[13]),
                new(module.CorLibTypeFactory.Byte, bytes[14]),
                new(module.CorLibTypeFactory.Byte, bytes[15])));
        }

        TypeDefinition AddInterface(string ns, string name, Guid iid)
        {
            TypeDefinition type = new(ns, name,
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.WindowsRuntime);

            module.TopLevelTypes.Add(type);
            AddGuid(type, iid);

            return type;
        }

        void AddMethod(TypeDefinition type, string name, TypeSignature returnType, params TypeSignature[] parameters)
        {
            type.Methods.Add(new MethodDefinition(name,
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                MethodSignature.CreateInstance(returnType, parameters)));
        }

        TypeDefinition AddClass(string ns, string name, Guid defaultIid, Guid staticIid)
        {
            TypeDefinition type = new(ns, name,
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime,
                module.CorLibTypeFactory.Object.Type);

            module.TopLevelTypes.Add(type);

            TypeDefinition defaultInterface = AddInterface(ns, $"I{name}", defaultIid);
            defaultInterface.CustomAttributes.Add(Attribute("ExclusiveToAttribute", new CustomAttributeArgument(systemType, type.ToTypeSignature())));
            AddMethod(defaultInterface, "GetValue", module.CorLibTypeFactory.Object);

            InterfaceImplementation implementation = new(defaultInterface);
            implementation.CustomAttributes.Add(Attribute("DefaultAttribute"));
            type.Interfaces.Add(implementation);

            TypeDefinition statics = AddInterface(ns, $"I{name}Statics", staticIid);
            statics.CustomAttributes.Add(Attribute("ExclusiveToAttribute", new CustomAttributeArgument(systemType, type.ToTypeSignature())));
            AddMethod(statics, "GetStaticValue", module.CorLibTypeFactory.Int32);
            type.CustomAttributes.Add(Attribute("StaticAttribute",
                new(systemType, statics.ToTypeSignature()),
                new(module.CorLibTypeFactory.UInt32, 1u)));

            return defaultInterface;
        }

        _ = AddClass("Contoso", "User",
            new Guid("A3DD8A5E-90C1-4E15-B3A8-95444C622501"),
            new Guid("A3DD8A5E-90C1-4E15-B3A8-95444C622502"));
        _ = AddClass("Contoso", "User2",
            new Guid("A3DD8A5E-90C1-4E15-B3A8-95444C622503"),
            new Guid("A3DD8A5E-90C1-4E15-B3A8-95444C622504"));
        TypeDefinition omittedInterface = AddClass("Contoso.UserProfile", "UserSetupManager",
            new Guid("A3DD8A5E-90C1-4E15-B3A8-95444C622505"),
            new Guid("A3DD8A5E-90C1-4E15-B3A8-95444C622506"));

        // These signatures are intentionally unsupported. Excluding their declaring types from the
        // reference projection must also exclude them when regenerating the implementation.
        TypeDefinition propertyValue = AddInterface("Windows.Foundation", "IPropertyValue",
            new Guid("4BD682DD-7554-40E9-9A9B-82654EDE7E62"));
        TypeDefinition asyncOperation = AddInterface("Windows.Foundation", "IAsyncOperation`1",
            new Guid("9FC2B0BB-E446-44E2-AA61-9CAB8F636AF2"));
        asyncOperation.GenericParameters.Add(new GenericParameter("T"));

        AddMethod(omittedInterface, "SetProperty", module.CorLibTypeFactory.Void, propertyValue.ToTypeSignature());
        AddMethod(omittedInterface, "GetPropertyAsync", new GenericInstanceTypeSignature(asyncOperation, false, [propertyValue.ToTypeSignature()]));

        string path = Path.Combine(directory, "Contoso.winmd");
        module.Write(path);

        return path;
    }
}
