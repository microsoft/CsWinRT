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
/// Creates synthetic metadata with delegates of different shapes (the kind used to declare events).
/// </summary>
/// <remarks>
/// <code>
/// namespace Contoso
/// {
///     struct Pair { Int32 First; Int32 Second; }
///     delegate void VoidHandler(Object sender, Int32 value);
///     delegate Int32 ResultHandler(Int32 value);
///     delegate Boolean OutputHandler(Int32 value, [out] String text, [out] Int32[] numbers);
///     delegate void VoidOutputHandler([out] String text);
///     delegate void ParametersHandler([in] Pair pair, [in] Int32[] source, Int32[] destination);
/// }
/// </code>
/// </remarks>
internal static class EventDelegateMetadata
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

        void AddGuid(TypeDefinition type, string value)
        {
            byte[] bytes = new Guid(value).ToByteArray();

            TypeReference attributeType = new(module, foundation, "Windows.Foundation.Metadata", "GuidAttribute");
            CustomAttributeArgument[] arguments =
            [
                new(module.CorLibTypeFactory.UInt32, BitConverter.ToUInt32(bytes, 0)),
                new(module.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(bytes, 4)),
                new(module.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(bytes, 6)),
                .. bytes[8..].Select(value => new CustomAttributeArgument(module.CorLibTypeFactory.Byte, value))
            ];
            MemberReference constructor = new(attributeType, ".ctor", MethodSignature.CreateInstance(
                module.CorLibTypeFactory.Void, arguments.Select(static argument => argument.ArgumentType).ToArray()));

            type.CustomAttributes.Add(new CustomAttribute(constructor, new CustomAttributeSignature(arguments)));
        }

        void AddDelegate(string name, string iid, TypeSignature returnType, params (string Name, TypeSignature Type, ParameterAttributes Attributes, bool IsByRef)[] parameters)
        {
            TypeDefinition type = new("Contoso", name,
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.WindowsRuntime,
                new TypeReference(module, corlib, "System", "MulticastDelegate"));

            module.TopLevelTypes.Add(type);
            AddGuid(type, iid);

            type.Methods.Add(new MethodDefinition(".ctor",
                MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RuntimeSpecialName,
                MethodSignature.CreateInstance(module.CorLibTypeFactory.Void, [module.CorLibTypeFactory.Object, module.CorLibTypeFactory.IntPtr]))
            {
                ImplAttributes = MethodImplAttributes.Runtime
            });

            MethodDefinition invoke = new("Invoke",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.SpecialName,
                MethodSignature.CreateInstance(returnType, parameters.Select(static parameter => parameter.IsByRef ? parameter.Type.MakeByReferenceType() : parameter.Type).ToArray()))
            {
                ImplAttributes = MethodImplAttributes.Runtime
            };

            for (int i = 0; i < parameters.Length; i++)
            {
                invoke.ParameterDefinitions.Add(new ParameterDefinition((ushort)(i + 1), parameters[i].Name, parameters[i].Attributes));
            }

            type.Methods.Add(invoke);
        }

        TypeDefinition pair = new("Contoso", "Pair",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.WindowsRuntime,
            new TypeReference(module, corlib, "System", "ValueType"));

        pair.Fields.Add(new FieldDefinition("First", FieldAttributes.Public, module.CorLibTypeFactory.Int32));
        pair.Fields.Add(new FieldDefinition("Second", FieldAttributes.Public, module.CorLibTypeFactory.Int32));
        module.TopLevelTypes.Add(pair);

        AddDelegate("VoidHandler", "5d1c2f8e-3b7a-4c64-9e0a-1a2b3c4d5e01", module.CorLibTypeFactory.Void,
            ("sender", module.CorLibTypeFactory.Object, 0, false),
            ("value", module.CorLibTypeFactory.Int32, 0, false));

        AddDelegate("ResultHandler", "5d1c2f8e-3b7a-4c64-9e0a-1a2b3c4d5e02", module.CorLibTypeFactory.Int32,
            ("value", module.CorLibTypeFactory.Int32, 0, false));

        AddDelegate("OutputHandler", "5d1c2f8e-3b7a-4c64-9e0a-1a2b3c4d5e03", module.CorLibTypeFactory.Boolean,
            ("value", module.CorLibTypeFactory.Int32, 0, false),
            ("text", module.CorLibTypeFactory.String, ParameterAttributes.Out, true),
            ("numbers", new SzArrayTypeSignature(module.CorLibTypeFactory.Int32), ParameterAttributes.Out, true));

        AddDelegate("VoidOutputHandler", "5d1c2f8e-3b7a-4c64-9e0a-1a2b3c4d5e04", module.CorLibTypeFactory.Void,
            ("text", module.CorLibTypeFactory.String, ParameterAttributes.Out, true));

        // Covers the other parameter categories (by-reference struct, and arrays passed or filled by the callee)
        AddDelegate("ParametersHandler", "5d1c2f8e-3b7a-4c64-9e0a-1a2b3c4d5e05", module.CorLibTypeFactory.Void,
            ("pair", pair.ToTypeSignature(true), ParameterAttributes.In, true),
            ("source", new SzArrayTypeSignature(module.CorLibTypeFactory.Int32), ParameterAttributes.In, false),
            ("destination", new SzArrayTypeSignature(module.CorLibTypeFactory.Int32), 0, false));

        string path = Path.Combine(directory, "Contoso.winmd");

        module.Write(path);

        return path;
    }
}
