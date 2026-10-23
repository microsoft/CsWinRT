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
/// Adds value, delegate, and metadata-only shapes to the existing projection fixture.
/// </summary>
internal static class IidProjectionMetadata
{
    public static string Create(string directory)
    {
        string path = TypeFilteringMetadata.Create(directory);
        ModuleDefinition module = ModuleDefinition.FromFile(path);
        IResolutionScope corlib = module.CorLibTypeFactory.CorLibScope;
        AssemblyReference foundation = new("Windows.Foundation.FoundationContract", new Version(255, 255, 255, 255));

        CustomAttribute Attribute(string name, params CustomAttributeArgument[] arguments)
        {
            TypeReference attributeType = new(module, foundation, "Windows.Foundation.Metadata", name);
            MemberReference constructor = new(attributeType, ".ctor", MethodSignature.CreateInstance(
                module.CorLibTypeFactory.Void, arguments.Select(argument => argument.ArgumentType).ToArray()));
            return new(constructor, new CustomAttributeSignature(arguments));
        }

        TypeDefinition AddType(string name, string baseName)
        {
            TypeDefinition type = new("Contoso", name,
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime,
                new TypeReference(module, corlib, "System", baseName));
            module.TopLevelTypes.Add(type);
            return type;
        }

        void AddGuid(TypeDefinition type, Guid iid)
        {
            byte[] guid = iid.ToByteArray();
            type.CustomAttributes.Add(Attribute("GuidAttribute",
            [
                new(module.CorLibTypeFactory.UInt32, BitConverter.ToUInt32(guid, 0)),
                new(module.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(guid, 4)),
                new(module.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(guid, 6)),
                .. guid[8..].Select(value => new CustomAttributeArgument(module.CorLibTypeFactory.Byte, value))
            ]));
        }

        TypeDefinition value = AddType("Value", "ValueType");
        value.IsSequentialLayout = true;
        value.Fields.Add(new FieldDefinition("Number", FieldAttributes.Public, module.CorLibTypeFactory.Int32));
        value.Fields.Add(new FieldDefinition("Text", FieldAttributes.Public, module.CorLibTypeFactory.String));

        TypeDefinition typeValue = AddType("TypeValue", "ValueType");
        typeValue.IsSequentialLayout = true;
        typeValue.Fields.Add(new FieldDefinition("Type", FieldAttributes.Public,
            new TypeReference(module, corlib, "System", "Type").ToTypeSignature(false)));

        TypeDefinition nested = AddType("Nested", "ValueType");
        nested.IsSequentialLayout = true;
        nested.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, value.ToTypeSignature(true)));

        TypeDefinition choice = AddType("Choice", "Enum");
        choice.Fields.Add(new FieldDefinition("value__",
            FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName,
            module.CorLibTypeFactory.Int32));
        choice.Fields.Add(new FieldDefinition("First", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal,
            choice.ToTypeSignature(true)) { Constant = Constant.FromValue(0) });

        TypeDefinition flags = AddType("Flags", "Enum");
        flags.Fields.Add(new FieldDefinition("value__",
            FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName,
            module.CorLibTypeFactory.UInt32));
        flags.Fields.Add(new FieldDefinition("None", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal,
            flags.ToTypeSignature(true)) { Constant = Constant.FromValue(0u) });
        flags.CustomAttributes.Add(new CustomAttribute(new MemberReference(
            new TypeReference(module, corlib, "System", "FlagsAttribute"), ".ctor",
            MethodSignature.CreateInstance(module.CorLibTypeFactory.Void)), new CustomAttributeSignature()));

        TypeDefinition handler = AddType("Handler", "MulticastDelegate");
        MethodDefinition invoke = new("Invoke",
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
            MethodSignature.CreateInstance(module.CorLibTypeFactory.Void))
        {
            ImplAttributes = MethodImplAttributes.Runtime
        };
        handler.Methods.Add(invoke);
        AddGuid(handler, new Guid("60A8E0C5-7371-4B96-AB04-B2CCFAFCE870"));

        TypeDefinition standalone = new("Contoso", "IStandalone",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Interface | TypeAttributes.WindowsRuntime);
        module.TopLevelTypes.Add(standalone);
        AddGuid(standalone, new Guid("A3DD8A5E-90C1-4E15-B3A8-95444C622509"));

        _ = AddType("MetadataAttribute", "Attribute");
        _ = AddType("NoIdentity", "Object");
        TypeDefinition staticClass = AddType("Statics", "Object");
        staticClass.IsAbstract = true;
        TypeDefinition contract = AddType("Contract", "ValueType");
        contract.CustomAttributes.Add(Attribute("ApiContractAttribute"));

        using MemoryStream stream = new();
        module.Write(stream);
        File.WriteAllBytes(path, stream.ToArray());
        return path;
    }
}
