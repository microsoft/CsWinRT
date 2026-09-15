// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace ProjectionWriterTest.Helpers;

internal static class CreateFromStringMetadata
{
    public static string Create(string directory)
    {
        ModuleDefinition module = new("TestComponentCSharp.winmd")
        {
            RuntimeVersion = "WindowsRuntime 1.4"
        };

        _ = new AssemblyDefinition("TestComponentCSharp", new Version(255, 255, 255, 255))
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

        TypeDefinition AddInterface(string name, Guid iid, TypeDefinition owner)
        {
            TypeDefinition type = new("TestComponentCSharp", name,
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.WindowsRuntime);

            module.TopLevelTypes.Add(type);

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
            type.CustomAttributes.Add(Attribute("ExclusiveToAttribute", new CustomAttributeArgument(systemType, owner.ToTypeSignature())));

            return type;
        }

        TypeDefinition AddClass(string name, Guid iid)
        {
            TypeDefinition type = new("TestComponentCSharp", name,
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime,
                module.CorLibTypeFactory.Object.Type);

            module.TopLevelTypes.Add(type);

            InterfaceImplementation implementation = new(AddInterface($"I{name}", iid, type));
            implementation.CustomAttributes.Add(Attribute("DefaultAttribute"));
            type.Interfaces.Add(implementation);

            return type;
        }

        TypeDefinition AddStruct(string name, string fieldName, TypeSignature fieldType)
        {
            TypeDefinition type = new("TestComponentCSharp", name,
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.WindowsRuntime,
                new TypeReference(module, corlib, "System", "ValueType"));

            type.Fields.Add(new FieldDefinition(fieldName, FieldAttributes.Public, new FieldSignature(fieldType)));
            module.TopLevelTypes.Add(type);

            return type;
        }

        void AddCreateFromString(TypeDefinition type, string methodName)
        {
            CustomAttribute attribute = Attribute("CreateFromStringAttribute");
            attribute.Signature!.NamedArguments.Add(new CustomAttributeNamedArgument(
                CustomAttributeArgumentMemberType.Field,
                "MethodName",
                module.CorLibTypeFactory.String,
                new CustomAttributeArgument(module.CorLibTypeFactory.String, methodName)));
            type.CustomAttributes.Add(attribute);
        }

        TypeDefinition runtimeClass = AddClass("Class", new Guid("AC198551-BECE-4221-9268-30B70B5A2501"));
        TypeDefinition valueType = AddStruct("NonBlittableStringStruct", "str", module.CorLibTypeFactory.String);

        AddCreateFromString(runtimeClass, "TestComponentCSharp.Class.CreateFromString");
        AddCreateFromString(valueType, "TestComponentCSharp.Class.CreateStructFromString");

        _ = AddClass("NonAgileClass", new Guid("AC198551-BECE-4221-9268-30B70B5A2502"));
        _ = AddStruct("BlittableStruct", "i32", module.CorLibTypeFactory.Int32);

        TypeDefinition statics = AddInterface("IClassStatics", new Guid("AC198551-BECE-4221-9268-30B70B5A2503"), runtimeClass);
        runtimeClass.CustomAttributes.Add(Attribute("StaticAttribute",
            new(systemType, statics.ToTypeSignature()),
            new(module.CorLibTypeFactory.UInt32, 1u)));

        foreach ((string name, TypeDefinition result) in new[] { ("CreateFromString", runtimeClass), ("CreateStructFromString", valueType) })
        {
            MethodDefinition method = new(name,
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                MethodSignature.CreateInstance(result.ToTypeSignature(), [module.CorLibTypeFactory.String]));
            method.ParameterDefinitions.Add(new ParameterDefinition(1, "value", ParameterAttributes.In));
            statics.Methods.Add(method);
        }

        string path = Path.Combine(directory, "TestComponentCSharp.winmd");
        module.Write(path);

        return path;
    }
}
