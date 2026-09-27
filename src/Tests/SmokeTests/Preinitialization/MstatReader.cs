// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Xml;

namespace PreinitializationValidation;

public sealed class MstatReader
{
    public Dictionary<string, string> Constructors { get; } = new(StringComparer.Ordinal);

    public int MethodCount { get; private set; }

    public int TypeCount { get; private set; }

    // MSTAT 2.2 stores records as synthetic IL, not text or ordinary executable method bodies.
    // Contract: dotnet/runtime v10.0.0, ILCompiler.Compiler/Compiler/MstatObjectDumper.cs.
    public static MstatReader Read(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        MetadataReader reader = pe.GetMetadataReader();
        Version version = reader.GetAssemblyDefinition().Version;
        if (version.Major != 2 || version.Minor != 2)
        {
            throw new InvalidDataException($"Unsupported MSTAT format {version} in '{path}'; expected 2.2.");
        }

        var streams = new Dictionary<string, MethodDefinition>(StringComparer.Ordinal);
        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeDefinition type = reader.GetTypeDefinition(handle);
            if (reader.GetString(type.Name) != "<Module>")
            {
                continue;
            }

            foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
            {
                MethodDefinition method = reader.GetMethodDefinition(methodHandle);
                streams.Add(reader.GetString(method.Name), method);
            }
        }

        foreach (string name in new[] { "Methods", "Types", "DeduplicatedMethods" })
        {
            if (!streams.ContainsKey(name))
            {
                throw new InvalidDataException($"MSTAT is missing its '{name}' record stream.");
            }
        }

        BlobReader names = pe.GetSectionData(".names").GetReader();
        var result = new MstatReader();
        BlobReader methods = pe.GetMethodBody(streams["Methods"].RelativeVirtualAddress).GetILReader();
        while (methods.RemainingBytes > 0)
        {
            var method = ReadMethod(reader, ReadToken(ref methods));
            int codeSize = ReadNonnegativeInt(ref methods);
            _ = ReadNonnegativeInt(ref methods); // GC info.
            _ = ReadNonnegativeInt(ref methods); // EH info.
            ValidateName(names, ReadNonnegativeInt(ref methods));
            result.MethodCount++;
            if (method.Name == ".cctor")
            {
                result.Constructors[method.Owner] = $"{codeSize} native bytes";
            }
        }

        BlobReader types = pe.GetMethodBody(streams["Types"].RelativeVirtualAddress).GetILReader();
        while (types.RemainingBytes > 0)
        {
            _ = MetadataNames.GetTypeName(reader, ReadToken(ref types));
            _ = ReadNonnegativeInt(ref types);
            ValidateName(names, ReadNonnegativeInt(ref types));
            result.TypeCount++;
        }

        // A folded constructor still executes; absence from the ordinary Methods stream is not enough.
        BlobReader aliases = pe.GetMethodBody(streams["DeduplicatedMethods"].RelativeVirtualAddress).GetILReader();
        while (aliases.RemainingBytes > 0)
        {
            var method = ReadMethod(reader, ReadToken(ref aliases));
            int count = ReadNonnegativeInt(ref aliases);
            if (count == 0)
            {
                throw new InvalidDataException("A folded MSTAT method has no target.");
            }

            for (int i = 0; i < count; i++)
            {
                _ = ReadMethod(reader, ReadToken(ref aliases));
                ValidateName(names, ReadNonnegativeInt(ref aliases));
            }

            if (method.Name == ".cctor")
            {
                result.Constructors[method.Owner] = "folded native body";
            }
        }

        if (result.MethodCount == 0 || result.TypeCount == 0)
        {
            throw new InvalidDataException("MSTAT contains no native methods or types.");
        }

        return result;
    }

    public static Dictionary<string, int> ReadStaticData(string path)
    {
        using XmlReader reader = XmlReader.Create(path, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        });
        if (reader.MoveToContent() != XmlNodeType.Element || reader.Name != "ObjectNodes")
        {
            throw new InvalidDataException($"'{path}' is not an ILC XML map.");
        }

        var data = new Dictionary<string, int>(StringComparer.Ordinal);
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.Depth != 1 || reader.Name != "NonGCStatics")
            {
                continue;
            }

            string name = reader.GetAttribute("Name");
            if (string.IsNullOrEmpty(name) ||
                !int.TryParse(reader.GetAttribute("Length"), NumberStyles.None, CultureInfo.InvariantCulture, out int size))
            {
                throw new InvalidDataException("Invalid NonGCStatics record in the ILC XML map.");
            }

            data.Add(name, size);
        }

        if (data.Count == 0)
        {
            throw new InvalidDataException("The ILC XML map contains no NonGCStatics records.");
        }

        return data;
    }

    private static (string Owner, string Name) ReadMethod(MetadataReader reader, EntityHandle handle)
    {
        if (handle.Kind == HandleKind.MethodSpecification)
        {
            handle = reader.GetMethodSpecification((MethodSpecificationHandle)handle).Method;
        }

        if (handle.Kind == HandleKind.MemberReference)
        {
            MemberReference method = reader.GetMemberReference((MemberReferenceHandle)handle);
            if (method.GetKind() != MemberReferenceKind.Method)
            {
                throw new InvalidDataException("Expected a method in the MSTAT method stream.");
            }

            return (MetadataNames.GetTypeName(reader, method.Parent), reader.GetString(method.Name));
        }

        if (handle.Kind == HandleKind.MethodDefinition)
        {
            MethodDefinition method = reader.GetMethodDefinition((MethodDefinitionHandle)handle);
            return (MetadataNames.GetTypeName(reader, method.GetDeclaringType()), reader.GetString(method.Name));
        }

        throw new InvalidDataException($"Unexpected MSTAT method token: {handle.Kind}.");
    }

    private static EntityHandle ReadToken(ref BlobReader reader)
    {
        if (reader.ReadByte() != 0xD0) // ldtoken
        {
            throw new InvalidDataException("Expected ldtoken in MSTAT.");
        }

        return MetadataTokens.EntityHandle(reader.ReadInt32());
    }

    private static int ReadNonnegativeInt(ref BlobReader reader)
    {
        byte opcode = reader.ReadByte();
        int value = opcode switch
        {
            >= 0x15 and <= 0x1E => opcode - 0x16,
            0x1F => reader.ReadSByte(),
            0x20 => reader.ReadInt32(),
            _ => throw new InvalidDataException($"Unexpected integer opcode 0x{opcode:X2} in MSTAT.")
        };
        if (value < 0)
        {
            throw new InvalidDataException("Negative size, count, or name offset in MSTAT.");
        }

        return value;
    }

    private static void ValidateName(BlobReader reader, int offset)
    {
        if (offset >= reader.Length)
        {
            throw new InvalidDataException("MSTAT name offset is outside the .names section.");
        }

        reader.Offset = offset;
        if (string.IsNullOrEmpty(reader.ReadSerializedString()))
        {
            throw new InvalidDataException("MSTAT has an empty node name.");
        }
    }
}
