using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace CsWinRT.SmokeTests
{
    // Loaded by the PowerShell harness, not by a consuming application. Never execute a reference
    // assembly: read the actual packed metadata, including policy entries for compiler-pruned types.
    public sealed class ExclusiveToProjectionMetadata
    {
        public bool HasReferenceAttribute { get; private set; }

        public int MetadataAttributeCount { get; private set; }

        public string[] SelectedInterfaces { get; private set; } = Array.Empty<string>();

        public HashSet<string> UnknownMetadataKeys { get; } = new HashSet<string>(StringComparer.Ordinal);

        public Dictionary<string, bool> Interfaces { get; } = new Dictionary<string, bool>(StringComparer.Ordinal);

        public HashSet<string> DynamicInterfaces { get; } = new HashSet<string>(StringComparer.Ordinal);

        public static ExclusiveToProjectionMetadata Read(Stream stream)
        {
            using (var pe = new PEReader(stream, PEStreamOptions.LeaveOpen))
            {
                MetadataReader reader = pe.GetMetadataReader();
                var result = new ExclusiveToProjectionMetadata();
                var selectedInterfaces = new List<string>();

                foreach (CustomAttributeHandle handle in reader.GetAssemblyDefinition().GetCustomAttributes())
                {
                    CustomAttribute attribute = reader.GetCustomAttribute(handle);
                    string attributeName = GetAttributeName(reader, attribute);

                    if (attributeName == "WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyAttribute")
                    {
                        result.HasReferenceAttribute = true;
                        continue;
                    }

                    if (attributeName != "WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyMetadataAttribute")
                    {
                        continue;
                    }

                    result.MetadataAttributeCount++;

                    var arguments = attribute.DecodeValue(new AttributeTypeProvider()).FixedArguments;

                    if (arguments.Length != 2 || arguments[0].Value is not string key)
                    {
                        throw new InvalidDataException("Reference assembly metadata must have a string key and a nullable string value.");
                    }

                    if (key != "CsWinRT.IdicExclusiveTo.v1")
                    {
                        result.UnknownMetadataKeys.Add(key);
                        continue;
                    }

                    if (arguments[1].Value is not string interfaceName || string.IsNullOrWhiteSpace(interfaceName))
                    {
                        throw new InvalidDataException("Exclusive-interface metadata must name an interface.");
                    }

                    selectedInterfaces.Add(interfaceName);
                }

                result.SelectedInterfaces = selectedInterfaces.ToArray();

                foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
                {
                    TypeDefinition type = reader.GetTypeDefinition(handle);

                    if ((type.Attributes & TypeAttributes.Interface) == 0)
                    {
                        continue;
                    }

                    result.Interfaces.Add(GetTypeName(reader, handle), (type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public);

                    foreach (CustomAttributeHandle attributeHandle in type.GetCustomAttributes())
                    {
                        if (GetAttributeName(reader, reader.GetCustomAttribute(attributeHandle)) !=
                            "System.Runtime.InteropServices.DynamicInterfaceCastableImplementationAttribute")
                        {
                            continue;
                        }

                        foreach (InterfaceImplementationHandle implementationHandle in type.GetInterfaceImplementations())
                        {
                            EntityHandle interfaceType = reader.GetInterfaceImplementation(implementationHandle).Interface;

                            if (interfaceType.Kind == HandleKind.TypeDefinition || interfaceType.Kind == HandleKind.TypeReference)
                            {
                                result.DynamicInterfaces.Add(GetTypeName(reader, interfaceType));
                            }
                        }
                    }
                }

                return result;
            }
        }

        private static string GetAttributeName(MetadataReader reader, CustomAttribute attribute)
        {
            EntityHandle type = attribute.Constructor.Kind == HandleKind.MemberReference
                ? reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent
                : reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();

            return GetTypeName(reader, type);
        }

        private static string GetTypeName(MetadataReader reader, EntityHandle handle)
        {
            if (handle.Kind == HandleKind.TypeDefinition)
            {
                TypeDefinition type = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
                TypeDefinitionHandle declaringType = type.GetDeclaringType();

                return declaringType.IsNil
                    ? reader.GetString(type.Namespace) + "." + reader.GetString(type.Name)
                    : GetTypeName(reader, declaringType) + "+" + reader.GetString(type.Name);
            }

            if (handle.Kind == HandleKind.TypeReference)
            {
                TypeReference type = reader.GetTypeReference((TypeReferenceHandle)handle);

                return type.ResolutionScope.Kind == HandleKind.TypeReference
                    ? GetTypeName(reader, type.ResolutionScope) + "+" + reader.GetString(type.Name)
                    : reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
            }

            return "";
        }

        private sealed class AttributeTypeProvider : ICustomAttributeTypeProvider<string>
        {
            public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();

            public string GetSystemType() => "System.Type";

            public bool IsSystemType(string type) => type == "System.Type";

            public string GetSZArrayType(string elementType) => elementType + "[]";

            public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => GetTypeName(reader, handle);

            public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => GetTypeName(reader, handle);

            public string GetTypeFromSerializedName(string name) => name;

            public PrimitiveTypeCode GetUnderlyingEnumType(string type) => throw new NotSupportedException(type);
        }
    }
}
