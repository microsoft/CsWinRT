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
/// Creates synthetic metadata with array valued properties on an <c>[exclusiveto]</c> interface, so that the
/// CCW (<c>Do_Abi</c>) side has to marshal an array <i>into</i> a property setter.
/// </summary>
/// <remarks>
/// Array valued <b>properties</b> are the shape that is interesting here. An array <i>parameter</i> projects
/// as a span, but a property keeps its <c>T[]</c> type, so the setter cannot simply hand over the span local
/// the way a method call can.
/// </remarks>
internal static class ArrayPropertyMetadata
{
    public static string Create(string directory)
    {
        ModuleDefinition module = new("Contoso.winmd") { RuntimeVersion = "WindowsRuntime 1.4" };

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

        void AddGuid(TypeDefinition type, string value)
        {
            byte[] bytes = new Guid(value).ToByteArray();

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

        TypeDefinition owner = new("Contoso", "Widget",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime,
            module.CorLibTypeFactory.Object.Type);

        module.TopLevelTypes.Add(owner);

        TypeDefinition iface = new("Contoso", "IWidget",
            TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.WindowsRuntime);

        module.TopLevelTypes.Add(iface);
        AddGuid(iface, "1f9b3c50-6d21-4f2e-9a33-5c71c0d84e01");
        iface.CustomAttributes.Add(Attribute("ExclusiveToAttribute", new CustomAttributeArgument(systemType, owner.ToTypeSignature())));

        InterfaceImplementation implementation = new(iface);
        owner.Interfaces.Add(implementation);
        implementation.CustomAttributes.Add(Attribute("DefaultAttribute"));

        // A read/write property whose type is an array. Both a non-blittable element (which the CCW side has
        // to copy through a marshaller) and a blittable one (which it reads straight off the ABI buffer) are
        // covered, since the two take different paths to produce the local.
        void AddArrayProperty(string name, TypeSignature elementType)
        {
            TypeSignature arrayType = elementType.MakeSzArrayType();

            MethodDefinition getter = new($"get_{name}",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual |
                MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                MethodSignature.CreateInstance(arrayType));

            MethodDefinition setter = new($"put_{name}",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual |
                MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                MethodSignature.CreateInstance(module.CorLibTypeFactory.Void, [arrayType]));

            // Real Windows Runtime metadata marks a property setter's value as '[in]', which is what makes it
            // a read-only input array rather than a buffer the callee fills.
            setter.ParameterDefinitions.Add(new ParameterDefinition(1, "value", ParameterAttributes.In));

            iface.Methods.Add(getter);
            iface.Methods.Add(setter);

            PropertyDefinition property = new(name, 0, PropertySignature.CreateInstance(arrayType));

            property.Semantics.Add(new MethodSemantics(getter, MethodSemanticsAttributes.Getter));
            property.Semantics.Add(new MethodSemantics(setter, MethodSemanticsAttributes.Setter));

            iface.Properties.Add(property);
        }

        AddArrayProperty("Names", module.CorLibTypeFactory.String);
        AddArrayProperty("Counts", module.CorLibTypeFactory.Int32);

        string path = Path.Combine(directory, "Contoso.winmd");

        module.Write(path);

        return path;
    }
}
