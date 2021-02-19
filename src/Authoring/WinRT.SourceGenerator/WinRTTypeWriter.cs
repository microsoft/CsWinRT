using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;

namespace Generator
{
    class Parameter
    {
        public Symbol Type;
        public string Name;
        public ParameterAttributes Attributes;
        public bool ByRef;

        public Parameter(Symbol type, string name, ParameterAttributes attributes)
            : this(type, name, attributes, attributes == ParameterAttributes.Out)
        {
        }

        public Parameter(Symbol type, string name, ParameterAttributes attributes, bool byRef)
        {
            Type = type;
            Name = name;
            Attributes = attributes;
            ByRef = byRef;
        }

        public Parameter(ITypeSymbol type, string name, ParameterAttributes attributes)
            : this(new Symbol(type), name, attributes)
        {
        }

        public Parameter(EntityHandle type, string name, ParameterAttributes attributes)
            : this(new Symbol(type), name, attributes)
        {
        }

        public Parameter(IParameterSymbol parameterSymbol)
        {
            // Set out parameter attribute if write only array.
            bool isWriteOnlyArray = parameterSymbol.Type is IArrayTypeSymbol &&
                parameterSymbol.GetAttributes().Where(
                    attr => attr.AttributeClass.ToString() == "System.Runtime.InteropServices.WindowsRuntime.WriteOnlyArray"
                ).Count() != 0;

            Type = new Symbol(parameterSymbol.Type);
            Name = parameterSymbol.Name;
            Attributes = (parameterSymbol.RefKind == RefKind.Out || isWriteOnlyArray) ? ParameterAttributes.Out : ParameterAttributes.In;
            ByRef = parameterSymbol.RefKind == RefKind.Out;
        }

        public static Parameter[] GetParameters(ParameterListSyntax parameterList, SemanticModel model)
        {
            int numParameters = parameterList.Parameters.Count;
            Parameter[] parameters = new Parameter[numParameters];
            for (int idx = 0; idx < numParameters; idx++)
            {
                parameters[idx] = new Parameter(model.GetDeclaredSymbol(parameterList.Parameters[idx]));
            }

            return parameters;
        }

        public static Parameter[] GetParameters(IMethodSymbol method)
        {
            int numParameters = method.Parameters.Count();
            Parameter[] parameters = new Parameter[numParameters];
            for (int idx = 0; idx < numParameters; idx++)
            {
                parameters[idx] = new Parameter(method.Parameters[idx]);
            }

            return parameters;
        }
    }

    class Symbol
    {
        public ITypeSymbol Type;
        public EntityHandle Handle;
        public int GenericIndex;
        public bool IsArray;

        public Symbol(ITypeSymbol type, bool isArray = false)
        {
            Type = type;
            Handle = default;
            GenericIndex = -1;
            IsArray = isArray;
        }

        public Symbol(EntityHandle handle)
        {
            Type = default;
            Handle = handle;
            GenericIndex = -1;
        }

        public Symbol(int genericIndex, bool isArray)
        {
            Type = default;
            Handle = default;
            GenericIndex = genericIndex;
            IsArray = isArray;
        }

        public bool IsHandle()
        {
            return Handle != default;
        }

        public bool IsGeneric()
        {
            return GenericIndex != -1;
        }
    }

    class TypeDeclaration
    {
        public readonly ISymbol Node;
        public TypeDefinitionHandle Handle;
        public string DefaultInterface;
        public string StaticInterface;
        public bool IsSynthesizedInterface;

        public Dictionary<ISymbol, List<MethodDefinitionHandle>> MethodDefinitions = new Dictionary<ISymbol, List<MethodDefinitionHandle>>();
        public Dictionary<ISymbol, List<EntityHandle>> MethodReferences = new Dictionary<ISymbol, List<EntityHandle>>();
        public Dictionary<ISymbol, FieldDefinitionHandle> FieldDefinitions = new Dictionary<ISymbol, FieldDefinitionHandle>();
        public Dictionary<ISymbol, PropertyDefinitionHandle> PropertyDefinitions = new Dictionary<ISymbol, PropertyDefinitionHandle>();
        public Dictionary<ISymbol, EventDefinitionHandle> EventDefinitions = new Dictionary<ISymbol, EventDefinitionHandle>();
        public Dictionary<ISymbol, InterfaceImplementationHandle> InterfaceImplDefinitions = new Dictionary<ISymbol, InterfaceImplementationHandle>();
        public Dictionary<string, List<ISymbol>> MethodsByName = new Dictionary<string, List<ISymbol>>();
        public Dictionary<ISymbol, string> OverloadedMethods = new Dictionary<ISymbol, string>();
        public List<ISymbol> CustomMappedSymbols = new List<ISymbol>();
        public HashSet<ISymbol> SymbolsWithAttributes = new HashSet<ISymbol>();
        public Dictionary<ISymbol, ISymbol> ClassInterfaceMemberMapping = new Dictionary<ISymbol, ISymbol>();

        public TypeDeclaration()
            : this(null)
        {
            IsSynthesizedInterface = true;
        }

        public TypeDeclaration(ISymbol node)
        {
            Node = node;
            Handle = default;
            IsSynthesizedInterface = false;
        }

        public override string ToString()
        {
            return Node.ToString();
        }

        public void AddMethod(ISymbol node, string name, MethodDefinitionHandle handle)
        {
            if (!MethodDefinitions.ContainsKey(node))
            {
                MethodDefinitions[node] = new List<MethodDefinitionHandle>();
                MethodReferences[node] = new List<EntityHandle>();
            }

            if (!MethodsByName.ContainsKey(name))
            {
                MethodsByName[name] = new List<ISymbol>();
            }

            MethodDefinitions[node].Add(handle);
            MethodReferences[node].Add(handle);
            MethodsByName[name].Add(node);
        }

        public void AddMethodReference(ISymbol node, MemberReferenceHandle handle)
        {
            if (!MethodReferences.ContainsKey(node))
            {
                MethodReferences[node] = new List<EntityHandle>();
            }

            MethodReferences[node].Add(handle);
        }

        public void AddMethodOverload(ISymbol node, string overloadedMethodName)
        {
            OverloadedMethods[node] = overloadedMethodName;
        }

        public List<MethodDefinitionHandle> GetMethodDefinitions()
        {
            return MethodDefinitions.Values.SelectMany(list => list).ToList();
        }

        public List<EntityHandle> GetMethodReferences()
        {
            return MethodReferences.Values.SelectMany(list => list).ToList();
        }

        public void AddField(ISymbol node, FieldDefinitionHandle handle)
        {
            FieldDefinitions[node] = handle;
        }

        public List<FieldDefinitionHandle> GetFieldDefinitions()
        {
            return FieldDefinitions.Values.ToList();
        }

        public void AddProperty(ISymbol node, PropertyDefinitionHandle handle)
        {
            PropertyDefinitions[node] = handle;
        }

        public List<PropertyDefinitionHandle> GetPropertyDefinitions()
        {
            return PropertyDefinitions.Values.ToList();
        }

        public void AddEvent(ISymbol node, EventDefinitionHandle handle)
        {
            EventDefinitions[node] = handle;
        }

        public List<EventDefinitionHandle> GetEventDefinitions()
        {
            return EventDefinitions.Values.ToList();
        }

        public void AddInterfaceImpl(ISymbol node, InterfaceImplementationHandle handle)
        {
            InterfaceImplDefinitions[node] = handle;
        }

    }

    class WinRTTypeWriter : CSharpSyntaxWalker
    {
        internal struct MappedType
        {
            private readonly string @namespace;
            private readonly string name;
            private readonly string assembly;
            private readonly bool isSystemType;
            private readonly bool isValueType;
            private readonly Func<ISymbol, (string, string, string, bool, bool)> multipleMappingFunc;

            public MappedType(string @namespace, string name, string assembly, bool isValueType = false)
            {
                this.@namespace = @namespace;
                this.name = name;
                this.assembly = assembly;
                isSystemType = this.assembly == "mscorlib";
                this.isValueType = isValueType;
                multipleMappingFunc = null;
            }

            public MappedType(Func<ISymbol, (string, string, string, bool, bool)> multipleMappingFunc)
            {
                @namespace = null;
                name = null;
                assembly = null;
                isSystemType = false;
                isValueType = false;
                this.multipleMappingFunc = multipleMappingFunc;
            }

            public (string, string, string, bool, bool) GetMapping(ISymbol containingType = null)
            {
                return multipleMappingFunc != null ?
                    multipleMappingFunc(containingType) : (@namespace, name, assembly, isSystemType, isValueType);
            }
        }

        // This should be in sync with the reverse mapping from WinRT.Runtime/Projections.cs and cswinrt/helpers.h.
        internal static readonly Dictionary<string, MappedType> MappedCSharpTypes = new Dictionary<string, MappedType>()
        {
            { "System.DateTimeOffset", new MappedType("Windows.Foundation", "DateTime", "Windows.Foundation.FoundationContract", true) },
            { "System.Exception", new MappedType("Windows.Foundation", "HResult", "Windows.Foundation.FoundationContract", true) },
            { "System.EventHandler`1", new MappedType("Windows.Foundation", "EventHandler`1", "Windows.Foundation.FoundationContract", true) },
            { "System.FlagsAttribute", new MappedType("System", "FlagsAttribute", "mscorlib" ) },
            { "System.IDisposable", new MappedType("Windows.Foundation", "IClosable", "Windows.Foundation.FoundationContract") },
            { "System.IServiceProvider", new MappedType("Microsoft.UI.Xaml", "IXamlServiceProvider", "Microsoft.UI") },
            { "System.Nullable`1", new MappedType("Windows.Foundation", "IReference`1", "Windows.Foundation.FoundationContract" ) },
            { "System.Object", new MappedType("System", "Object", "mscorlib" ) },
            { "System.TimeSpan", new MappedType("Windows.Foundation", "TimeSpan", "Windows.Foundation.FoundationContract", true) },
            { "System.Uri", new MappedType("Windows.Foundation", "Uri", "Windows.Foundation.FoundationContract") },
            { "System.ComponentModel.DataErrorsChangedEventArgs", new MappedType("Microsoft.UI.Xaml.Data", "DataErrorsChangedEventArgs", "Microsoft.UI") },
            { "System.ComponentModel.INotifyDataErrorInfo", new MappedType("Microsoft.UI.Xaml.Data", "INotifyDataErrorInfo", "Microsoft.UI") },
            { "System.ComponentModel.INotifyPropertyChanged", new MappedType("Microsoft.UI.Xaml.Data", "INotifyPropertyChanged", "Microsoft.UI") },
            { "System.ComponentModel.PropertyChangedEventArgs", new MappedType("Microsoft.UI.Xaml.Data", "PropertyChangedEventArgs", "Microsoft.UI") },
            { "System.ComponentModel.PropertyChangedEventHandler", new MappedType("Microsoft.UI.Xaml.Data", "PropertyChangedEventHandler", "Microsoft.UI") },
            { "System.Windows.Input.ICommand", new MappedType("Microsoft.UI.Xaml.Input", "ICommand", "Microsoft.UI") },
            { "System.Collections.IEnumerable", new MappedType("Microsoft.UI.Xaml.Interop", "IBindableIterable", "Microsoft.UI") },
            { "System.Collections.IList", new MappedType("Microsoft.UI.Xaml.Interop", "IBindableVector", "Microsoft.UI") },
            { "System.Collections.Specialized.INotifyCollectionChanged", new MappedType("Microsoft.UI.Xaml.Interop", "INotifyCollectionChanged", "Microsoft.UI") },
            { "System.Collections.Specialized.NotifyCollectionChangedAction", new MappedType("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedAction", "Microsoft.UI") },
            { "System.Collections.Specialized.NotifyCollectionChangedEventArgs", new MappedType("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", "Microsoft.UI") },
            { "System.Collections.Specialized.NotifyCollectionChangedEventHandler", new MappedType("Microsoft.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler", "Microsoft.UI") },
            { "WinRT.EventRegistrationToken", new MappedType("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract", true) },
            { "System.AttributeTargets", new MappedType("Windows.Foundation.Metadata", "AttributeTargets", "Windows.Foundation.FoundationContract", true) },
            { "System.AttributeUsageAttribute", new MappedType("Windows.Foundation.Metadata", "AttributeUsageAttribute", "Windows.Foundation.FoundationContract") },
            { "System.Numerics.Matrix3x2", new MappedType("Windows.Foundation.Numerics", "Matrix3x2", "Windows.Foundation.FoundationContract", true) },
            { "System.Numerics.Matrix4x4", new MappedType("Windows.Foundation.Numerics", "Matrix4x4", "Windows.Foundation.FoundationContract", true) },
            { "System.Numerics.Plane", new MappedType("Windows.Foundation.Numerics", "Plane", "Windows.Foundation.FoundationContract", true) },
            { "System.Numerics.Quaternion", new MappedType("Windows.Foundation.Numerics", "Quaternion", "Windows.Foundation.FoundationContract", true) },
            { "System.Numerics.Vector2", new MappedType("Windows.Foundation.Numerics", "Vector2", "Windows.Foundation.FoundationContract", true) },
            { "System.Numerics.Vector3", new MappedType("Windows.Foundation.Numerics", "Vector3", "Windows.Foundation.FoundationContract", true) },
            { "System.Numerics.Vector4", new MappedType("Windows.Foundation.Numerics", "Vector4", "Windows.Foundation.FoundationContract", true) },
            { "System.Type", new MappedType(GetSystemTypeCustomMapping) },
            { "System.Collections.Generic.IEnumerable`1", new MappedType("Windows.Foundation.Collections", "IIterable`1", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.IEnumerator`1", new MappedType("Windows.Foundation.Collections", "IIterator`1", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.KeyValuePair`2", new MappedType("Windows.Foundation.Collections", "IKeyValuePair`2", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.IReadOnlyDictionary`2", new MappedType("Windows.Foundation.Collections", "IMapView`2", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.IDictionary`2", new MappedType("Windows.Foundation.Collections", "IMap`2", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.IReadOnlyList`1", new MappedType("Windows.Foundation.Collections", "IVectorView`1", "Windows.Foundation.FoundationContract") },
            { "System.Collections.Generic.IList`1", new MappedType("Windows.Foundation.Collections", "IVector`1", "Windows.Foundation.FoundationContract") },
            { "Windows.UI.Color", new MappedType("Windows.UI", "Color", "Windows.Foundation.UniversalApiContract", true) },
        };

        internal static readonly List<string> ImplementedInterfacesWithoutMapping = new List<string>()
        {
            "System.Collections.Generic.ICollection`1",
            "System.Collections.Generic.IReadOnlyCollection`1",
            "System.Collections.ICollection",
            "System.Collections.IEnumerator",
            "System.IEquatable`1",
            "System.Runtime.InteropServices.ICustomQueryInterface",
            "System.Runtime.InteropServices.IDynamicInterfaceCastable",
            "WinRT.IWinRTObject"
        };

        public SemanticModel Model;

        private readonly string assembly;
        private readonly string version;

        private readonly Dictionary<string, TypeReferenceHandle> typeReferenceMapping;
        private readonly Dictionary<string, EntityHandle> assemblyReferenceMapping;
        private readonly MetadataBuilder metadataBuilder;

        private readonly Dictionary<string, TypeDeclaration> typeDefinitionMapping;
        private TypeDeclaration currentTypeDeclaration;

        private Logger Logger { get; }

        public WinRTTypeWriter(
            string assembly,
            string version,
            MetadataBuilder metadataBuilder,
            Logger logger)
        {
            this.assembly = assembly;
            this.version = version;
            this.metadataBuilder = metadataBuilder;
            Logger = logger;
            typeReferenceMapping = new Dictionary<string, TypeReferenceHandle>();
            assemblyReferenceMapping = new Dictionary<string, EntityHandle>();
            typeDefinitionMapping = new Dictionary<string, TypeDeclaration>();

            CreteAssembly();
        }

        private void CreteAssembly()
        {
            Logger.Log("Generating assembly " + assembly + " version " + version);
            metadataBuilder.AddAssembly(
                metadataBuilder.GetOrAddString(assembly),
                new Version(version),
                default,
                default,
                AssemblyFlags.WindowsRuntime,
                AssemblyHashAlgorithm.Sha1);

            var moduleDefinition = metadataBuilder.AddModule(
                0,
                metadataBuilder.GetOrAddString(assembly + ".winmd"),
                metadataBuilder.GetOrAddGuid(Guid.NewGuid()),
                default,
                default);
            assemblyReferenceMapping[assembly] = moduleDefinition;

            metadataBuilder.AddTypeDefinition(
                default,
                default,
                metadataBuilder.GetOrAddString("<Module>"),
                default,
                MetadataTokens.FieldDefinitionHandle(1),
                MetadataTokens.MethodDefinitionHandle(1));
        }

        private bool IsEncodableAsSpecialType(SpecialType specialType)
        {
            return specialType != SpecialType.None && specialType <= SpecialType.System_Array;
        }

        private void EncodeSpecialType(SpecialType specialType, SignatureTypeEncoder typeEncoder)
        {
            switch (specialType)
            {
                case SpecialType.System_Boolean:
                    typeEncoder.Boolean();
                    break;
                case SpecialType.System_Byte:
                    typeEncoder.Byte();
                    break;
                case SpecialType.System_Int16:
                    typeEncoder.Int16();
                    break;
                case SpecialType.System_Int32:
                    typeEncoder.Int32();
                    break;
                case SpecialType.System_Int64:
                    typeEncoder.Int64();
                    break;
                case SpecialType.System_UInt16:
                    typeEncoder.UInt16();
                    break;
                case SpecialType.System_UInt32:
                    typeEncoder.UInt32();
                    break;
                case SpecialType.System_UInt64:
                    typeEncoder.UInt64();
                    break;
                case SpecialType.System_Single:
                    typeEncoder.Single();
                    break;
                case SpecialType.System_Double:
                    typeEncoder.Double();
                    break;
                case SpecialType.System_Char:
                    typeEncoder.Char();
                    break;
                case SpecialType.System_String:
                    typeEncoder.String();
                    break;
                case SpecialType.System_Object:
                    typeEncoder.Object();
                    break;
                case SpecialType.System_IntPtr:
                    typeEncoder.IntPtr();
                    break;
                default:
                    Logger.Log("TODO special type:  " + specialType);
                    break;
            }

            // TODO: handle C# interface mappings for special types
        }

        private BlobHandle GetStrongNameKey(string assembly)
        {
            if (assembly == "mscorlib")
            {
                byte[] mscorlibStrongName = { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 };
                return metadataBuilder.GetOrAddBlob(mscorlibStrongName);
            }

            return default;
        }

        private EntityHandle GetTypeReference(string @namespace, string name, string assembly)
        {
            string fullname = QualifiedName(@namespace, name);
            if (typeReferenceMapping.ContainsKey(fullname))
            {
                return typeReferenceMapping[fullname];
            }

            if (!assemblyReferenceMapping.ContainsKey(assembly))
            {
                EntityHandle assemblyReference = metadataBuilder.AddAssemblyReference(
                    metadataBuilder.GetOrAddString(assembly),
                    new Version(0xff, 0xff, 0xff, 0xff),
                    default,
                    GetStrongNameKey(assembly),
                    assembly == "mscorlib" ? default : AssemblyFlags.WindowsRuntime,
                    default);
                assemblyReferenceMapping[assembly] = assemblyReference;
            }

            var typeRef = metadataBuilder.AddTypeReference(
                assemblyReferenceMapping[assembly],
                metadataBuilder.GetOrAddString(@namespace),
                metadataBuilder.GetOrAddString(name));
            typeReferenceMapping[fullname] = typeRef;
            return typeRef;
        }

        public bool IsWinRTType(ISymbol type)
        {
            bool isProjectedType = type.GetAttributes().
                Any(attribute => attribute.AttributeClass.Name == "WindowsRuntimeTypeAttribute");
            return isProjectedType;
        }

        public string GetAssemblyForWinRTType(ISymbol type)
        {
            var winrtTypeAttribute = type.GetAttributes().
                Where(attribute => attribute.AttributeClass.Name == "WindowsRuntimeTypeAttribute");
            if (winrtTypeAttribute.Any())
            {
                return (string)winrtTypeAttribute.First().ConstructorArguments[0].Value;
            }

            return null;
        }

        private EntityHandle GetTypeReference(ISymbol symbol)
        {
            string @namespace = symbol.ContainingNamespace.ToString();
            string name = GetGenericName(symbol);

            string fullType = QualifiedName(@namespace, name);
            var assembly = GetAssemblyForWinRTType(symbol);
            if (assembly == null)
            {
                if (MappedCSharpTypes.ContainsKey(fullType))
                {
                    (@namespace, name, assembly, _, _) = MappedCSharpTypes[fullType].GetMapping(currentTypeDeclaration.Node);
                    Logger.Log("custom mapping " + fullType + " to " + QualifiedName(@namespace, name) + " from " + assembly);
                }
                else
                {
                    assembly = symbol.ContainingAssembly.Name;
                }
            }

            return GetTypeReference(@namespace, name, assembly);
        }

        private EntityHandle GetTypeSpecification(INamedTypeSymbol symbol)
        {
            if (symbol.IsGenericType)
            {
                Logger.Log("Adding TypeSpec for " + symbol.ToString());
                var typeSpecSignature = new BlobBuilder();
                var genericType = new BlobEncoder(typeSpecSignature)
                    .TypeSpecificationSignature()
                    .GenericInstantiation(GetTypeReference(symbol), symbol.TypeArguments.Length, false);
                foreach (var typeArgument in symbol.TypeArguments)
                {
                    EncodeSymbol(new Symbol(typeArgument), genericType.AddArgument());
                }

                return metadataBuilder.AddTypeSpecification(metadataBuilder.GetOrAddBlob(typeSpecSignature));
            }
            else
            {
                return GetTypeReference(symbol);
            }
        }

        private void EncodeSymbol(Symbol symbol, SignatureTypeEncoder typeEncoder)
        {
            if (symbol.IsHandle())
            {
                typeEncoder.Type(symbol.Handle, false);
            }
            else if (symbol.IsGeneric())
            {
                if (symbol.IsArray)
                {
                    typeEncoder.SZArray().GenericTypeParameter(symbol.GenericIndex);
                }
                else
                {
                    typeEncoder.GenericTypeParameter(symbol.GenericIndex);
                }
            }
            else if (symbol.IsArray)
            {
                EncodeSymbol(new Symbol(symbol.Type), typeEncoder.SZArray());
            }
            else if (symbol.Type is IArrayTypeSymbol arrayType)
            {
                EncodeSymbol(new Symbol(arrayType.ElementType), typeEncoder.SZArray());
            }
            else if (symbol.Type is INamedTypeSymbol namedType && namedType.TypeArguments.Length != 0)
            {
                var genericType = typeEncoder.GenericInstantiation(GetTypeReference(symbol.Type), namedType.TypeArguments.Length, false);
                int parameterIndex = 0;
                foreach (var typeArgument in namedType.TypeArguments)
                {
                    if (namedType.IsUnboundGenericType)
                    {
                        genericType.AddArgument().GenericTypeParameter(parameterIndex);
                    }
                    else
                    {
                        EncodeSymbol(new Symbol(typeArgument), genericType.AddArgument());
                    }
                    parameterIndex++;
                }
            }
            else if (IsEncodableAsSpecialType(symbol.Type.SpecialType))
            {
                EncodeSpecialType(symbol.Type.SpecialType, typeEncoder);
            }
            else
            {
                bool isValueType = symbol.Type.TypeKind == TypeKind.Enum || symbol.Type.TypeKind == TypeKind.Struct;
                if (MappedCSharpTypes.ContainsKey(QualifiedName(symbol.Type)))
                {
                    (_, _, _, _, isValueType) = MappedCSharpTypes[QualifiedName(symbol.Type)].GetMapping(currentTypeDeclaration.Node);
                }
                typeEncoder.Type(GetTypeReference(symbol.Type), isValueType);
            }
        }

        private void EncodeReturnType(Symbol symbol, ReturnTypeEncoder returnTypeEncoder)
        {
            if (symbol == null)
            {
                returnTypeEncoder.Void();
            }
            else if (symbol.IsHandle() || symbol.IsGeneric() || !IsEncodableAsSpecialType(symbol.Type.SpecialType))
            {
                EncodeSymbol(symbol, returnTypeEncoder.Type());
            }
            else if (symbol.Type.SpecialType == SpecialType.System_Void)
            {
                returnTypeEncoder.Void();
            }
            else
            {
                EncodeSpecialType(symbol.Type.SpecialType, returnTypeEncoder.Type());
            }
        }

        private void EncodeParameters(Parameter[] parameters, ParametersEncoder parametersEncoder)
        {
            foreach (var parameter in parameters)
            {
                var parameterType = parameter.Type;
                var parameterTypeEncoder = parametersEncoder.AddParameter();

                if (!parameterType.IsHandle() && !parameterType.IsGeneric() && IsEncodableAsSpecialType(parameterType.Type.SpecialType))
                {
                    EncodeSpecialType(parameterType.Type.SpecialType, parameterTypeEncoder.Type(parameter.ByRef));
                }
                else
                {
                    EncodeSymbol(parameterType, parameterTypeEncoder.Type(parameter.ByRef));
                }
            }
        }

        public MethodDefinitionHandle AddMethodDefinition(
            string name,
            Parameter[] parameters,
            Symbol returnSymbol,
            bool isStatic,
            bool isInterfaceParent,
            bool isSpecialMethod = false,
            bool isPublic = true,
            bool isOverridable = false)
        {
            var methodSignature = new BlobBuilder();
            new BlobEncoder(methodSignature)
                .MethodSignature(
                    SignatureCallingConvention.Default,
                    0,
                    !isStatic)
                .Parameters(
                    parameters.Length,
                    returnType => EncodeReturnType(returnSymbol, returnType),
                    parametersEncoder => EncodeParameters(parameters, parametersEncoder)
                );

            List<ParameterHandle> parameterHandles = new List<ParameterHandle>();
            for (int idx = 0; idx < parameters.Length; idx++)
            {
                parameterHandles.Add(metadataBuilder.AddParameter(
                    parameters[idx].Attributes,
                    metadataBuilder.GetOrAddString(parameters[idx].Name),
                    idx + 1));
            }

            var methodAttributes =
                (isPublic ? MethodAttributes.Public : MethodAttributes.Private) |
                MethodAttributes.HideBySig;

            var methodImplAttributes = MethodImplAttributes.Managed;

            if (isInterfaceParent)
            {
                methodAttributes |= MethodAttributes.Abstract;
            }
            else
            {
                methodImplAttributes |= MethodImplAttributes.Runtime;
            }

            if (isSpecialMethod && name == ".ctor")
            {
                methodAttributes |= MethodAttributes.RTSpecialName;
            }
            else if (isStatic)
            {
                methodAttributes |= MethodAttributes.Static;
            }
            else
            {
                methodAttributes |=
                    MethodAttributes.Virtual |
                    MethodAttributes.NewSlot;

                if (!isOverridable && !isInterfaceParent)
                {
                    methodAttributes |= MethodAttributes.Final;
                }
            }

            if (isSpecialMethod)
            {
                methodAttributes |= MethodAttributes.SpecialName;
            }

            var methodDefinitionHandle = metadataBuilder.AddMethodDefinition(
                methodAttributes,
                methodImplAttributes,
                metadataBuilder.GetOrAddString(name),
                metadataBuilder.GetOrAddBlob(methodSignature),
                -1,
                parameterHandles.Count == 0 ?
                    MetadataTokens.ParameterHandle(metadataBuilder.GetRowCount(TableIndex.Param) + 1) :
                    parameterHandles[0]);
            return methodDefinitionHandle;
        }

        public void AddFieldDeclaration(IFieldSymbol field, bool isEnum)
        {
            Logger.Log("defining field " + field.Name + " with type " + field.Type.ToString());

            var fieldSignature = new BlobBuilder();
            var encoder = new BlobEncoder(fieldSignature);
            EncodeSymbol(new Symbol(field.Type), encoder.FieldSignature());

            var fieldAttributes = FieldAttributes.Public;
            if (isEnum)
            {
                fieldAttributes |=
                    FieldAttributes.Static |
                    FieldAttributes.Literal |
                    FieldAttributes.HasDefault;
            }

            var fieldDefinitionHandle = metadataBuilder.AddFieldDefinition(
                fieldAttributes,
                metadataBuilder.GetOrAddString(field.Name),
                metadataBuilder.GetOrAddBlob(fieldSignature));
            currentTypeDeclaration.AddField(field, fieldDefinitionHandle);

            if (isEnum && field.HasConstantValue)
            {
                metadataBuilder.AddConstant(fieldDefinitionHandle, field.ConstantValue);
            }
        }

        public void AddPropertyDefinition(
            string propertyName,
            Symbol type,
            ISymbol symbol,
            bool isStatic,
            bool hasSetMethod,
            bool isInterfaceParent,
            bool isPublic = true)
        {
            Logger.Log("defining property " + propertyName);
            GetNamespaceAndTypename(propertyName, out var @namespace, out var typename);

            var propertySignature = new BlobBuilder();
            new BlobEncoder(propertySignature)
                .PropertySignature(!isStatic)
                .Parameters(
                    0,
                    returnType => EncodeReturnType(type, returnType),
                    parameters => { }
                );

            var propertyDefinitonHandle = metadataBuilder.AddProperty(
                PropertyAttributes.None,
                metadataBuilder.GetOrAddString(propertyName),
                metadataBuilder.GetOrAddBlob(propertySignature));
            currentTypeDeclaration.AddProperty(symbol, propertyDefinitonHandle);

            if (hasSetMethod)
            {
                string setMethodName = QualifiedName(@namespace, "put_" + typename);
                var setMethod = AddMethodDefinition(
                    setMethodName,
                    new Parameter[] { new Parameter(type, "value", ParameterAttributes.In) },
                    null,
                    !isInterfaceParent && isStatic,
                    isInterfaceParent,
                    true,
                    isPublic);
                currentTypeDeclaration.AddMethod(symbol, setMethodName, setMethod);

                metadataBuilder.AddMethodSemantics(
                    propertyDefinitonHandle,
                    MethodSemanticsAttributes.Setter,
                    setMethod);
            }

            string getMethodName = QualifiedName(@namespace, "get_" + typename);
            var getMethod = AddMethodDefinition(
                getMethodName,
                new Parameter[0],
                type,
                !isInterfaceParent && isStatic,
                isInterfaceParent,
                true,
                isPublic);
            currentTypeDeclaration.AddMethod(symbol, getMethodName, getMethod);

            metadataBuilder.AddMethodSemantics(
                propertyDefinitonHandle,
                MethodSemanticsAttributes.Getter,
                getMethod);
        }

        public void AddPropertyDeclaration(IPropertySymbol property, bool isInterfaceParent)
        {
            AddPropertyDefinition(
                property.Name,
                new Symbol(property.Type),
                property,
                property.IsStatic,
                property.SetMethod != null &&
                    (property.SetMethod.DeclaredAccessibility == Accessibility.Public ||
                     !property.SetMethod.ExplicitInterfaceImplementations.IsDefaultOrEmpty),
                isInterfaceParent,
                property.ExplicitInterfaceImplementations.IsDefaultOrEmpty);
        }

        private TypeDefinitionHandle AddTypeDefinition(
            TypeAttributes typeAttributes,
            string @namespace,
            string identifier,
            EntityHandle baseType)
        {
            var fieldDefinitions = currentTypeDeclaration.GetFieldDefinitions();
            var methodDefinitions = currentTypeDeclaration.GetMethodDefinitions();

            var typeDefinitionHandle = metadataBuilder.AddTypeDefinition(
                typeAttributes,
                metadataBuilder.GetOrAddString(@namespace),
                metadataBuilder.GetOrAddString(identifier),
                baseType,
                fieldDefinitions.Count == 0 ?
                    MetadataTokens.FieldDefinitionHandle(metadataBuilder.GetRowCount(TableIndex.Field) + 1) :
                    fieldDefinitions[0],
                methodDefinitions.Count == 0 ?
                    MetadataTokens.MethodDefinitionHandle(metadataBuilder.GetRowCount(TableIndex.MethodDef) + 1) :
                    methodDefinitions[0]
                );

            var propertyDefinitions = currentTypeDeclaration.GetPropertyDefinitions();
            if (propertyDefinitions.Count != 0)
            {
                metadataBuilder.AddPropertyMap(
                    typeDefinitionHandle,
                    propertyDefinitions[0]);
            }

            var eventDefinitions = currentTypeDeclaration.GetEventDefinitions();
            if (eventDefinitions.Count != 0)
            {
                metadataBuilder.AddEventMap(
                    typeDefinitionHandle,
                    eventDefinitions[0]);
            }

            return typeDefinitionHandle;
        }

        // Based on whether System.Type is used in an attribute declaration or elsewhere, we need to choose the correct custom mapping
        // as attributes don't use the TypeName mapping.
        internal static (string, string, string, bool, bool) GetSystemTypeCustomMapping(ISymbol containingSymbol)
        {
            bool isDefinedInAttribute =
                containingSymbol != null && (containingSymbol as INamedTypeSymbol).BaseType?.ToString() == "System.Attribute";
            return isDefinedInAttribute ?
                ("System", "Type", "mscorlib", true, false) :
                ("Windows.UI.Xaml.Interop", "TypeName", "Windows.Foundation.UniversalApiContract", false, true);
        }

        private void ProcessCustomMappedInterfaces(INamedTypeSymbol classSymbol)
        {
            Logger.Log("writing custom mapped interfaces for " + QualifiedName(classSymbol));
            Dictionary<INamedTypeSymbol, bool> isPublicImplementation = new Dictionary<INamedTypeSymbol, bool>();

            // Mark custom mapped interface members for removal later.
            // Note we want to also mark members from interfaces without mappings.
            foreach (var implementedInterface in GetInterfaces(classSymbol, true).
                Where(symbol => MappedCSharpTypes.ContainsKey(QualifiedName(symbol)) ||
                                ImplementedInterfacesWithoutMapping.Contains(QualifiedName(symbol))))
            {
                bool isPubliclyImplemented = false;
                Logger.Log("custom mapped interface: " + QualifiedName(implementedInterface, true));
                foreach (var interfaceMember in implementedInterface.GetMembers())
                {
                    var classMember = classSymbol.FindImplementationForInterfaceMember(interfaceMember);
                    currentTypeDeclaration.CustomMappedSymbols.Add(classMember);

                    // For custom mapped interfaces, we don't have 1 to 1 mapping of members between the mapped from
                    // and mapped to interface and due to that we need to decide if the mapped inteface as a whole
                    // is public or not (explicitly implemented).  Due to that, as long as one member is not
                    // explicitly implemented (i.e accessible via the class), we treat the entire mapped interface
                    // also as accessible via the class.
                    isPubliclyImplemented |= (classMember.DeclaredAccessibility == Accessibility.Public);
                }
                isPublicImplementation[implementedInterface] = isPubliclyImplemented;
            }

            foreach (var implementedInterface in GetInterfaces(classSymbol)
                        .Where(symbol => MappedCSharpTypes.ContainsKey(QualifiedName(symbol))))
            {
                WriteCustomMappedTypeMembers(implementedInterface, true, isPublicImplementation[implementedInterface]);
            }
        }

        INamedTypeSymbol GetTypeByMetadataName(string metadataName)
        {
            var namedType = Model.Compilation.GetTypeByMetadataName(metadataName);
            if (namedType != null)
            {
                return namedType;
            }

            // Model.Compilation.GetTypeByMetadataName doesn't return a type if there is multiple references with the same type.
            // So as a fallback, go through all the references and check each one filtering to public ones.
            var types = Model.Compilation.References
                 .Select(Model.Compilation.GetAssemblyOrModuleSymbol)
                 .OfType<IAssemblySymbol>()
                 .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName(metadataName))
                 .Where(type => type != null && type.DeclaredAccessibility == Accessibility.Public);
            return types.FirstOrDefault();
        }

        // Convert the entire type name including the generic types to WinMD format.
        private string GetMappedQualifiedTypeName(ITypeSymbol symbol)
        {
            string qualifiedName = QualifiedName(symbol);
            if (MappedCSharpTypes.ContainsKey(qualifiedName))
            {
                var (@namespace, mappedTypeName, _, _, _) = MappedCSharpTypes[qualifiedName].GetMapping(currentTypeDeclaration.Node);
                qualifiedName = QualifiedName(@namespace, mappedTypeName);
                if (symbol is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
                {
                    return string.Format("{0}<{1}>", qualifiedName, string.Join(", ", namedType.TypeArguments.Select(type => GetMappedQualifiedTypeName(type))));
                }
            }
            else if ((symbol.ContainingNamespace.ToString() == "System" && symbol.IsValueType) || qualifiedName == "System.String")
            {
                // WinRT fundamental types
                return symbol.Name;
            }

            return qualifiedName;
        }

        private void WriteCustomMappedTypeMembers(INamedTypeSymbol symbol, bool isDefinition, bool isPublic = true)
        {
            var (_, mappedTypeName, _, _, _) = MappedCSharpTypes[QualifiedName(symbol)].GetMapping(currentTypeDeclaration.Node);
            string qualifiedName = GetMappedQualifiedTypeName(symbol);

            Logger.Log("writing custom mapped type members for " + mappedTypeName + " public: " + isPublic + " qualified name: " + qualifiedName);
            void AddMethod(string name, Parameter[] parameters, Symbol returnType)
            {
                parameters ??= new Parameter[0];
                if (isDefinition)
                {
                    var methodName = isPublic ? name : QualifiedName(qualifiedName, name);
                    var methodDefinitionHandle = AddMethodDefinition(methodName, parameters, returnType, false, false, false, isPublic);
                    currentTypeDeclaration.AddMethod(symbol, methodName, methodDefinitionHandle);
                }
                else
                {
                    var memberReferenceHandle = AddMethodReference(name, parameters, returnType, symbol, false);
                    currentTypeDeclaration.AddMethodReference(symbol, memberReferenceHandle);
                }
            }

            void AddProperty(string name, Symbol type, bool setProperty)
            {
                if (isDefinition)
                {
                    var propertyName = isPublic ? name : QualifiedName(qualifiedName, name);
                    AddPropertyDefinition(propertyName, type, symbol, false, setProperty, false, isPublic);
                }
                else
                {
                    AddPropertyReference(name, type, symbol, symbol, setProperty);
                }
            }

            void AddEvent(string name, Symbol eventType)
            {
                if (isDefinition)
                {
                    var eventName = isPublic ? name : QualifiedName(qualifiedName, name);
                    AddEventDeclaration(eventName, eventType.Type, symbol, false, false, isPublic);
                }
                else
                {
                    AddEventReference(name, eventType.Type, symbol, symbol);
                }
            }

            Symbol GetType(string type, bool isGeneric = false, int genericIndex = -1, bool isArray = false, ITypeSymbol[] genericTypes = null)
            {
                if (string.IsNullOrEmpty(type) && isGeneric)
                {
                    return isDefinition ? new Symbol(symbol.TypeArguments[genericIndex], isArray) : new Symbol(genericIndex, isArray);
                }

                var namedTypeSymbol = GetTypeByMetadataName(type);
                if (!isGeneric)
                {
                    return new Symbol(namedTypeSymbol, isArray);
                }

                if (isDefinition)
                {
                    var typeArguments = genericTypes ?? ((genericIndex == -1) ?
                        symbol.TypeArguments.ToArray() : new ITypeSymbol[] { symbol.TypeArguments[genericIndex] });
                    return new Symbol(namedTypeSymbol.Construct(typeArguments), isArray);
                }
                else
                {
                    return new Symbol(namedTypeSymbol.ConstructUnboundGenericType(), isArray);
                }
            }

            if (mappedTypeName == "IClosable")
            {
                AddMethod("Close", null, null);
            }
            else if (mappedTypeName == "IIterable`1")
            {
                AddMethod("First", null, GetType("System.Collections.Generic.IEnumerator`1", true));
            }
            else if (mappedTypeName == "IMap`2")
            {
                AddMethod("Clear", null, null);
                AddMethod("GetView", null, GetType("System.Collections.Generic.IReadOnlyDictionary`2", true));
                AddMethod(
                    "HasKey",
                    new[] { new Parameter(GetType(null, true, 0), "key", ParameterAttributes.In) },
                    GetType("System.Boolean")
                );
                AddMethod(
                    "Insert",
                    new[] {
                        new Parameter(GetType(null, true, 0), "key", ParameterAttributes.In),
                        new Parameter(GetType(null, true, 1), "value", ParameterAttributes.In)
                    },
                    GetType("System.Boolean")
                );
                AddMethod(
                    "Lookup",
                    new[] { new Parameter(GetType(null, true, 0), "key", ParameterAttributes.In) },
                    GetType(null, true, 1)
                );
                AddMethod(
                    "Remove",
                    new[] { new Parameter(GetType(null, true, 0), "key", ParameterAttributes.In) },
                    null
                );
                AddProperty("Size", GetType("System.UInt32"), false);
            }
            else if (mappedTypeName == "IMapView`2")
            {
                AddMethod(
                    "HasKey",
                    new[] { new Parameter(GetType(null, true, 0), "key", ParameterAttributes.In) },
                    GetType("System.Boolean")
                );
                AddMethod(
                    "Lookup",
                    new[] { new Parameter(GetType(null, true, 0), "key", ParameterAttributes.In) },
                    GetType(null, true, 1)
                );
                AddMethod(
                    "Split",
                    new[] {
                        new Parameter(GetType("System.Collections.Generic.IReadOnlyDictionary`2", true), "first", ParameterAttributes.Out),
                        new Parameter(GetType("System.Collections.Generic.IReadOnlyDictionary`2", true), "second", ParameterAttributes.Out)
                    },
                    null
                );
                AddProperty("Size", GetType("System.UInt32"), false);
            }
            else if (mappedTypeName == "IIterator`1")
            {
                // make array
                AddMethod(
                    "GetMany",
                    new[] { new Parameter(GetType(null, true, 0, true), "items", ParameterAttributes.In, false) },
                    GetType("System.UInt32")
                );
                AddMethod(
                    "MoveNext",
                    null,
                    GetType("System.Boolean")
                );
                AddProperty("Current", GetType(null, true, 0), false);
                AddProperty("HasCurrent", GetType("System.Boolean"), false);
            }
            else if (mappedTypeName == "IVector`1")
            {
                AddMethod(
                    "Append",
                    new[] { new Parameter(GetType(null, true, 0), "value", ParameterAttributes.In) },
                    null
                );
                AddMethod("Clear", null, null);
                AddMethod(
                    "GetAt",
                    new[] { new Parameter(GetType("System.UInt32"), "index", ParameterAttributes.In) },
                    GetType(null, true, 0)
                );
                AddMethod(
                    "GetMany",
                    new[] {
                        new Parameter(GetType("System.UInt32"), "startIndex", ParameterAttributes.In),
                        new Parameter(GetType(null, true, 0, true), "items", ParameterAttributes.In)
                    },
                    GetType("System.UInt32")
                );
                AddMethod("GetView", null, GetType("System.Collections.Generic.IReadOnlyList`1", true));
                AddMethod(
                    "IndexOf",
                    new[] {
                        new Parameter(GetType(null, true, 0), "value", ParameterAttributes.In),
                        new Parameter(GetType("System.UInt32"), "index", ParameterAttributes.Out)
                    },
                    GetType("System.Boolean")
                );
                AddMethod(
                    "InsertAt",
                    new[] {
                        new Parameter(GetType("System.UInt32"), "index", ParameterAttributes.In),
                        new Parameter(GetType(null, true, 0), "value", ParameterAttributes.In),
                    },
                    null
                );
                AddMethod(
                    "RemoveAt",
                    new[] { new Parameter(GetType("System.UInt32"), "index", ParameterAttributes.In) },
                    null
                );
                AddMethod("RemoveAtEnd", null, null);
                AddMethod(
                    "ReplaceAll",
                    new[] {
                        new Parameter(GetType(null, true, 0, true), "items", ParameterAttributes.In)
                    },
                    null
                );
                AddMethod(
                    "SetAt",
                    new[] {
                        new Parameter(GetType("System.UInt32"), "index", ParameterAttributes.In),
                        new Parameter(GetType(null, true, 0), "value", ParameterAttributes.In),
                    },
                    null
                );
                AddProperty("Size", GetType("System.UInt32"), false);
            }
            else if (mappedTypeName == "IVectorView`1")
            {
                AddMethod(
                    "GetAt",
                    new[] { new Parameter(GetType("System.UInt32"), "index", ParameterAttributes.In) },
                    GetType(null, true, 0)
                );
                AddMethod(
                    "GetMany",
                    new[] {
                        new Parameter(GetType("System.UInt32"), "startIndex", ParameterAttributes.In),
                        new Parameter(GetType(null, true, 0, true), "items", ParameterAttributes.In)
                    },
                    GetType("System.UInt32")
                );
                AddMethod(
                    "IndexOf",
                    new[] {
                        new Parameter(GetType(null, true, 0), "value", ParameterAttributes.In),
                        new Parameter(GetType("System.UInt32"), "index", ParameterAttributes.Out)
                    },
                    GetType("System.Boolean")
                );
                AddProperty("Size", GetType("System.UInt32"), false);
            }
            else if (mappedTypeName == "IXamlServiceProvider")
            {
                AddMethod(
                    "GetService",
                    new[] { new Parameter(GetType("System.Type"), "type", ParameterAttributes.In) },
                    GetType("System.Object")
                );
            }
            else if (mappedTypeName == "INotifyDataErrorInfo")
            {
                AddProperty("HasErrors", GetType("System.Boolean"), false);
                AddEvent(
                    "ErrorsChanged",
                    GetType("System.EventHandler`1", true, -1, false, new[] { GetType("System.ComponentModel.DataErrorsChangedEventArgs").Type }));
                AddMethod(
                    "GetErrors",
                    new[] { new Parameter(GetType("System.String"), "propertyName", ParameterAttributes.In) },
                    GetType("System.Collections.Generic.IEnumerable`1", true, -1, false, new[] { GetType("System.Object").Type })
                );
            }
            else if (mappedTypeName == "INotifyPropertyChanged")
            {
                AddEvent("PropertyChanged", GetType("System.ComponentModel.PropertyChangedEventHandler"));
            }
            else if (mappedTypeName == "ICommand")
            {
                AddEvent(
                    "CanExecuteChanged",
                    GetType("System.EventHandler`1", true, -1, false, new[] { GetType("System.Object").Type }));
                AddMethod(
                    "CanExecute",
                    new[] { new Parameter(GetType("System.Object"), "parameter", ParameterAttributes.In) },
                    GetType("System.Boolean")
                );
                AddMethod(
                    "Execute",
                    new[] { new Parameter(GetType("System.Object"), "parameter", ParameterAttributes.In) },
                    null
                );
            }
            else if (mappedTypeName == "IBindableIterable")
            {
                AddMethod("First", null, GetType("Microsoft.UI.Xaml.Interop.IBindableIterator"));
            }
            else if (mappedTypeName == "IBindableVector")
            {
                AddMethod(
                    "Append",
                    new[] { new Parameter(GetType("System.Object"), "value", ParameterAttributes.In) },
                    null
                );
                AddMethod("Clear", null, null);
                AddMethod(
                    "GetAt",
                    new[] { new Parameter(GetType("System.UInt32"), "index", ParameterAttributes.In) },
                    GetType("System.Object")
                );
                AddMethod("GetView", null, GetType("Microsoft.UI.Xaml.Interop.IBindableVectorView"));
                AddMethod(
                    "IndexOf",
                    new[] {
                        new Parameter(GetType("System.Object"), "value", ParameterAttributes.In),
                        new Parameter(GetType("System.UInt32"), "index", ParameterAttributes.Out)
                    },
                    GetType("System.Boolean")
                );
                AddMethod(
                    "InsertAt",
                    new[] {
                        new Parameter(GetType("System.UInt32"), "index", ParameterAttributes.In),
                        new Parameter(GetType("System.Object"), "value", ParameterAttributes.In),
                    },
                    null
                );
                AddMethod(
                    "RemoveAt",
                    new[] { new Parameter(GetType("System.UInt32"), "index", ParameterAttributes.In) },
                    null
                );
                AddMethod("RemoveAtEnd", null, null);
                AddMethod(
                    "SetAt",
                    new[] {
                        new Parameter(GetType("System.UInt32"), "index", ParameterAttributes.In),
                        new Parameter(GetType("System.Object"), "value", ParameterAttributes.In),
                    },
                    null
                );
                AddProperty("Size", GetType("System.UInt32"), false);
            }
            else if (mappedTypeName == "INotifyCollectionChanged")
            {
                AddEvent("CollectionChanged", GetType("System.Collections.Specialized.NotifyCollectionChangedEventHandler"));
            }
        }

        private IEnumerable<INamedTypeSymbol> GetInterfaces(INamedTypeSymbol symbol, bool includeInterfacesWithoutMappings = false)
        {
            HashSet<INamedTypeSymbol> interfaces = new HashSet<INamedTypeSymbol>();
            foreach (var @interface in symbol.Interfaces)
            {
                interfaces.Add(@interface);
                interfaces.UnionWith(@interface.AllInterfaces);
            }

            var baseType = symbol.BaseType;
            while (baseType != null && !IsWinRTType(baseType))
            {
                interfaces.UnionWith(baseType.Interfaces);
                foreach (var @interface in baseType.Interfaces)
                {
                    interfaces.UnionWith(@interface.AllInterfaces);
                }

                baseType = baseType.BaseType;
            }

            // If the generic enumerable is implemented, don't implement the non generic one to prevent issues
            // with the interface members being implemented multiple times.
            if (!includeInterfacesWithoutMappings &&
                interfaces.Any(@interface => QualifiedName(@interface) == "System.Collections.Generic.IEnumerable`1"))
            {
                interfaces.Remove(GetTypeByMetadataName("System.Collections.IEnumerable"));
            }

            return interfaces.Where(@interface =>
                    includeInterfacesWithoutMappings ||
                    !ImplementedInterfacesWithoutMapping.Contains(QualifiedName(@interface)))
                .OrderBy(implementedInterface => implementedInterface.ToString());
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            AddComponentType(Model.GetDeclaredSymbol(node), () => base.VisitInterfaceDeclaration(node));
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            AddComponentType(Model.GetDeclaredSymbol(node), () => base.VisitClassDeclaration(node));
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            AddComponentType(Model.GetDeclaredSymbol(node), () => base.VisitStructDeclaration(node));
        }

        private void EncodeTypedConstant(TypedConstant constant, LiteralEncoder encoder)
        {
            Logger.Log("typed constant kind: " + constant.Kind);
            Logger.Log("typed constant type: " + constant.Type);
            Logger.Log("typed constant value: " + constant.Value);

            switch (constant.Kind)
            {
                case TypedConstantKind.Primitive:
                    encoder.Scalar().Constant(constant.Value);
                    break;
                case TypedConstantKind.Enum:
                    encoder.TaggedScalar(
                        type => type.Enum(constant.Type.ToString()),
                        scalar => scalar.Constant(constant.Value)
                    );
                    break;
                case TypedConstantKind.Type:
                    encoder.Scalar().SystemType(constant.Type.ToString());
                    break;
                case TypedConstantKind.Array:
                    {
                        LiteralsEncoder arrayEncoder = encoder.Vector().Count(constant.Values.Length);
                        foreach (var arrayConstant in constant.Values)
                        {
                            EncodeTypedConstant(arrayConstant, arrayEncoder.AddLiteral());
                        }
                        break;
                    }
            }
        }

        private void EncodeFixedArguments(IList<TypedConstant> arguments, FixedArgumentsEncoder argumentsEncoder)
        {
            foreach (var argument in arguments)
            {
                EncodeTypedConstant(argument, argumentsEncoder.AddArgument());
            }
        }

        private void EncodeCustomElementType(ITypeSymbol type, CustomAttributeElementTypeEncoder typeEncoder)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    typeEncoder.Boolean();
                    break;
                case SpecialType.System_Byte:
                    typeEncoder.Byte();
                    break;
                case SpecialType.System_Int16:
                    typeEncoder.Int16();
                    break;
                case SpecialType.System_Int32:
                    typeEncoder.Int32();
                    break;
                case SpecialType.System_Int64:
                    typeEncoder.Int64();
                    break;
                case SpecialType.System_UInt16:
                    typeEncoder.UInt16();
                    break;
                case SpecialType.System_UInt32:
                    typeEncoder.UInt32();
                    break;
                case SpecialType.System_UInt64:
                    typeEncoder.UInt64();
                    break;
                case SpecialType.System_Single:
                    typeEncoder.Single();
                    break;
                case SpecialType.System_Double:
                    typeEncoder.Double();
                    break;
                case SpecialType.System_Char:
                    typeEncoder.Char();
                    break;
                case SpecialType.System_String:
                    typeEncoder.String();
                    break;
                case SpecialType.System_Enum:
                    typeEncoder.Enum(type.ToString());
                    break;
                case SpecialType.System_SByte:
                    typeEncoder.SByte();
                    break;
                default:
                    Logger.Log("TODO special type: " + type.SpecialType);
                    break;
            }
        }

        private void EncodeNamedArgumentType(ITypeSymbol type, NamedArgumentTypeEncoder encoder)
        {
            Logger.Log("encoding named type");
            if (type.SpecialType == SpecialType.System_Object)
            {
                encoder.Object();
            }
            else if (type.SpecialType == SpecialType.System_Array)
            {
                // TODO array type encoder
                encoder.SZArray();
            }
            else
            {
                EncodeCustomElementType(type, encoder.ScalarType());
            }
        }

        private ISymbol GetMember(INamedTypeSymbol type, string member)
        {
            var foundMembers = type.GetMembers(member);
            var baseType = type.BaseType;
            while (foundMembers.Count() == 0 && baseType != null)
            {
                foundMembers = baseType.GetMembers(member);
                baseType = baseType.BaseType;
            }

            Logger.Log("# members found: " + foundMembers.Count());
            var foundMember = foundMembers.First();
            Logger.Log("found member: " + foundMember);
            return foundMember;
        }

        private void EncodeNamedArguments(
            INamedTypeSymbol attributeType,
            IList<KeyValuePair<string, TypedConstant>> namedArguments,
            CustomAttributeNamedArgumentsEncoder argumentsEncoder)
        {
            var encoder = argumentsEncoder.Count(namedArguments.Count);
            foreach (var argument in namedArguments)
            {
                Logger.Log("named argument: " + argument.Key);
                Logger.Log("value " + argument.Value);

                ITypeSymbol argumentType = null;
                var attributeClassMember = GetMember(attributeType, argument.Key);
                if (attributeClassMember is IFieldSymbol field)
                {
                    argumentType = field.Type;
                }
                else if (attributeClassMember is IPropertySymbol property)
                {
                    argumentType = property.Type;
                }
                else
                {
                    Logger.Log("unexpected member: " + attributeClassMember.Name + " " + attributeClassMember.GetType());
                    throw new InvalidOperationException();
                }

                encoder.AddArgument(
                    attributeClassMember is IFieldSymbol,
                    type => EncodeNamedArgumentType(argumentType, type),
                    name => name.Name(argument.Key),
                    literal => EncodeTypedConstant(argument.Value, literal)
                );
            }
        }

        private void EncodeFixedArguments(IList<object> primitiveArguments, FixedArgumentsEncoder argumentsEncoder)
        {
            foreach (var argument in primitiveArguments)
            {
                var encoder = argumentsEncoder.AddArgument().Scalar();
                if (argument is string type)
                {
                    encoder.SystemType(type);
                }
                else
                {
                    encoder.Constant(argument);
                }
            }
        }

        private void AddDefaultVersionAttribute(EntityHandle parentHandle, int version = -1)
        {
            if (version == -1)
            {
                version = Version.Parse(this.version).Major;
            }

            List<ITypeSymbol> types = new List<ITypeSymbol>
            {
                Model.Compilation.GetTypeByMetadataName("System.UInt32")
            };

            List<object> arguments = new List<object>
            {
                (UInt32) version
            };

            AddCustomAttributes("Windows.Foundation.Metadata.VersionAttribute", types, arguments, parentHandle);
        }

        private void AddActivatableAttribute(EntityHandle parentHandle, UInt32 version, string factoryInterface)
        {
            List<ITypeSymbol> types = new List<ITypeSymbol>(2);
            List<object> arguments = new List<object>(2);

            if (factoryInterface != null)
            {
                types.Add(Model.Compilation.GetTypeByMetadataName("System.Type"));
                arguments.Add(factoryInterface);
            }
            types.Add(Model.Compilation.GetTypeByMetadataName("System.UInt32"));
            arguments.Add(version);

            AddCustomAttributes("Windows.Foundation.Metadata.ActivatableAttribute", types, arguments, parentHandle);
        }

        private void AddExclusiveToAttribute(EntityHandle interfaceHandle, string className)
        {
            List<ITypeSymbol> types = new List<ITypeSymbol>
            {
                Model.Compilation.GetTypeByMetadataName("System.Type")
            };

            List<object> arguments = new List<object>
            {
                className
            };

            AddCustomAttributes("Windows.Foundation.Metadata.ExclusiveToAttribute", types, arguments, interfaceHandle);
        }

        private void AddStaticAttribute(EntityHandle parentHandle, UInt32 version, string staticInterface)
        {
            List<ITypeSymbol> types = new List<ITypeSymbol>
            {
                Model.Compilation.GetTypeByMetadataName("System.Type"),
                Model.Compilation.GetTypeByMetadataName("System.UInt32")
            };

            List<object> arguments = new List<object>
            {
                staticInterface,
                version
            };

            AddCustomAttributes("Windows.Foundation.Metadata.StaticAttribute", types, arguments, parentHandle);
        }

        private void AddDefaultInterfaceImplAttribute(EntityHandle interfaceImplHandle)
        {
            AddCustomAttributes("Windows.Foundation.Metadata.DefaultAttribute", Array.Empty<ITypeSymbol>(), Array.Empty<object>(), interfaceImplHandle);
        }

        private void AddOverloadAttribute(EntityHandle methodHandle, string methodName)
        {
            List<ITypeSymbol> types = new List<ITypeSymbol>
            {
                Model.Compilation.GetTypeByMetadataName("System.String")
            };

            List<object> arguments = new List<object>
            {
                methodName
            };

            AddCustomAttributes("Windows.Foundation.Metadata.OverloadAttribute", types, arguments, methodHandle);
        }

        private void AddOverloadAttributeForInterfaceMethods(TypeDeclaration interfaceTypeDeclaration)
        {
            // Generate unique names for any overloaded methods
            foreach (var methodName in interfaceTypeDeclaration.MethodsByName.Where(symbol => symbol.Value.Count > 1))
            {
                var methodSymbols = methodName.Value.Where(symbol => symbol is IMethodSymbol);
                // Other members that are not methods such as properties and events can generate reserved methods
                // which for the purposes of overloading are considered to be the non overloaded version. If there is no
                // such function, then we consider the first encountered method to be the non overloaded version.
                var skipFirstMethod = methodName.Value.Count == methodSymbols.Count();
                var lastSuffix = 1;
                foreach (var method in methodSymbols)
                {
                    if (skipFirstMethod)
                    {
                        skipFirstMethod = false;
                        continue;
                    }

                    string overloadedMethodName = methodName.Key + (++lastSuffix);
                    while (interfaceTypeDeclaration.MethodsByName.ContainsKey(overloadedMethodName))
                    {
                        overloadedMethodName = methodName.Key + (++lastSuffix);
                    }

                    Logger.Log("Overloading " + methodName.Key + " with " + overloadedMethodName);
                    AddOverloadAttribute(interfaceTypeDeclaration.MethodDefinitions[method].First(), overloadedMethodName);
                    interfaceTypeDeclaration.AddMethodOverload(method, overloadedMethodName);
                }
            }
        }

        private void AddGuidAttribute(EntityHandle parentHandle, string name)
        {
            Guid guid;
            using (SHA1 sha = new SHA1CryptoServiceProvider())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(name));
                guid = Helper.EncodeGuid(hash);
            }

            var uint32Type = Model.Compilation.GetTypeByMetadataName("System.UInt32");
            var uint16Type = Model.Compilation.GetTypeByMetadataName("System.UInt16");
            var byteType = Model.Compilation.GetTypeByMetadataName("System.Byte");
            List<ITypeSymbol> types = new List<ITypeSymbol>
            {
                uint32Type,
                uint16Type,
                uint16Type,
                byteType,
                byteType,
                byteType,
                byteType,
                byteType,
                byteType,
                byteType,
                byteType
            };

            var byteArray = guid.ToByteArray();
            List<object> arguments = new List<object>
            {
                BitConverter.ToUInt32(byteArray, 0),
                BitConverter.ToUInt16(byteArray, 4),
                BitConverter.ToUInt16(byteArray, 6),
                byteArray[8],
                byteArray[9],
                byteArray[10],
                byteArray[11],
                byteArray[12],
                byteArray[13],
                byteArray[14],
                byteArray[15]
            };

            AddCustomAttributes("Windows.Foundation.Metadata.GuidAttribute", types, arguments, parentHandle);
        }

        private void AddCustomAttributes(
            string attributeTypeName,
            IList<ITypeSymbol> primitiveTypes,
            IList<object> primitiveValues,
            EntityHandle parentHandle)
        {
            var attributeType = Model.Compilation.GetTypeByMetadataName(attributeTypeName);
            Logger.Log("attribute type found " + attributeType);
            if (!typeDefinitionMapping.ContainsKey(attributeTypeName))
            {
                // Even if the attribute is an external non WinRT type, treat it as a projected type.
                Logger.Log("adding attribute type");
                AddType(attributeType, true);
            }

            Logger.Log("# constructor found: " + attributeType.Constructors.Length);
            var matchingConstructor = attributeType.Constructors.Where(constructor =>
                constructor.Parameters.Length == primitiveValues.Count &&
                constructor.Parameters.Select(param => param.Type).SequenceEqual(primitiveTypes));

            Logger.Log("# matching constructor found: " + matchingConstructor.Count());
            Logger.Log("matching constructor found: " + matchingConstructor.First());

            var constructorReference = typeDefinitionMapping[attributeTypeName].MethodReferences[matchingConstructor.First()];
            Logger.Log("found constructor handle:  " + constructorReference.Count);

            var attributeSignature = new BlobBuilder();
            new BlobEncoder(attributeSignature)
                .CustomAttributeSignature(
                    fixedArguments => EncodeFixedArguments(primitiveValues, fixedArguments),
                    namedArguments => namedArguments.Count(0)
                );

            metadataBuilder.AddCustomAttribute(
                parentHandle,
                constructorReference.First(),
                metadataBuilder.GetOrAddBlob(attributeSignature));
        }

        private void AddCustomAttributes(IEnumerable<AttributeData> attributes, EntityHandle parentHandle)
        {
            foreach (var attribute in attributes)
            {
                var attributeType = attribute.AttributeClass;
                if (attributeType.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                Logger.Log("attribute: " + attribute);
                Logger.Log("attribute type: " + attributeType);
                Logger.Log("attribute constructor: " + attribute.AttributeConstructor);
                Logger.Log("atttribute # constructor arguments: " + attribute.ConstructorArguments.Length);
                Logger.Log("atttribute # named arguments: " + attribute.NamedArguments.Length);

                if (!typeDefinitionMapping.ContainsKey(attributeType.ToString()))
                {
                    // Even if the attribute is an external non WinRT type, treat it as a projected type.
                    AddType(attributeType, true);
                }

                var constructorReference = typeDefinitionMapping[attributeType.ToString()].MethodReferences[attribute.AttributeConstructor];
                Logger.Log("found # constructors: " + constructorReference.Count);

                var attributeSignature = new BlobBuilder();
                new BlobEncoder(attributeSignature)
                    .CustomAttributeSignature(
                        fixedArguments => EncodeFixedArguments(attribute.ConstructorArguments, fixedArguments),
                        namedArguments => EncodeNamedArguments(attributeType, attribute.NamedArguments, namedArguments)
                    );

                metadataBuilder.AddCustomAttribute(
                    parentHandle,
                    constructorReference.First(),
                    metadataBuilder.GetOrAddBlob(attributeSignature));
            }
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var symbol = Model.GetDeclaredSymbol(node);

            void processEnumDeclaration()
            {
                var enumTypeFieldAttributes =
                    FieldAttributes.Private |
                    FieldAttributes.SpecialName |
                    FieldAttributes.RTSpecialName;

                var enumTypeSymbol = symbol.EnumUnderlyingType;
                var fieldSignature = new BlobBuilder();
                var encoder = new BlobEncoder(fieldSignature);
                // TODO: special type enforcement for 64 bit
                EncodeSpecialType(enumTypeSymbol.SpecialType, encoder.FieldSignature());

                var fieldDefinitionHandle = metadataBuilder.AddFieldDefinition(
                    enumTypeFieldAttributes,
                    metadataBuilder.GetOrAddString("value__"),
                    metadataBuilder.GetOrAddBlob(fieldSignature));
                currentTypeDeclaration.AddField(symbol, fieldDefinitionHandle);

                base.VisitEnumDeclaration(node);
            }

            AddComponentType(symbol, processEnumDeclaration);
        }

        public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            var symbol = Model.GetDeclaredSymbol(node);
            if (!IsPublic(symbol))
            {
                return;
            }

            Logger.Log("defining delegate " + symbol.Name);
            currentTypeDeclaration = new TypeDeclaration(symbol);

            base.VisitDelegateDeclaration(node);
            CheckAndMarkSymbolForAttributes(symbol);

            var objType = Model.Compilation.GetTypeByMetadataName(typeof(object).FullName);
            var nativeIntType = Model.Compilation.GetTypeByMetadataName(typeof(IntPtr).FullName);

            currentTypeDeclaration.AddMethod(
                symbol,
                ".ctor",
                AddMethodDefinition(
                    ".ctor",
                    new Parameter[] {
                        new Parameter(objType, "object", ParameterAttributes.None),
                        new Parameter(nativeIntType, "method", ParameterAttributes.None)
                    },
                    null,
                    false,
                    false,
                    true,
                    false,
                    true
                )
            );

            Parameter[] parameters = Parameter.GetParameters(node.ParameterList, Model);
            currentTypeDeclaration.AddMethod(
                symbol,
                "Invoke",
                AddMethodDefinition(
                    "Invoke",
                    parameters,
                    new Symbol(symbol.DelegateInvokeMethod.ReturnType),
                    false,
                    false,
                    true,
                    true,
                    true
                )
            );

            TypeAttributes typeAttributes =
                TypeAttributes.Public |
                TypeAttributes.WindowsRuntime |
                TypeAttributes.AutoLayout |
                TypeAttributes.AnsiClass |
                TypeAttributes.Sealed;

            var typeDefinitionHandle = AddTypeDefinition(
                typeAttributes,
                symbol.ContainingNamespace.ToString(),
                symbol.Name,
                GetTypeReference("System", "MulticastDelegate", "mscorlib"));
            currentTypeDeclaration.Handle = typeDefinitionHandle;
            typeDefinitionMapping[QualifiedName(symbol, true)] = currentTypeDeclaration;

            AddGuidAttribute(typeDefinitionHandle, symbol.ToString());
        }

        public void AddEventDeclaration(string eventName, ITypeSymbol eventType, ISymbol symbol, bool isStatic, bool isInterfaceParent, bool isPublic = true)
        {
            Logger.Log("defining event " + eventName + " with type " + eventType.ToString());

            GetNamespaceAndTypename(eventName, out var @namespace, out var typename);

            var delegateSymbolType = eventType as INamedTypeSymbol;
            EntityHandle typeReferenceHandle = GetTypeSpecification(delegateSymbolType);
            EntityHandle eventRegistrationTokenTypeHandle = GetTypeReference("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract");
            Symbol eventRegistrationToken = new Symbol(eventRegistrationTokenTypeHandle);

            var eventDefinitionHandle = metadataBuilder.AddEvent(
                EventAttributes.None,
                metadataBuilder.GetOrAddString(eventName),
                typeReferenceHandle);
            currentTypeDeclaration.AddEvent(symbol, eventDefinitionHandle);

            string addMethodName = QualifiedName(@namespace, "add_" + typename);
            var addMethod = AddMethodDefinition(
                addMethodName,
                new Parameter[] { new Parameter(delegateSymbolType, "handler", ParameterAttributes.In) },
                eventRegistrationToken,
                !isInterfaceParent && isStatic,
                isInterfaceParent,
                true,
                isPublic);
            currentTypeDeclaration.AddMethod(symbol, addMethodName, addMethod);

            metadataBuilder.AddMethodSemantics(
                eventDefinitionHandle,
                MethodSemanticsAttributes.Adder,
                addMethod);

            string removeMethodName = QualifiedName(@namespace, "remove_" + typename);
            var removeMethod = AddMethodDefinition(
                removeMethodName,
                new Parameter[] { new Parameter(eventRegistrationToken, "token", ParameterAttributes.In) },
                null,
                !isInterfaceParent && isStatic,
                isInterfaceParent,
                true,
                isPublic);
            currentTypeDeclaration.AddMethod(symbol, removeMethodName, removeMethod);

            metadataBuilder.AddMethodSemantics(
                eventDefinitionHandle,
                MethodSemanticsAttributes.Remover,
                removeMethod);
        }

        public void AddEventDeclaration(IEventSymbol @event, bool isInterfaceParent)
        {
            AddEventDeclaration(@event.Name, @event.Type, @event, @event.IsStatic, isInterfaceParent, @event.ExplicitInterfaceImplementations.IsDefaultOrEmpty);
        }

        void AddMethodDeclaration(IMethodSymbol method, bool isInterfaceParent)
        {
            Logger.Log("add method from symbol: " + method.Name);

            bool isConstructor = method.MethodKind == MethodKind.Constructor;
            string methodName = isConstructor ? ".ctor" : method.Name;
            var returnType = isConstructor ? null : new Symbol(method.ReturnType);
            Parameter[] parameters = Parameter.GetParameters(method);
            var methodDefinitionHandle = AddMethodDefinition(
                methodName,
                parameters,
                returnType,
                !isInterfaceParent && method.IsStatic,
                isInterfaceParent,
                isConstructor,
                method.ExplicitInterfaceImplementations.IsDefaultOrEmpty);
            currentTypeDeclaration.AddMethod(method, methodName, methodDefinitionHandle);
        }

        void AddFactoryMethod(INamedTypeSymbol classSymbol, IMethodSymbol method)
        {
            Logger.Log("adding factory method: " + method.Name);

            string methodName = "Create" + classSymbol.Name;
            Parameter[] parameters = Parameter.GetParameters(method);
            var methodDefinitionHandle = AddMethodDefinition(
                methodName,
                parameters,
                new Symbol(classSymbol),
                false,
                true,
                false,
                true,
                true);
            currentTypeDeclaration.AddMethod(method, methodName, methodDefinitionHandle);
        }

        void AddComponentType(INamedTypeSymbol type, Action visitTypeDeclaration = null)
        {
            if (!IsPublic(type) || typeDefinitionMapping.ContainsKey(type.ToString()))
            {
                return;
            }

            Logger.Log("defining type: " + type.TypeKind + " " + type.ToString());

            var typeDeclaration = new TypeDeclaration(type);
            currentTypeDeclaration = typeDeclaration;

            if (type.TypeKind == TypeKind.Class)
            {
                ProcessCustomMappedInterfaces(type);
            }

            visitTypeDeclaration?.Invoke();
            CheckAndMarkSymbolForAttributes(type);

            bool isInterface = type.TypeKind == TypeKind.Interface;
            bool hasConstructor = false;
            bool hasDefaultConstructor = false;
            foreach (var member in type.GetMembers())
            {
                if (!IsPublic(member) || typeDeclaration.CustomMappedSymbols.Contains(member))
                {
                    Logger.Log(member.Kind + " member skipped " + member.Name);
                    continue;
                }

                if (type.TypeKind == TypeKind.Struct || type.TypeKind == TypeKind.Enum)
                {
                    if (member is IFieldSymbol field)
                    {
                        AddFieldDeclaration(field, type.TypeKind == TypeKind.Enum);
                    }
                }
                else
                {
                    if (member is IMethodSymbol method &&
                        (method.MethodKind == MethodKind.Ordinary ||
                         method.MethodKind == MethodKind.ExplicitInterfaceImplementation ||
                         method.MethodKind == MethodKind.Constructor))
                    {
                        AddMethodDeclaration(method, isInterface);

                        if (method.MethodKind == MethodKind.Constructor)
                        {
                            hasConstructor = true;
                            hasDefaultConstructor |= (method.Parameters.Length == 0);
                        }
                    }
                    else if (member is IPropertySymbol property)
                    {
                        AddPropertyDeclaration(property, isInterface);
                    }
                    else if (member is IEventSymbol @event)
                    {
                        AddEventDeclaration(@event, isInterface);
                    }
                    else
                    {
                        Logger.Log("member not recognized: " + member.Kind + " name: " + member.Name);
                        continue;
                    }
                }

                CheckAndMarkSymbolForAttributes(member);
            }

            // implicit constructor if none defined
            if (!hasConstructor && type.TypeKind == TypeKind.Class && !type.IsStatic)
            {
                string constructorMethodName = ".ctor";
                var methodDefinitionHandle = AddMethodDefinition(
                    constructorMethodName,
                    new Parameter[0],
                    null,
                    false,
                    false,
                    true,
                    true,
                    true);
                typeDeclaration.AddMethod(type, constructorMethodName, methodDefinitionHandle);
                hasDefaultConstructor = true;
            }

            TypeAttributes typeAttributes =
                TypeAttributes.Public |
                TypeAttributes.WindowsRuntime |
                TypeAttributes.AutoLayout |
                TypeAttributes.AnsiClass;

            if (type.IsSealed ||
               type.IsStatic ||
               type.TypeKind == TypeKind.Struct ||
               type.TypeKind == TypeKind.Enum)
            {
                typeAttributes |= TypeAttributes.Sealed;
            }

            if (type.TypeKind == TypeKind.Class && type.IsStatic)
            {
                typeAttributes |= TypeAttributes.Abstract;
            }

            EntityHandle baseType = default;
            if (isInterface)
            {
                typeAttributes |=
                    TypeAttributes.Interface |
                    TypeAttributes.Abstract;
            }
            else if (type.TypeKind == TypeKind.Class)
            {
                typeAttributes |=
                    TypeAttributes.Class |
                    TypeAttributes.BeforeFieldInit;

                // extends
                if (type.BaseType != null)
                {
                    baseType = GetTypeReference(type.BaseType);
                }
                else
                {
                    baseType = GetTypeReference("System", "Object", "mscorlib");
                }
            }
            else if (type.TypeKind == TypeKind.Struct)
            {
                typeAttributes |= TypeAttributes.SequentialLayout;
                baseType = GetTypeReference("System", "ValueType", "mscorlib");
            }
            else if (type.TypeKind == TypeKind.Enum)
            {
                baseType = GetTypeReference("System", "Enum", "mscorlib");
            }

            var typeDefinitionHandle = AddTypeDefinition(
                typeAttributes,
                type.ContainingNamespace.ToString(),
                type.Name,
                baseType);
            typeDeclaration.Handle = typeDefinitionHandle;

            if (isInterface || type.TypeKind == TypeKind.Class)
            {
                // Interface implementations need to be added in order of class and then typespec.  Given entries are added
                // per class, that is already in order, but we need to sort by typespec.
                List<KeyValuePair<INamedTypeSymbol, EntityHandle>> implementedInterfaces = new List<KeyValuePair<INamedTypeSymbol, EntityHandle>>();
                foreach (var implementedInterface in GetInterfaces(type))
                {
                    implementedInterfaces.Add(new KeyValuePair<INamedTypeSymbol, EntityHandle>(implementedInterface, GetTypeSpecification(implementedInterface)));
                }
                implementedInterfaces.Sort((x, y) => CodedIndex.TypeDefOrRefOrSpec(x.Value).CompareTo(CodedIndex.TypeDefOrRefOrSpec(y.Value)));

                foreach (var implementedInterface in implementedInterfaces)
                {
                    var interfaceImplHandle = metadataBuilder.AddInterfaceImplementation(
                        typeDefinitionHandle,
                        implementedInterface.Value);
                    typeDeclaration.AddInterfaceImpl(implementedInterface.Key, interfaceImplHandle);
                }
            }

            if (isInterface)
            {
                AddGuidAttribute(typeDefinitionHandle, type.ToString());
                AddOverloadAttributeForInterfaceMethods(typeDeclaration);
            }

            typeDefinitionMapping[QualifiedName(type, true)] = typeDeclaration;

            if (type.TypeKind == TypeKind.Class)
            {
                if (hasDefaultConstructor)
                {
                    AddActivatableAttribute(
                        typeDeclaration.Handle,
                        (uint)GetVersion(type, true),
                        null);
                }
                AddSynthesizedInterfaces(typeDeclaration);

                // No synthesized default interface generated
                if (typeDeclaration.DefaultInterface == null && type.Interfaces.Length != 0)
                {
                    AddDefaultInterfaceImplAttribute(typeDeclaration.InterfaceImplDefinitions[type.Interfaces[0]]);
                }
            }
        }

        MemberReferenceHandle AddMethodReference(
            string name,
            Parameter[] parameters,
            Symbol returnSymbol,
            INamedTypeSymbol parentType,
            bool isStatic)
        {
            var methodSignature = new BlobBuilder();
            new BlobEncoder(methodSignature)
                .MethodSignature(
                    SignatureCallingConvention.Default,
                    0,
                    !isStatic)
                .Parameters(
                    parameters.Length,
                    returnType => EncodeReturnType(returnSymbol, returnType),
                    parametersEncoder => EncodeParameters(parameters, parametersEncoder)
                );

            var referenceHandle = metadataBuilder.AddMemberReference(
                GetTypeSpecification(parentType),
                metadataBuilder.GetOrAddString(name),
                metadataBuilder.GetOrAddBlob(methodSignature)
            );
            return referenceHandle;
        }

        MemberReferenceHandle AddMethodReference(IMethodSymbol method)
        {
            Logger.Log("adding method reference: " + method.Name);

            bool isInterfaceParent = method.ContainingType.TypeKind == TypeKind.Interface;
            string methodName = method.MethodKind == MethodKind.Constructor ? ".ctor" : method.Name;
            Parameter[] parameters = Parameter.GetParameters(method);
            var referenceHandle = AddMethodReference(
                methodName,
                parameters,
                new Symbol(method.ReturnType),
                method.ContainingType,
                !isInterfaceParent && method.IsStatic);
            currentTypeDeclaration.AddMethodReference(method, referenceHandle);
            return referenceHandle;
        }

        public void AddPropertyReference(string name, Symbol type, ISymbol symbol, INamedTypeSymbol parent, bool setMethod)
        {
            Logger.Log("adding property reference: " + name);

            if (setMethod)
            {
                var setMethodReference = AddMethodReference(
                    "put_" + name,
                    new Parameter[] { new Parameter(type, "value", ParameterAttributes.In) },
                    null,
                    parent,
                    false);
                currentTypeDeclaration.AddMethodReference(symbol, setMethodReference);
            }

            var getMethodReference = AddMethodReference(
                "get_" + name,
                new Parameter[0],
                type,
                parent,
                false);
            currentTypeDeclaration.AddMethodReference(symbol, getMethodReference);
        }

        public void AddPropertyReference(IPropertySymbol property)
        {
            AddPropertyReference(
                property.Name,
                new Symbol(property.Type),
                property,
                property.ContainingType,
                property.SetMethod != null);
        }

        public void AddEventReference(string eventName, ITypeSymbol eventType, ISymbol symbol, INamedTypeSymbol parent)
        {
            Logger.Log("adding event reference:  " + eventName);

            EntityHandle eventRegistrationTokenTypeHandle = GetTypeReference("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract");
            Symbol eventRegistrationToken = new Symbol(eventRegistrationTokenTypeHandle);

            var addMethodReference = AddMethodReference(
                "add_" + eventName,
                new Parameter[] { new Parameter(eventType, "handler", ParameterAttributes.In) },
                eventRegistrationToken,
                parent,
                false);
            currentTypeDeclaration.AddMethodReference(symbol, addMethodReference);

            var removeMethodReference = AddMethodReference(
                "remove_" + eventName,
                new Parameter[] { new Parameter(eventRegistrationToken, "token", ParameterAttributes.In) },
                null,
                parent,
                false);
            currentTypeDeclaration.AddMethodReference(symbol, removeMethodReference);
        }

        public void AddEventReference(IEventSymbol @event)
        {
            AddEventReference(@event.Name, @event.Type, @event, @event.ContainingType);
        }

        void AddProjectedType(INamedTypeSymbol type, string projectedTypeOverride = null)
        {
            currentTypeDeclaration = new TypeDeclaration(type);

            foreach (var member in type.GetMembers())
            {
                if (member is IMethodSymbol method &&
                    (method.MethodKind == MethodKind.Ordinary ||
                     method.MethodKind == MethodKind.ExplicitInterfaceImplementation ||
                     method.MethodKind == MethodKind.Constructor))
                {
                    AddMethodReference(method);
                }
                else if (member is IPropertySymbol property)
                {
                    AddPropertyReference(property);
                }
                else if (member is IEventSymbol @event)
                {
                    AddEventReference(@event);
                }
                else
                {
                    Logger.Log("member not recognized: " + member.Kind + " " + member.Name);
                }
            }

            typeDefinitionMapping[projectedTypeOverride ?? QualifiedName(type, true)] = currentTypeDeclaration;
        }

        void AddMappedType(INamedTypeSymbol type)
        {
            currentTypeDeclaration = new TypeDeclaration(type);
            WriteCustomMappedTypeMembers(type, false);
            typeDefinitionMapping[QualifiedName(type, true)] = currentTypeDeclaration;
        }

        enum SynthesizedInterfaceType
        {
            Static,
            Factory,
            Default
        }

        string GetSynthesizedInterfaceName(string className, SynthesizedInterfaceType type)
        {
            // TODO: handle existing types by appending number suffix
            return "I" + className +
                type switch
                {
                    SynthesizedInterfaceType.Default => "Class",
                    SynthesizedInterfaceType.Factory => "Factory",
                    SynthesizedInterfaceType.Static => "Static",
                    _ => "",
                };
        }

        void AddSynthesizedInterfaces(TypeDeclaration classDeclaration)
        {
            HashSet<ISymbol> classMembersFromInterfaces = new HashSet<ISymbol>();
            INamedTypeSymbol classSymbol = classDeclaration.Node as INamedTypeSymbol;
            foreach (var @interface in classSymbol.AllInterfaces)
            {
                foreach (var interfaceMember in @interface.GetMembers())
                {
                    var classMember = classSymbol.FindImplementationForInterfaceMember(interfaceMember);
                    if (classMember == null || !classDeclaration.MethodDefinitions.ContainsKey(classMember))
                    {
                        continue;
                    }

                    classMembersFromInterfaces.Add(classMember);
                    classDeclaration.ClassInterfaceMemberMapping[classMember] = interfaceMember;

                    // Mark class members whose interface declaration has attributes 
                    // so that we can propagate them later.
                    if (interfaceMember.GetAttributes().Any())
                    {
                        classDeclaration.SymbolsWithAttributes.Add(classMember);
                    }
                }
            }

            AddSynthesizedInterface(
                classDeclaration,
                SynthesizedInterfaceType.Static,
                classMembersFromInterfaces);

            AddSynthesizedInterface(
                classDeclaration,
                SynthesizedInterfaceType.Factory,
                classMembersFromInterfaces);

            AddSynthesizedInterface(
                classDeclaration,
                SynthesizedInterfaceType.Default,
                classMembersFromInterfaces);

            // TODO: address overridable and composable interfaces.
        }

        void CheckAndMarkSynthesizedInterfaceSymbolForAttributes(ISymbol symbol, TypeDeclaration classDeclaration)
        {
            // Check the class declaration if the symbol had any attributes marked for it,
            // and if so propagate it to the synthesized interface.
            if (classDeclaration.SymbolsWithAttributes.Contains(symbol))
            {
                currentTypeDeclaration.SymbolsWithAttributes.Add(symbol);
            }
        }

        void CheckAndMarkSymbolForAttributes(ISymbol symbol)
        {
            if (symbol.GetAttributes().Any())
            {
                currentTypeDeclaration.SymbolsWithAttributes.Add(symbol);
            }
        }

        void AddSynthesizedInterface(
            TypeDeclaration classDeclaration,
            SynthesizedInterfaceType interfaceType,
            HashSet<ISymbol> classMembersFromInterfaces)
        {
            var typeDeclaration = new TypeDeclaration();
            currentTypeDeclaration = typeDeclaration;

            bool hasTypes = false;
            INamedTypeSymbol classSymbol = classDeclaration.Node as INamedTypeSymbol;

            // Each class member results in some form of method definition,
            // so using that to our advantage to get public members.
            foreach (var classMember in classDeclaration.MethodDefinitions)
            {
                if (interfaceType == SynthesizedInterfaceType.Factory &&
                    classMember.Key is IMethodSymbol constructorMethod &&
                    constructorMethod.MethodKind == MethodKind.Constructor &&
                    constructorMethod.Parameters.Length != 0)
                {
                    hasTypes = true;
                    AddFactoryMethod(classSymbol, constructorMethod);
                    CheckAndMarkSynthesizedInterfaceSymbolForAttributes(classMember.Key, classDeclaration);
                }
                else if ((interfaceType == SynthesizedInterfaceType.Default && !classMember.Key.IsStatic &&
                         !classMembersFromInterfaces.Contains(classMember.Key)) ||
                    (interfaceType == SynthesizedInterfaceType.Static && classMember.Key.IsStatic))
                {
                    if (classMember.Key is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary)
                    {
                        AddMethodDeclaration(method, true);
                    }
                    else if (classMember.Key is IPropertySymbol property)
                    {
                        AddPropertyDeclaration(property, true);
                    }
                    else if (classMember.Key is IEventSymbol @event)
                    {
                        AddEventDeclaration(@event, true);
                    }
                    else
                    {
                        Logger.Log("member for synthesized interface not recognized: " + classMember.Key.Kind + " " + classMember.Key.Name);
                        continue;
                    }

                    CheckAndMarkSynthesizedInterfaceSymbolForAttributes(classMember.Key, classDeclaration);
                    hasTypes = true;
                }
            }

            TypeAttributes typeAttributes =
                TypeAttributes.NotPublic |
                TypeAttributes.WindowsRuntime |
                TypeAttributes.AutoLayout |
                TypeAttributes.AnsiClass |
                TypeAttributes.Interface |
                TypeAttributes.Abstract;

            if (hasTypes || (interfaceType == SynthesizedInterfaceType.Default && classSymbol.Interfaces.Length == 0))
            {
                Logger.Log("writing generated interface " + interfaceType);
                var interfaceName = GetSynthesizedInterfaceName(classDeclaration.Node.Name, interfaceType);
                var typeDefinitionHandle = AddTypeDefinition(
                    typeAttributes,
                    classDeclaration.Node.ContainingNamespace.ToString(),
                    interfaceName,
                    default);
                typeDeclaration.Handle = typeDefinitionHandle;

                string qualifiedInterfaceName = QualifiedName(classDeclaration.Node.ContainingNamespace.ToString(), interfaceName);
                typeDefinitionMapping[qualifiedInterfaceName] = typeDeclaration;

                if (interfaceType == SynthesizedInterfaceType.Default)
                {
                    classDeclaration.DefaultInterface = qualifiedInterfaceName;
                    var interfaceImplHandle = metadataBuilder.AddInterfaceImplementation(
                        classDeclaration.Handle,
                        GetTypeReference(classDeclaration.Node.ContainingNamespace.ToString(), interfaceName, assembly));
                    classDeclaration.AddInterfaceImpl(classSymbol, interfaceImplHandle);
                    AddDefaultInterfaceImplAttribute(interfaceImplHandle);
                }

                AddDefaultVersionAttribute(typeDefinitionHandle, GetVersion(classSymbol, true));
                AddGuidAttribute(typeDefinitionHandle, interfaceName);
                AddExclusiveToAttribute(typeDefinitionHandle, classSymbol.ToString());
                AddOverloadAttributeForInterfaceMethods(typeDeclaration);

                if (interfaceType == SynthesizedInterfaceType.Factory)
                {
                    AddActivatableAttribute(classDeclaration.Handle, (uint)GetVersion(classSymbol, true), qualifiedInterfaceName);
                }
                else if (interfaceType == SynthesizedInterfaceType.Static)
                {
                    classDeclaration.StaticInterface = qualifiedInterfaceName;
                    AddStaticAttribute(classDeclaration.Handle, (uint)GetVersion(classSymbol, true), qualifiedInterfaceName);
                }
            }
        }

        private int GetVersion(INamedTypeSymbol type, bool setDefaultIfNotSet = false)
        {
            var versionAttribute = type.GetAttributes().
                Where(attribute => attribute.AttributeClass.Name == "VersionAttribute");
            if (!versionAttribute.Any())
            {
                return setDefaultIfNotSet ? Version.Parse(this.version).Major : -1;
            }

            uint version = (uint)versionAttribute.First().ConstructorArguments[0].Value;
            return (int)version;
        }

        void AddType(INamedTypeSymbol type, bool treatAsProjectedType = false)
        {
            Logger.Log("add type: " + type.ToString());
            bool isProjectedType = type.GetAttributes().
                Any(attribute => attribute.AttributeClass.Name == "WindowsRuntimeTypeAttribute");
            var qualifiedName = QualifiedName(type);
            if (isProjectedType)
            {
                AddProjectedType(type);
            }
            else if (MappedCSharpTypes.ContainsKey(qualifiedName))
            {
                var (@namespace, name, assembly, isSystemType, _) = MappedCSharpTypes[qualifiedName].GetMapping();
                if (isSystemType)
                {
                    var projectedType = Model.Compilation.GetTypeByMetadataName(QualifiedName(@namespace, name));
                    AddProjectedType(projectedType);
                }
                else
                {
                    AddMappedType(type);
                }
            }
            else if (treatAsProjectedType)
            {
                // Prioritize any mapped types before treating an attribute as a projected type.
                AddProjectedType(type);
            }
            else
            {
                AddComponentType(type);
            }
        }

        void AddCustomAttributes(TypeDeclaration typeDeclaration, string interfaceName = null)
        {
            foreach (var node in typeDeclaration.SymbolsWithAttributes)
            {
                EntityHandle parentHandle;
                if (node is INamedTypeSymbol namedType)
                {
                    // Attributes on classes don't propagate to synthesized interfaces.
                    if (interfaceName != null)
                    {
                        continue;
                    }

                    parentHandle = typeDefinitionMapping[namedType.ToString()].Handle;
                }
                else
                {
                    var typeName = interfaceName ?? node.ContainingType.ToString();

                    if (node is IMethodSymbol method)
                    {
                        parentHandle = typeDefinitionMapping[typeName].MethodDefinitions[method][0];
                    }
                    else if (node is IPropertySymbol property)
                    {
                        parentHandle = typeDefinitionMapping[typeName].PropertyDefinitions[property];
                    }
                    else if (node is IEventSymbol @event)
                    {
                        parentHandle = typeDefinitionMapping[typeName].EventDefinitions[@event];
                    }
                    else
                    {
                        Logger.Log("node not recognized " + node.Kind + " name: " + node.Name);
                        continue;
                    }
                }

                // Add attributes from both the class member declaration and its interface member declaration.
                HashSet<AttributeData> attributes = new HashSet<AttributeData>(node.GetAttributes(), new AttributeDataComparer());
                if (typeDeclaration.ClassInterfaceMemberMapping.ContainsKey(node))
                {
                    attributes.UnionWith(typeDeclaration.ClassInterfaceMemberMapping[node].GetAttributes());
                }
                AddCustomAttributes(attributes, parentHandle);
            }
        }

        public void FinalizeGeneration()
        {
            Logger.Log("finalizing");
            var classTypeDeclarations = typeDefinitionMapping.Values
                .Where(declaration => declaration.Node is INamedTypeSymbol symbol && symbol.TypeKind == TypeKind.Class)
                .ToList();
            foreach (var classTypeDeclaration in classTypeDeclarations)
            {
                INamedTypeSymbol classSymbol = classTypeDeclaration.Node as INamedTypeSymbol;

                Logger.Log("finalizing class " + QualifiedName(classSymbol));
                foreach (var implementedInterface in GetInterfaces(classSymbol))
                {
                    var implementedInterfaceQualifiedNameWithGenerics = QualifiedName(implementedInterface, true);
                    if (!typeDefinitionMapping.ContainsKey(implementedInterfaceQualifiedNameWithGenerics))
                    {
                        AddType(implementedInterface);
                    }

                    Logger.Log("finalizing interface " + implementedInterfaceQualifiedNameWithGenerics);
                    var interfaceTypeDeclaration = typeDefinitionMapping[implementedInterfaceQualifiedNameWithGenerics];
                    if (MappedCSharpTypes.ContainsKey(QualifiedName(implementedInterface)))
                    {
                        Logger.Log("adding MethodImpls for custom mapped interface");
                        foreach (var interfaceMember in interfaceTypeDeclaration.MethodReferences)
                        {
                            var interfaceMemberMethodDefinitions = interfaceMember.Value;
                            var classMemberMethodDefinitions = classTypeDeclaration.MethodDefinitions[implementedInterface];
                            for (int idx = 0; idx < interfaceMemberMethodDefinitions.Count; idx++)
                            {
                                metadataBuilder.AddMethodImplementation(
                                    classTypeDeclaration.Handle,
                                    classMemberMethodDefinitions[idx],
                                    interfaceMemberMethodDefinitions[idx]);
                            }
                        }
                    }
                    else
                    {
                        Logger.Log("adding MethodImpls for interface");
                        foreach (var interfaceMember in interfaceTypeDeclaration.MethodReferences)
                        {
                            var classMember = classSymbol.FindImplementationForInterfaceMember(interfaceMember.Key);
                            if (classTypeDeclaration.MethodDefinitions.ContainsKey(classMember))
                            {
                                var interfaceMemberMethodDefinitions = interfaceMember.Value;
                                var classMemberMethodDefinitions = classTypeDeclaration.MethodDefinitions[classMember];
                                for (int idx = 0; idx < interfaceMemberMethodDefinitions.Count; idx++)
                                {
                                    metadataBuilder.AddMethodImplementation(
                                        classTypeDeclaration.Handle,
                                        classMemberMethodDefinitions[idx],
                                        interfaceMemberMethodDefinitions[idx]);
                                }

                                // If method overloaded in interface, overload in class too.
                                if (interfaceTypeDeclaration.OverloadedMethods.ContainsKey(interfaceMember.Key))
                                {
                                    AddOverloadAttribute(classMemberMethodDefinitions.First(), interfaceTypeDeclaration.OverloadedMethods[interfaceMember.Key]);
                                }
                            }
                        }
                    }
                }

                if (classTypeDeclaration.DefaultInterface != null)
                {
                    Logger.Log("finalizing default interface " + classTypeDeclaration.DefaultInterface);
                    var defaultInterfaceTypeDeclaration = typeDefinitionMapping[classTypeDeclaration.DefaultInterface];
                    foreach (var interfaceMember in defaultInterfaceTypeDeclaration.MethodReferences)
                    {
                        if (classTypeDeclaration.MethodDefinitions.ContainsKey(interfaceMember.Key))
                        {
                            var interfaceMemberMethodDefinitions = interfaceMember.Value;
                            var classMemberMethodDefinitions = classTypeDeclaration.MethodDefinitions[interfaceMember.Key];
                            for (int idx = 0; idx < interfaceMemberMethodDefinitions.Count; idx++)
                            {
                                metadataBuilder.AddMethodImplementation(
                                    classTypeDeclaration.Handle,
                                    classMemberMethodDefinitions[idx],
                                    interfaceMemberMethodDefinitions[idx]);
                            }

                            // If method overloaded in interface, overload in class too.
                            if (defaultInterfaceTypeDeclaration.OverloadedMethods.ContainsKey(interfaceMember.Key))
                            {
                                AddOverloadAttribute(classMemberMethodDefinitions.First(), defaultInterfaceTypeDeclaration.OverloadedMethods[interfaceMember.Key]);
                            }
                        }
                    }
                }

                if (classTypeDeclaration.StaticInterface != null)
                {
                    Logger.Log("finalizing static interface " + classTypeDeclaration.StaticInterface);
                    var staticInterfaceTypeDeclaration = typeDefinitionMapping[classTypeDeclaration.StaticInterface];
                    foreach (var interfaceMember in staticInterfaceTypeDeclaration.MethodReferences)
                    {
                        // If method overloaded in static interface, overload in class too.
                        if (classTypeDeclaration.MethodDefinitions.ContainsKey(interfaceMember.Key) &&
                            staticInterfaceTypeDeclaration.OverloadedMethods.ContainsKey(interfaceMember.Key))
                        {
                            AddOverloadAttribute(
                                classTypeDeclaration.MethodDefinitions[interfaceMember.Key].First(),
                                staticInterfaceTypeDeclaration.OverloadedMethods[interfaceMember.Key]
                            );
                        }
                    }
                }
            }

            Logger.Log("adding default version attributes");
            var declarations = typeDefinitionMapping.Values
                .Where(declaration => declaration.Node != null)
                .ToList();
            foreach (var declaration in declarations)
            {
                INamedTypeSymbol namedType = declaration.Node as INamedTypeSymbol;
                string qualifiedNameWithGenerics = QualifiedName(namedType, true);
                if (typeDefinitionMapping[qualifiedNameWithGenerics].Handle != default && GetVersion(namedType) == -1)
                {
                    AddDefaultVersionAttribute(typeDefinitionMapping[qualifiedNameWithGenerics].Handle);
                }
            }

            Logger.Log("adding custom attributes");
            var typeDeclarationsWithAttributes = typeDefinitionMapping.Values
                .Where(declaration => !declaration.IsSynthesizedInterface && declaration.SymbolsWithAttributes.Any())
                .ToList();
            foreach (var typeDeclaration in typeDeclarationsWithAttributes)
            {
                AddCustomAttributes(typeDeclaration);

                if (typeDeclaration.Node is INamedTypeSymbol symbol && symbol.TypeKind == TypeKind.Class)
                {
                    if (!string.IsNullOrEmpty(typeDeclaration.DefaultInterface))
                    {
                        Logger.Log("adding attributes for default interface " + typeDeclaration.DefaultInterface);
                        AddCustomAttributes(typeDefinitionMapping[typeDeclaration.DefaultInterface], typeDeclaration.DefaultInterface);
                    }

                    if (!string.IsNullOrEmpty(typeDeclaration.StaticInterface))
                    {
                        Logger.Log("adding attributes for static interface " + typeDeclaration.StaticInterface);
                        AddCustomAttributes(typeDefinitionMapping[typeDeclaration.StaticInterface], typeDeclaration.StaticInterface);
                    }
                }
            }
        }

        public bool IsPublic(ISymbol type)
        {
            return type.DeclaredAccessibility == Accessibility.Public ||
                type is IMethodSymbol method && !method.ExplicitInterfaceImplementations.IsDefaultOrEmpty ||
                type is IPropertySymbol property && !property.ExplicitInterfaceImplementations.IsDefaultOrEmpty ||
                type is IEventSymbol @event && !@event.ExplicitInterfaceImplementations.IsDefaultOrEmpty;
        }

        public void GetNamespaceAndTypename(string qualifiedName, out string @namespace, out string typename)
        {
            var idx = qualifiedName.LastIndexOf('.');
            if (idx == -1)
            {
                @namespace = "";
                typename = qualifiedName;
            }
            else
            {
                @namespace = qualifiedName.Substring(0, idx);
                typename = qualifiedName.Substring(idx + 1);
            }
        }

        public string QualifiedName(string @namespace, string identifier)
        {
            if (string.IsNullOrEmpty(@namespace))
            {
                return identifier;
            }
            return string.Join(".", @namespace, identifier);
        }

        public static string GetGenericName(ISymbol symbol, bool includeGenerics = false)
        {
            string name = symbol.Name;
            if (symbol is INamedTypeSymbol namedType && namedType.TypeArguments.Length != 0)
            {
                name += "`" + namedType.TypeArguments.Length;
                if (includeGenerics)
                {
                    name += string.Format("<{0}>", string.Join(", ", namedType.TypeArguments));
                }
            }
            return name;
        }

        public string QualifiedName(ISymbol symbol, bool includeGenerics = false)
        {
            return QualifiedName(symbol.ContainingNamespace.ToString(), GetGenericName(symbol, includeGenerics));
        }
    }
}