using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace GuidPatch
{
    abstract record SignaturePart;

    enum SignatureType
    {
        i1,
        u1,
        i2,
        u2,
        i4,
        u4,
        i8,
        u8,
        f4,
        f8,
        b1,
        c2,
        g16,
        @string,
        iinspectable
    }

    record BasicSignaturePart(SignatureType Type) : SignaturePart;

    sealed record GuidSignature(Guid IID) : SignaturePart;
    
    sealed record CustomSignatureMethod(MethodReference Method) : SignaturePart;

    sealed record NonGenericDelegateSignature(Guid DelegateIID) : SignaturePart;
    
    sealed record UninstantiatedGeneric(GenericParameter OriginalGenericParameter) : SignaturePart;

    abstract record SignatureWithChildren(string GroupingName, string ThisEntitySignature, IEnumerable<SignaturePart> ChildrenSignatures) : SignaturePart;

    sealed record GenericSignature(Guid BaseGuid, IEnumerable<SignaturePart> GenericMemberSignatures) : 
        SignatureWithChildren("pinterface", BaseGuid.ToString("B"), GenericMemberSignatures);

    sealed record ValueTypeSignature(TypeReference Type, IEnumerable<SignaturePart> StructFieldSignatures) : 
        SignatureWithChildren("struct", Type.FullName, StructFieldSignatures);

    sealed record RuntimeClassSignature(TypeReference RuntimeClass, SignaturePart DefaultInterfaceSignature) : 
        SignatureWithChildren("rc", RuntimeClass.FullName, new[] { DefaultInterfaceSignature });

    sealed record EnumSignature(TypeReference Type, bool IsFlagEnum) : 
        SignatureWithChildren("enum", Type.FullName, new SignaturePart[] { new BasicSignaturePart(IsFlagEnum ? SignatureType.u4 : SignatureType.i4) });

    sealed class SignatureGenerator
    {
        private readonly AssemblyDefinition assembly;
        private readonly TypeDefinition guidAttributeType;
        private readonly AssemblyDefinition winRTRuntimeAssembly;

        public SignatureGenerator(AssemblyDefinition assembly, TypeDefinition guidAttributeType, AssemblyDefinition runtimeAssembly)
        {
            this.assembly = assembly;
            this.guidAttributeType = guidAttributeType;
            this.winRTRuntimeAssembly = runtimeAssembly;
        }

        public SignaturePart GetSignatureParts(TypeReference type)
        {
            if (type.IsGenericParameter)
            {
                return new UninstantiatedGeneric((GenericParameter)type);
            }

            var typeDef = type.Resolve();

            var helperType = new TypeReference($"ABI.{typeDef.Namespace}", typeDef.Name, typeDef.Module, assembly.MainModule);
            if (helperType.Resolve() is not null ||
                // Handle custom mapped built-in structs such as System.Numerics.Vector3 which have their ABI type defined in WinRT.Runtime.
                // This is handled separately due to the need for the is public check which isn't needed if in same module as in the initial case.
                ((helperType = typeDef.GetCswinrtAbiTypeDefinition(winRTRuntimeAssembly)) is not null && 
                  ((TypeDefinition)helperType).Attributes.HasFlag(TypeAttributes.Public)))
            {
                if (type.IsGenericInstance)
                {
                    var helperTypeGeneric = new GenericInstanceType(helperType);
                    foreach (var arg in ((GenericInstanceType)type).GenericArguments)
                    {
                        helperTypeGeneric.GenericArguments.Add(arg);
                    }
                    helperType = helperTypeGeneric;
                }

                var getGuidSignatureMethod = new MethodReference("GetGuidSignature", assembly.MainModule.TypeSystem.String, helperType)
                {
                    HasThis = false
                };

                if (getGuidSignatureMethod.Resolve() is not null)
                {
                    return new CustomSignatureMethod(assembly.MainModule.ImportReference(getGuidSignatureMethod));
                }
            }

            type = typeDef.IsInterface ? (CreateAuthoringMetadataTypeReference(type).Resolve() ?? type) : type;
            if (typeDef == assembly.MainModule.TypeSystem.Object.Resolve())
            {
                return new BasicSignaturePart(SignatureType.iinspectable);
            }

            if (type.IsGenericInstance)
            {
                List<SignaturePart> signatureParts = new();

                foreach (var arg in ((GenericInstanceType)type).GenericArguments)
                {
                    signatureParts.Add(GetSignatureParts(arg));
                }

                Guid? baseGuid = type.ReadGuidFromAttribute(guidAttributeType, winRTRuntimeAssembly);
                if (baseGuid == null)
                {
                    throw new InvalidOperationException();
                }
                return new GenericSignature(baseGuid.Value, signatureParts);
            }

            if (type.IsValueType)
            {
                switch (type.Name)
                {
                    case "SByte": return new BasicSignaturePart(SignatureType.i1);
                    case "Byte": return new BasicSignaturePart(SignatureType.u1);
                    case "Int16": return new BasicSignaturePart(SignatureType.i2);
                    case "UInt16": return new BasicSignaturePart(SignatureType.u2);
                    case "Int32": return new BasicSignaturePart(SignatureType.i4);
                    case "UInt32": return new BasicSignaturePart(SignatureType.u4);
                    case "Int64": return new BasicSignaturePart(SignatureType.i8);
                    case "UInt64": return new BasicSignaturePart(SignatureType.u8);
                    case "Single": return new BasicSignaturePart(SignatureType.f4);
                    case "Double": return new BasicSignaturePart(SignatureType.f8);
                    case "Boolean": return new BasicSignaturePart(SignatureType.b1);
                    case "Char": return new BasicSignaturePart(SignatureType.c2);
                    case "Guid": return new BasicSignaturePart(SignatureType.g16);
                    default:
                        {
                            if (typeDef.IsEnum)
                            {
                                var isFlags = typeDef.CustomAttributes.Any(cad => string.CompareOrdinal(cad.AttributeType.Name, "FlagsAttribute") == 0);
                                return new EnumSignature(type, isFlags);
                            }
                            if (!type.IsPrimitive)
                            {
                                var args = type.Resolve().Fields.Where(f => f.IsPublic && !f.IsStatic).Select(fi =>
                                    GetSignatureParts(
                                        assembly.MainModule.ImportReference(new FieldReference(fi.Name, fi.FieldType, type)).FieldType)).ToArray();
                                return new ValueTypeSignature(type, args);
                            }
                            throw new InvalidOperationException("unsupported value type");
                        }
                }
            }

            if (typeDef == assembly.MainModule.TypeSystem.String.Resolve())
            {
                return new BasicSignaturePart(SignatureType.@string);
            }

            if (TryGetDefaultInterfaceTypeForRuntimeClassType(type, out TypeReference? iface))
            {
                return new RuntimeClassSignature(type, GetSignatureParts(iface));
            }

            Guid? guidAttributeValue = type.ReadGuidFromAttribute(guidAttributeType, winRTRuntimeAssembly);
            if (guidAttributeValue == null)
            {
                throw new InvalidOperationException($"Unable to read IID attribute value for {type.FullName}.");
            }

            if (string.CompareOrdinal(typeDef.BaseType?.Name, "MulticastDelegate") == 0)
            {
                return new NonGenericDelegateSignature(guidAttributeValue.Value);
            }
            return new GuidSignature(guidAttributeValue.Value);
        }

        private TypeReference CreateAuthoringMetadataTypeReference(TypeReference type)
        {
            return new TypeReference($"ABI.Impl.{type.Namespace}", type.Name, type.Module, assembly.MainModule);
        }

        bool TryGetDefaultInterfaceTypeForRuntimeClassType(TypeReference runtimeClassTypeMaybe, [NotNullWhen(true)] out TypeReference? defaultInterface)
        {
            defaultInterface = null;

            TypeDefinition rcDef = runtimeClassTypeMaybe.Resolve();
            rcDef = CreateAuthoringMetadataTypeReference(rcDef).Resolve() ?? rcDef;

            CustomAttribute? runtimeClassAttribute = rcDef.CustomAttributes.FirstOrDefault(ca =>
                string.CompareOrdinal(ca.AttributeType.Namespace, "WinRT") == 0 && 
                string.CompareOrdinal(ca.AttributeType.Name, "ProjectedRuntimeClassAttribute") == 0);

            if (runtimeClassAttribute is null)
            {
                return false;
            }

            if (runtimeClassAttribute.ConstructorArguments[0].Value is TypeReference typeReference)
            {
                defaultInterface = typeReference;
                return true;
            }

            string defaultInterfacePropertyName = (string)runtimeClassAttribute.ConstructorArguments[0].Value;

            var defaultInterfaceProperty = rcDef.Properties.FirstOrDefault(prop => prop.Name == defaultInterfacePropertyName);

            if (defaultInterfaceProperty is null)
            {
                return false;
            }

            defaultInterface = defaultInterfaceProperty.PropertyType;
            return true;
        }
    }
}
