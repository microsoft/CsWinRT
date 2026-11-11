// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

namespace WindowsRuntime.SourceGenerator.Tests;

/// <summary>
/// Computes the IID and signature of Windows Runtime types directly from <c>.winmd</c> metadata, using only Windows Runtime names.
/// </summary>
/// <remarks>
/// This is an independent oracle for the audit tests: it shares no code with the projection writer, the interop generator,
/// or the interceptor generator, so it doesn't depend on any of their type mapping or signature logic.
/// </remarks>
internal sealed class WinMdIidOracle : IDisposable
{
    private const string IVectorIid = "{913337e9-11a1-4345-a3a2-4e7f956e222d}";

    private sealed record Entry(MetadataReader Reader, TypeDefinitionHandle Handle);

    private readonly bool _useWindowsUIXaml;
    private readonly List<PEReader> _peReaders = [];
    private readonly Dictionary<string, Entry> _types = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _definitionSignatures = new(StringComparer.Ordinal);

    /// <summary>
    /// Creates a new <see cref="WinMdIidOracle"/> instance.
    /// </summary>
    /// <param name="useWindowsUIXaml">Whether custom-mapped XAML types refer to UWP XAML (rather than WinUI) types.</param>
    public WinMdIidOracle(bool useWindowsUIXaml)
    {
        _useWindowsUIXaml = useWindowsUIXaml;
    }

    /// <summary>
    /// Gets the Windows Runtime type a .NET type that is custom-mapped to it is referenced as in the SDK metadata.
    /// </summary>
    /// <remarks>
    /// The SDK metadata refers to custom-mapped types by their .NET names. Only the names are mapped here: the IIDs and
    /// signatures are always read from the Windows Runtime type definitions in the metadata.
    /// </remarks>
    private string GetWindowsRuntimeName(string name)
    {
        string xaml = _useWindowsUIXaml ? "Windows.UI.Xaml" : "Microsoft.UI.Xaml";

        return name switch
        {
            "System.Collections.Generic.IEnumerable`1" => "Windows.Foundation.Collections.IIterable`1",
            "System.Collections.Generic.IEnumerator`1" => "Windows.Foundation.Collections.IIterator`1",
            "System.Collections.Generic.IList`1" => "Windows.Foundation.Collections.IVector`1",
            "System.Collections.Generic.IReadOnlyList`1" => "Windows.Foundation.Collections.IVectorView`1",
            "System.Collections.Generic.IDictionary`2" => "Windows.Foundation.Collections.IMap`2",
            "System.Collections.Generic.IReadOnlyDictionary`2" => "Windows.Foundation.Collections.IMapView`2",
            "System.Collections.Generic.KeyValuePair`2" => "Windows.Foundation.Collections.IKeyValuePair`2",
            "System.Nullable`1" => "Windows.Foundation.IReference`1",
            "System.EventHandler`1" => "Windows.Foundation.EventHandler`1",
            "System.IDisposable" => "Windows.Foundation.IClosable",
            "System.DateTimeOffset" => "Windows.Foundation.DateTime",
            "System.TimeSpan" => "Windows.Foundation.TimeSpan",
            "System.Exception" => "Windows.Foundation.HResult",
            "System.Uri" => "Windows.Foundation.Uri",
            "System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken" => "Windows.Foundation.EventRegistrationToken",
            "System.Numerics.Matrix3x2" or "System.Numerics.Matrix4x4" or "System.Numerics.Plane" or "System.Numerics.Quaternion" or
            "System.Numerics.Vector2" or "System.Numerics.Vector3" or "System.Numerics.Vector4" => $"Windows.Foundation.Numerics.{name["System.Numerics.".Length..]}",
            "System.Type" => $"{xaml}.Interop.TypeName",
            "System.Windows.Input.ICommand" => $"{xaml}.Input.ICommand",
            "System.Collections.IEnumerable" => $"{xaml}.Interop.IBindableIterable",
            "System.Collections.IList" => $"{xaml}.Interop.IBindableVector",
            "System.Collections.Specialized.INotifyCollectionChanged" => $"{xaml}.Interop.INotifyCollectionChanged",
            "System.Collections.Specialized.NotifyCollectionChangedAction" => $"{xaml}.Interop.NotifyCollectionChangedAction",
            "System.Collections.Specialized.NotifyCollectionChangedEventArgs" => $"{xaml}.Interop.NotifyCollectionChangedEventArgs",
            "System.Collections.Specialized.NotifyCollectionChangedEventHandler" => $"{xaml}.Interop.NotifyCollectionChangedEventHandler",
            "System.ComponentModel.INotifyPropertyChanged" => $"{xaml}.Data.INotifyPropertyChanged",
            "System.ComponentModel.PropertyChangedEventArgs" => $"{xaml}.Data.PropertyChangedEventArgs",
            "System.ComponentModel.PropertyChangedEventHandler" => $"{xaml}.Data.PropertyChangedEventHandler",
            "System.ComponentModel.INotifyDataErrorInfo" => $"{xaml}.Data.INotifyDataErrorInfo",
            "System.ComponentModel.DataErrorsChangedEventArgs" => $"{xaml}.Data.DataErrorsChangedEventArgs",
            _ => name
        };
    }

    /// <summary>
    /// Adds all the types of a <c>.winmd</c> file. Types from earlier files win over same-named types from later ones.
    /// </summary>
    /// <param name="path">The path of the <c>.winmd</c> file.</param>
    public void Add(string path)
    {
        PEReader peReader = new(File.OpenRead(path));

        _peReaders.Add(peReader);

        MetadataReader reader = peReader.GetMetadataReader();

        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            string name = GetFullName(reader, reader.GetTypeDefinition(handle));

            if (name != "<Module>")
            {
                _ = _types.TryAdd(name, new(reader, handle));
            }
        }
    }

    /// <summary>
    /// Gets the names of all instantiable runtime classes that have a default interface.
    /// </summary>
    /// <returns>The fully qualified names of the classes.</returns>
    public IEnumerable<string> GetRuntimeClassNames()
    {
        foreach ((string name, Entry entry) in _types)
        {
            TypeDefinition type = entry.Reader.GetTypeDefinition(entry.Handle);

            if (GetKind(entry.Reader, type) == Kind.Class &&
                (type.Attributes & (System.Reflection.TypeAttributes.Abstract | System.Reflection.TypeAttributes.Sealed)) != (System.Reflection.TypeAttributes.Abstract | System.Reflection.TypeAttributes.Sealed) &&
                TryGetDefaultInterface(entry.Reader, type, out _))
            {
                yield return name;
            }
        }
    }

    /// <summary>
    /// Gets the signature of a runtime class (<c>rc(name;defaultInterfaceSignature)</c>).
    /// </summary>
    /// <param name="className">The fully qualified name of the class.</param>
    /// <returns>The signature of the class.</returns>
    public string GetClassSignature(string className)
    {
        Entry entry = _types[className];

        return GetDefinitionSignature(entry.Reader, entry.Handle);
    }

    /// <summary>
    /// Gets the IID of a runtime class, which is the IID of its default interface.
    /// </summary>
    /// <param name="className">The fully qualified name of the class.</param>
    /// <returns>The IID of the class.</returns>
    public Guid GetClassIid(string className)
    {
        Entry entry = _types[className];

        _ = TryGetDefaultInterface(entry.Reader, entry.Reader.GetTypeDefinition(entry.Handle), out EntityHandle defaultInterface);

        string signature = GetSignature(entry.Reader, defaultInterface);

        return signature.StartsWith('{') ? Guid.ParseExact(signature, "B") : ComputeIid(signature);
    }

    /// <summary>
    /// Computes the IID of a parameterized type from its signature.
    /// </summary>
    /// <param name="signature">The signature of the type.</param>
    /// <returns>The IID of the type.</returns>
    public static Guid ComputeIid(string signature)
    {
        byte[] namespaceBytes = new Guid("11f47ad5-7b73-42c0-abae-878b1e16adee").ToByteArray(bigEndian: true);
        byte[] digest = SHA1.HashData([.. namespaceBytes, .. Encoding.UTF8.GetBytes(signature)]);

        digest[6] = (byte)((digest[6] & 0x0F) | 0x50);
        digest[8] = (byte)((digest[8] & 0x3F) | 0x80);

        return new Guid(digest.AsSpan(0, 16), bigEndian: true);
    }

    /// <summary>
    /// Gets the signature of the <c>IVector&lt;T&gt;</c> of a given signature.
    /// </summary>
    /// <param name="elementSignature">The signature of the element type.</param>
    /// <returns>The signature of the vector.</returns>
    public static string GetVectorSignature(string elementSignature)
    {
        return $"pinterface({IVectorIid};{elementSignature})";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (PEReader peReader in _peReaders)
        {
            peReader.Dispose();
        }
    }

    private enum Kind
    {
        Class,
        Interface,
        Delegate,
        Enum,
        Struct,
        Attribute
    }

    private static string Join(MetadataReader reader, StringHandle @namespace, StringHandle name)
    {
        string text = reader.GetString(name);

        return @namespace.IsNil || reader.GetString(@namespace).Length == 0 ? text : $"{reader.GetString(@namespace)}.{text}";
    }

    private static string GetFullName(MetadataReader reader, TypeDefinition type)
    {
        return Join(reader, type.Namespace, type.Name);
    }

    private static string GetTypeName(MetadataReader reader, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeReference:
                TypeReference reference = reader.GetTypeReference((TypeReferenceHandle)handle);
                return Join(reader, reference.Namespace, reference.Name);
            case HandleKind.TypeDefinition:
                return GetFullName(reader, reader.GetTypeDefinition((TypeDefinitionHandle)handle));
            default:
                return "";
        }
    }

    private static Kind GetKind(MetadataReader reader, TypeDefinition type)
    {
        if ((type.Attributes & System.Reflection.TypeAttributes.Interface) != 0)
        {
            return Kind.Interface;
        }

        return GetTypeName(reader, type.BaseType) switch
        {
            "System.Enum" => Kind.Enum,
            "System.ValueType" => Kind.Struct,
            "System.MulticastDelegate" => Kind.Delegate,
            "System.Attribute" => Kind.Attribute,
            _ => Kind.Class
        };
    }

    private static string GetAttributeTypeName(MetadataReader reader, CustomAttribute attribute)
    {
        EntityHandle parent = attribute.Constructor.Kind switch
        {
            HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent,
            HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
            _ => default
        };

        return GetTypeName(reader, parent);
    }

    private static bool TryGetDefaultInterface(MetadataReader reader, TypeDefinition type, out EntityHandle defaultInterface)
    {
        foreach (InterfaceImplementationHandle handle in type.GetInterfaceImplementations())
        {
            InterfaceImplementation implementation = reader.GetInterfaceImplementation(handle);

            foreach (CustomAttributeHandle attributeHandle in implementation.GetCustomAttributes())
            {
                if (GetAttributeTypeName(reader, reader.GetCustomAttribute(attributeHandle)) == "Windows.Foundation.Metadata.DefaultAttribute")
                {
                    defaultInterface = implementation.Interface;

                    return true;
                }
            }
        }

        defaultInterface = default;

        return false;
    }

    private static Guid GetGuid(MetadataReader reader, TypeDefinition type)
    {
        foreach (CustomAttributeHandle handle in type.GetCustomAttributes())
        {
            CustomAttribute attribute = reader.GetCustomAttribute(handle);

            if (GetAttributeTypeName(reader, attribute) == "Windows.Foundation.Metadata.GuidAttribute")
            {
                BlobReader blob = reader.GetBlobReader(attribute.Value);

                _ = blob.ReadUInt16();

                uint a = blob.ReadUInt32();
                ushort b = blob.ReadUInt16();
                ushort c = blob.ReadUInt16();

                return new Guid((int)a, (short)b, (short)c, blob.ReadByte(), blob.ReadByte(), blob.ReadByte(), blob.ReadByte(), blob.ReadByte(), blob.ReadByte(), blob.ReadByte(), blob.ReadByte());
            }
        }

        throw new InvalidOperationException($"The type '{GetFullName(reader, type)}' has no IID.");
    }

    private string GetSignature(MetadataReader reader, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
                return GetDefinitionSignature(reader, (TypeDefinitionHandle)handle);
            case HandleKind.TypeReference:
                string name = GetWindowsRuntimeName(GetTypeName(reader, handle));

                if (name == "System.Guid")
                {
                    return "g16";
                }

                return _types.TryGetValue(name, out Entry? entry)
                    ? GetDefinitionSignature(entry.Reader, entry.Handle)
                    : throw new InvalidOperationException($"The type '{name}' was not found in the loaded metadata.");
            case HandleKind.TypeSpecification:
                return reader.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(new Provider(this), null);
            default:
                throw new NotSupportedException($"Unsupported handle kind '{handle.Kind}'.");
        }
    }

    private string GetDefinitionSignature(MetadataReader reader, TypeDefinitionHandle handle)
    {
        TypeDefinition type = reader.GetTypeDefinition(handle);
        string name = GetFullName(reader, type);

        if (_definitionSignatures.TryGetValue(name, out string? signature))
        {
            return signature;
        }

        switch (GetKind(reader, type))
        {
            case Kind.Interface:
                signature = GetGuid(reader, type).ToString("B");
                break;
            case Kind.Delegate:
                signature = $"delegate({GetGuid(reader, type):B})";
                break;
            case Kind.Enum:
                FieldDefinition field = reader.GetFieldDefinition(type.GetFields().First());
                signature = $"enum({name};{field.DecodeSignature(new Provider(this), null)})";
                break;
            case Kind.Struct:
                List<string> fields = [];

                foreach (FieldDefinitionHandle fieldHandle in type.GetFields())
                {
                    FieldDefinition structField = reader.GetFieldDefinition(fieldHandle);

                    if ((structField.Attributes & System.Reflection.FieldAttributes.Static) == 0)
                    {
                        fields.Add(structField.DecodeSignature(new Provider(this), null));
                    }
                }

                signature = $"struct({name};{string.Join(";", fields)})";
                break;
            case Kind.Class when TryGetDefaultInterface(reader, type, out EntityHandle defaultInterface):
                signature = $"rc({name};{GetSignature(reader, defaultInterface)})";
                break;
            default:
                throw new NotSupportedException($"The type '{name}' has no signature.");
        }

        _definitionSignatures.Add(name, signature);

        return signature;
    }

    private sealed class Provider(WinMdIidOracle oracle) : ISignatureTypeProvider<string, object?>
    {
        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
        {
            PrimitiveTypeCode.Boolean => "b1",
            PrimitiveTypeCode.Char => "c2",
            PrimitiveTypeCode.SByte => "i1",
            PrimitiveTypeCode.Byte => "u1",
            PrimitiveTypeCode.Int16 => "i2",
            PrimitiveTypeCode.UInt16 => "u2",
            PrimitiveTypeCode.Int32 => "i4",
            PrimitiveTypeCode.UInt32 => "u4",
            PrimitiveTypeCode.Int64 => "i8",
            PrimitiveTypeCode.UInt64 => "u8",
            PrimitiveTypeCode.Single => "f4",
            PrimitiveTypeCode.Double => "f8",
            PrimitiveTypeCode.String => "string",
            PrimitiveTypeCode.Object => "cinterface(IInspectable)",
            _ => throw new NotSupportedException($"Unsupported primitive type '{typeCode}'.")
        };

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => oracle.GetSignature(reader, handle);

        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => oracle.GetSignature(reader, handle);

        public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => oracle.GetSignature(reader, handle);

        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
        {
            // The generic definition is either '{iid}' or 'delegate({iid})'
            string iid = genericType.StartsWith("delegate(", StringComparison.Ordinal) ? genericType["delegate(".Length..^1] : genericType;

            return $"pinterface({iid};{string.Join(";", typeArguments)})";
        }

        public string GetSZArrayType(string elementType) => $"pinterface({{61c17707-2d65-11e0-9ae8-d48564015472}};{elementType})";

        public string GetArrayType(string elementType, ArrayShape shape) => throw new NotSupportedException();

        public string GetByReferenceType(string elementType) => throw new NotSupportedException();

        public string GetPointerType(string elementType) => throw new NotSupportedException();

        public string GetFunctionPointerType(MethodSignature<string> signature) => throw new NotSupportedException();

        public string GetGenericMethodParameter(object? genericContext, int index) => throw new NotSupportedException();

        public string GetGenericTypeParameter(object? genericContext, int index) => throw new NotSupportedException();

        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;

        public string GetPinnedType(string elementType) => throw new NotSupportedException();
    }
}
