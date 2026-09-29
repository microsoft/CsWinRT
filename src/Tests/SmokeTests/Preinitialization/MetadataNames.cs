// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace PreinitializationValidation;

// Shared by the assembly inventory and the MSTAT reader. Never load the inspected assemblies.
public sealed class MetadataNames : ISignatureTypeProvider<string, object>
{
    public static Dictionary<string, string> ReadFixedAddressTypes(string[] assemblyPaths)
    {
        var types = new Dictionary<string, string>(StringComparer.Ordinal);
        var assemblyPrefixes = new HashSet<string>(StringComparer.Ordinal);

        foreach (string path in assemblyPaths)
        {
            using var stream = File.OpenRead(path);
            using var pe = new PEReader(stream);
            MetadataReader reader = pe.GetMetadataReader();
            string assemblyName = reader.GetString(reader.GetAssemblyDefinition().Name);
            const string privatePrefix = "System.Private.";
            string prefix = Sanitize(assemblyName.StartsWith(privatePrefix, StringComparison.Ordinal)
                ? "S.P." + assemblyName.Substring(privatePrefix.Length)
                : assemblyName) + "_";
            if (!assemblyPrefixes.Add(prefix))
            {
                throw new InvalidDataException($"Ambiguous native assembly prefix '{prefix}' for '{path}'.");
            }

            var usedNames = new HashSet<string>(StringComparer.Ordinal);
            int count = 0;

            // NativeAotNameMangler reserves names for every TypeDef in metadata order, even unused
            // types without fixed-address fields. Those can change a table owner's collision suffix.
            foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
            {
                TypeDefinition type = reader.GetTypeDefinition(handle);
                string baseName = prefix + Sanitize(GetFullNameForMangling(reader, handle));
                string nativeName = baseName;
                for (int suffix = 0; !usedNames.Add(nativeName); suffix++)
                {
                    nativeName = baseName + "_" + suffix.ToString(CultureInfo.InvariantCulture);
                }

                foreach (FieldDefinitionHandle fieldHandle in type.GetFields())
                {
                    FieldDefinition field = reader.GetFieldDefinition(fieldHandle);
                    if ((field.Attributes & FieldAttributes.Static) == 0)
                    {
                        continue;
                    }

                    foreach (CustomAttributeHandle attributeHandle in field.GetCustomAttributes())
                    {
                        CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
                        EntityHandle owner = attribute.Constructor.Kind switch
                        {
                            HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent,
                            HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
                            _ => throw new BadImageFormatException("Unexpected custom attribute constructor.")
                        };

                        string attributeName = GetTypeName(reader, owner);
                        if (attributeName.Substring(attributeName.IndexOf(']') + 1) ==
                            "System.Runtime.CompilerServices.FixedAddressValueTypeAttribute")
                        {
                            types[GetTypeName(reader, handle)] = "?__NONGCSTATICS@" + nativeName + "@@";
                            count++;
                        }
                    }
                }
            }

            if (count == 0)
            {
                throw new InvalidDataException($"'{path}' has no FixedAddressValueType fields. Expected an implementation assembly.");
            }
        }

        return types;
    }

    private static string GetFullNameForMangling(MetadataReader reader, TypeDefinitionHandle handle)
    {
        TypeDefinition type = reader.GetTypeDefinition(handle);
        string ns = reader.GetString(type.Namespace);
        string name = reader.GetString(type.Name);
        string fullName = ns.Length == 0 ? name : ns + "." + name;
        TypeDefinitionHandle parent = type.GetDeclaringType();
        return parent.IsNil ? fullName : GetFullNameForMangling(reader, parent) + "_" + fullName;
    }

    private static string Sanitize(string name)
    {
        var result = new StringBuilder(name.Length + 1);
        if (name.Length > 0 && name[0] is >= '0' and <= '9')
        {
            result.Append('_');
        }

        foreach (char c in name)
        {
            result.Append(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' ? c : '_');
        }

        return result.ToString();
    }

    public static string GetTypeName(MetadataReader reader, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
                TypeDefinition definition = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
                string definitionName = reader.GetString(definition.Name);
                TypeDefinitionHandle parent = definition.GetDeclaringType();
                if (!parent.IsNil)
                {
                    return GetTypeName(reader, parent) + "+" + definitionName;
                }

                return Qualify(reader.GetString(reader.GetAssemblyDefinition().Name),
                    reader.GetString(definition.Namespace), definitionName);

            case HandleKind.TypeReference:
                TypeReference reference = reader.GetTypeReference((TypeReferenceHandle)handle);
                string referenceName = reader.GetString(reference.Name);
                if (reference.ResolutionScope.Kind == HandleKind.TypeReference)
                {
                    return GetTypeName(reader, reference.ResolutionScope) + "+" + referenceName;
                }

                if (reference.ResolutionScope.Kind != HandleKind.AssemblyReference)
                {
                    throw new BadImageFormatException($"Unsupported type reference scope: {reference.ResolutionScope.Kind}.");
                }

                AssemblyReference assembly = reader.GetAssemblyReference((AssemblyReferenceHandle)reference.ResolutionScope);
                return Qualify(reader.GetString(assembly.Name), reader.GetString(reference.Namespace), referenceName);

            case HandleKind.TypeSpecification:
                return reader.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(new MetadataNames(), null);

            default:
                throw new BadImageFormatException($"Expected a type token, got {handle.Kind}.");
        }
    }

    private static string Qualify(string assembly, string ns, string name) =>
        "[" + assembly + "]" + (ns.Length == 0 ? name : ns + "." + name);

    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => GetTypeName(reader, handle);
    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => GetTypeName(reader, handle);
    public string GetTypeFromSpecification(MetadataReader reader, object context, TypeSpecificationHandle handle, byte rawTypeKind) => GetTypeName(reader, handle);

    // Compare generic owners by definition, so any surviving instantiation fails the check.
    public string GetGenericInstantiation(string genericType, System.Collections.Immutable.ImmutableArray<string> arguments) => genericType;
    public string GetGenericMethodParameter(object context, int index) => "!!" + index;
    public string GetGenericTypeParameter(object context, int index) => "!" + index;
    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
    public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[" + new string(',', shape.Rank - 1) + "]";
    public string GetSZArrayType(string elementType) => elementType + "[]";
    public string GetByReferenceType(string elementType) => elementType + "&";
    public string GetPointerType(string elementType) => elementType + "*";
    public string GetPinnedType(string elementType) => elementType;
    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
    public string GetFunctionPointerType(MethodSignature<string> signature) => signature.ReturnType + "(*)(" + string.Join(",", signature.ParameterTypes) + ")";
}
