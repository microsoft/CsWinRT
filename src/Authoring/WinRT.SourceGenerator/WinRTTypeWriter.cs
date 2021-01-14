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
            :this(null)
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

        private readonly Stack<string> namespaces = new Stack<string>();
        private readonly Dictionary<string, TypeReferenceHandle> typeReferenceMapping;
        private readonly Dictionary<string, EntityHandle> assemblyReferenceMapping;
        private readonly MetadataBuilder metadataBuilder;
        private bool hasConstructor, hasDefaultConstructor;

        private readonly Dictionary<string, TypeDeclaration> typeDefinitionMapping;
        private TypeDeclaration currentTypeDeclaration;

        public WinRTTypeWriter(
            string assembly,
            string version,
            MetadataBuilder metadataBuilder)
        {
            this.assembly = assembly;
            this.version = version;
            this.metadataBuilder = metadataBuilder;
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
                // TODO check if from overridable interface
                if (!isOverridable)
                {
                    methodAttributes |= MethodAttributes.Final;
                }
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

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            Logger.Log("method: " + node.Identifier.ValueText);
            var methodSymbol = Model.GetDeclaredSymbol(node);
            if (!IsPublicNode(node) || currentTypeDeclaration.CustomMappedSymbols.Contains(methodSymbol))
            {
                Logger.Log("method skipped");
                return;
            }

            base.VisitMethodDeclaration(node);

            Parameter[] parameters = Parameter.GetParameters(node.ParameterList, Model);
            var methodDefinitionHandle = AddMethodDefinition(
                methodSymbol.Name,
                parameters,
                new Symbol(methodSymbol.ReturnType),
                IsStaticNode(node),
                node.Parent is InterfaceDeclarationSyntax);
            currentTypeDeclaration.AddMethod(methodSymbol, methodSymbol.Name, methodDefinitionHandle);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (!IsPublicNode(node) || node.Parent is not StructDeclarationSyntax)
            {
                return;
            }

            base.VisitFieldDeclaration(node);

            var symbol = Model.GetTypeInfo(node.Declaration.Type).Type;
            var fieldSignature = new BlobBuilder();
            var encoder = new BlobEncoder(fieldSignature);

            if (IsEncodableAsSpecialType(symbol.SpecialType))
            {
                EncodeSpecialType(symbol.SpecialType, encoder.FieldSignature());
            }
            else
            {
                EncodeSymbol(new Symbol(symbol), encoder.FieldSignature());
            }

            foreach (var variable in node.Declaration.Variables)
            {
                var fieldSymbol = Model.GetDeclaredSymbol(variable);
                var fieldDefinitionHandle = metadataBuilder.AddFieldDefinition(
                    FieldAttributes.Public,
                    metadataBuilder.GetOrAddString(variable.Identifier.Text),
                    metadataBuilder.GetOrAddBlob(fieldSignature));
                currentTypeDeclaration.AddField(fieldSymbol, fieldDefinitionHandle);
            }
        }

        public void AddPropertyDefinition(
            string propertyName,
            Symbol type,
            ISymbol symbol,
            bool hasSetMethod,
            bool isInterfaceParent)
        {
            Logger.Log("defining property " + propertyName);

            var propertySignature = new BlobBuilder();
            new BlobEncoder(propertySignature)
                .PropertySignature(true)
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
                string setMethodName = "put_" + propertyName;
                var setMethod = AddMethodDefinition(
                    setMethodName,
                    new Parameter[] { new Parameter(type, "value", ParameterAttributes.In) },
                    null,
                    false,
                    isInterfaceParent,
                    true);
                currentTypeDeclaration.AddMethod(symbol, setMethodName, setMethod);

                metadataBuilder.AddMethodSemantics(
                    propertyDefinitonHandle,
                    MethodSemanticsAttributes.Setter,
                    setMethod);
            }

            string getMethodName = "get_" + propertyName;
            var getMethod = AddMethodDefinition(
                getMethodName,
                new Parameter[0],
                type,
                false,
                isInterfaceParent,
                true);
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
                property.SetMethod != null && property.SetMethod.DeclaredAccessibility == Accessibility.Public,
                isInterfaceParent
            );
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var symbol = Model.GetDeclaredSymbol(node);
            if (!IsPublicNode(node) || currentTypeDeclaration.CustomMappedSymbols.Contains(symbol))
            {
                return;
            }

            base.VisitPropertyDeclaration(node);

            AddPropertyDeclaration(symbol, node.Parent is InterfaceDeclarationSyntax);
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
            // Mark custom mapped interface members for removal later.
            // Note we want to also mark members from interfaces without mappings.
            foreach (var implementedInterface in GetInterfaces(classSymbol, true).
                Where(symbol => MappedCSharpTypes.ContainsKey(QualifiedName(symbol)) ||
                                ImplementedInterfacesWithoutMapping.Contains(QualifiedName(symbol))))
            {
                string interfaceName = QualifiedName(implementedInterface);
                Logger.Log("custom mapped interface: " + interfaceName);
                foreach (var interfaceMember in implementedInterface.GetMembers())
                {
                    var classMember = classSymbol.FindImplementationForInterfaceMember(interfaceMember);
                    currentTypeDeclaration.CustomMappedSymbols.Add(classMember);
                }
            }

            foreach (var implementedInterface in GetInterfaces(classSymbol)
                        .Where(symbol => MappedCSharpTypes.ContainsKey(QualifiedName(symbol))))
            {
                WriteCustomMappedTypeMembers(implementedInterface, true);
            }
        }

        INamedTypeSymbol GetTypeByMetadataName(string metadataName)
        {
            var namedType = Model.Compilation.GetTypeByMetadataName(metadataName);
            if(namedType != null)
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

        private void WriteCustomMappedTypeMembers(INamedTypeSymbol symbol, bool isDefinition)
        {
            var (_, mappedTypeName, _, _, _) = MappedCSharpTypes[QualifiedName(symbol)].GetMapping(currentTypeDeclaration.Node);
            Logger.Log("writing custom mapped type members for " + mappedTypeName);
            void AddMethod(string name, Parameter[] parameters, Symbol returnType)
            {
                parameters ??= new Parameter[0];
                if (isDefinition)
                {
                    var methodDefinitionHandle = AddMethodDefinition(name, parameters, returnType, false, false);
                    currentTypeDeclaration.AddMethod(symbol, name, methodDefinitionHandle);
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
                    AddPropertyDefinition(name, type, symbol, setProperty, false);
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
                    AddEventDeclaration(name, eventType.Type, symbol, false);
                }
                else
                {
                    AddEventReference(name, eventType.Type, symbol);
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
            foreach(var @interface in symbol.Interfaces)
            {
                interfaces.Add(@interface);
                interfaces.UnionWith(@interface.AllInterfaces);
            }

            var baseType = symbol.BaseType;
            while(baseType != null && !IsWinRTType(baseType))
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
            if(!includeInterfacesWithoutMappings && 
                interfaces.Any(@interface => QualifiedName(@interface) == "System.Collections.Generic.IEnumerable`1"))
            {
                interfaces.Remove(GetTypeByMetadataName("System.Collections.IEnumerable"));
            }

            return interfaces.Where(@interface => 
                    includeInterfacesWithoutMappings || 
                    !ImplementedInterfacesWithoutMapping.Contains(QualifiedName(@interface)))
                .OrderBy(implementedInterface => implementedInterface.ToString());
        }

        private void ProcessTypeDeclaration(BaseTypeDeclarationSyntax node, Action visitTypeDeclaration)
        {
            if (!IsPublicNode(node))
            {
                return;
            }

            var symbol = Model.GetDeclaredSymbol(node);
            currentTypeDeclaration = new TypeDeclaration(symbol);

            if (node is ClassDeclarationSyntax)
            {
                ProcessCustomMappedInterfaces(symbol);
            }

            visitTypeDeclaration();

            TypeAttributes typeAttributes =
                TypeAttributes.Public |
                TypeAttributes.WindowsRuntime |
                TypeAttributes.AutoLayout |
                TypeAttributes.AnsiClass;

            if (IsSealedNode(node) ||
                IsStaticNode(node) ||
                (node is EnumDeclarationSyntax ||
                    node is StructDeclarationSyntax))
            {
                typeAttributes |= TypeAttributes.Sealed;
            }

            if (node is ClassDeclarationSyntax && IsStaticNode(node))
            {
                typeAttributes |= TypeAttributes.Abstract;
            }

            EntityHandle baseType = default;
            if (node is InterfaceDeclarationSyntax)
            {
                typeAttributes |=
                    TypeAttributes.Interface |
                    TypeAttributes.Abstract;
            }
            else if (node is ClassDeclarationSyntax)
            {
                typeAttributes |=
                    TypeAttributes.Class |
                    TypeAttributes.BeforeFieldInit;

                // extends
                if (node.BaseList != null)
                {
                    foreach (var type in node.BaseList.Types)
                    {
                        var typeSymbol = Model.GetTypeInfo(type.Type).Type;
                        if (typeSymbol.TypeKind == TypeKind.Class)
                        {
                            baseType = GetTypeReference(typeSymbol);
                            break;
                        }
                    }
                }

                if (baseType == default)
                {
                    baseType = GetTypeReference("System", "Object", "mscorlib");
                }
            }
            else if (node is StructDeclarationSyntax)
            {
                typeAttributes |= TypeAttributes.SequentialLayout;
                baseType = GetTypeReference("System", "ValueType", "mscorlib");
            }
            else if (node is EnumDeclarationSyntax)
            {
                baseType = GetTypeReference("System", "Enum", "mscorlib");
            }

            var typeDefinitionHandle = AddTypeDefinition(
                typeAttributes,
                GetNamespace(),
                node.Identifier.ValueText,
                baseType);
            currentTypeDeclaration.Handle = typeDefinitionHandle;

            if (node.BaseList != null && (node is InterfaceDeclarationSyntax || node is ClassDeclarationSyntax))
            {
                // Interface implementations need to be added in order of class and then typespec.  Given entries are added
                // per class, that is already in order, but we need to sort by typespec.
                List<KeyValuePair<INamedTypeSymbol, EntityHandle>> implementedInterfaces = new List<KeyValuePair<INamedTypeSymbol, EntityHandle>>();
                foreach (var implementedInterface in GetInterfaces(symbol))
                {
                    implementedInterfaces.Add(new KeyValuePair<INamedTypeSymbol, EntityHandle>(implementedInterface, GetTypeSpecification(implementedInterface)));
                }
                implementedInterfaces.Sort((x, y) => CodedIndex.TypeDefOrRefOrSpec(x.Value).CompareTo(CodedIndex.TypeDefOrRefOrSpec(y.Value)));

                foreach (var implementedInterface in implementedInterfaces)
                {
                    var interfaceImplHandle = metadataBuilder.AddInterfaceImplementation(
                        typeDefinitionHandle,
                        implementedInterface.Value);
                    currentTypeDeclaration.AddInterfaceImpl(implementedInterface.Key, interfaceImplHandle);
                }
            }

            typeDefinitionMapping[QualifiedName(node.Identifier.ValueText)] = currentTypeDeclaration;
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            ProcessTypeDeclaration(node, () => base.VisitInterfaceDeclaration(node));

            var interfaceTypeDeclaration = currentTypeDeclaration;
            AddGuidAttribute(interfaceTypeDeclaration.Handle, interfaceTypeDeclaration.Node.ToString());
            AddOverloadAttributeForInterfaceMethods(interfaceTypeDeclaration);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            void processClassDeclaration()
            {
                hasConstructor = false;
                hasDefaultConstructor = false;
                base.VisitClassDeclaration(node);

                // implicit constructor if none defined
                if (!hasConstructor && !IsStaticNode(node))
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
                    var symbol = Model.GetDeclaredSymbol(node);
                    currentTypeDeclaration.AddMethod(symbol, constructorMethodName, methodDefinitionHandle);
                    hasDefaultConstructor = true;
                }
            }

            ProcessTypeDeclaration(node, processClassDeclaration);

            if (IsPublicNode(node))
            {
                var classDeclaration = currentTypeDeclaration;
                var classSymbol = classDeclaration.Node as INamedTypeSymbol;

                if (hasDefaultConstructor)
                {
                    AddActivatableAttribute(
                        classDeclaration.Handle,
                        (uint)GetVersion(classSymbol, true),
                        null);
                }
                AddSynthesizedInterfaces(classDeclaration);

                // No synthesized default interface generated
                if (classDeclaration.DefaultInterface == null && classSymbol.Interfaces.Length != 0)
                {
                    AddDefaultInterfaceImplAttribute(classDeclaration.InterfaceImplDefinitions[classSymbol.Interfaces[0]]);
                }
            }
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            ProcessTypeDeclaration(node, () => base.VisitStructDeclaration(node));
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

        private void EncodeCustomElementType(IFieldSymbol field, CustomAttributeElementTypeEncoder typeEncoder)
        {
            switch (field.Type.SpecialType)
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
                    typeEncoder.Enum(field.Type.ToString());
                    break;
                case SpecialType.System_SByte:
                    typeEncoder.SByte();
                    break;
                default:
                    Logger.Log("TODO special type: " + field.Type.SpecialType);
                    break;
            }
        }

        private void EncodeNamedArgumentType(INamedTypeSymbol attributeType, string field, NamedArgumentTypeEncoder encoder)
        {
            Logger.Log("encoding named type");
            var fieldMembers = attributeType.GetMembers(field);
            Logger.Log("# fields: " + fieldMembers.Count());
            var fieldSymbol = fieldMembers.First() as IFieldSymbol;
            Logger.Log("found field: " + fieldSymbol);

            if (fieldSymbol.Type.SpecialType == SpecialType.System_Object)
            {
                encoder.Object();
            }
            else if (fieldSymbol.Type.SpecialType == SpecialType.System_Array)
            {
                // TODO array type encoder
                encoder.SZArray();
            }
            else
            {
                EncodeCustomElementType(fieldSymbol, encoder.ScalarType());
            }
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

                encoder.AddArgument(
                    true,
                    type => EncodeNamedArgumentType(attributeType, argument.Key, type),
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
                Logger.Log("adding attribute type");
                AddType(attributeType);
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
                    AddType(attributeType);
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

        public override void VisitAttributeList(AttributeListSyntax node)
        {
            base.VisitAttributeList(node);

            // Skip assembly attributes
            if (node.Target != null && node.Target.Identifier.ValueText == "assembly")
            {
                return;
            }

            var parentSymbol = Model.GetDeclaredSymbol(node.Parent);
            currentTypeDeclaration.SymbolsWithAttributes.Add(parentSymbol);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            void processEnumDeclaration()
            {
                var enumTypeFieldAttributes =
                    FieldAttributes.Private |
                    FieldAttributes.SpecialName |
                    FieldAttributes.RTSpecialName;

                var symbol = Model.GetDeclaredSymbol(node);
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

            ProcessTypeDeclaration(node, processEnumDeclaration);
        }

        public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
        {
            base.VisitEnumMemberDeclaration(node);

            var enumFieldAttributes =
                FieldAttributes.Public |
                FieldAttributes.Static |
                FieldAttributes.Literal |
                FieldAttributes.HasDefault;

            var symbol = Model.GetDeclaredSymbol(node);
            var fieldSignature = new BlobBuilder();
            var encoder = new BlobEncoder(fieldSignature);
            EncodeSymbol(new Symbol(symbol.ContainingType), encoder.FieldSignature());

            var fieldDefinitionHandle = metadataBuilder.AddFieldDefinition(
                enumFieldAttributes,
                metadataBuilder.GetOrAddString(node.Identifier.Text),
                metadataBuilder.GetOrAddBlob(fieldSignature));
            currentTypeDeclaration.AddField(symbol, fieldDefinitionHandle);

            if (symbol.HasConstantValue)
            {
                metadataBuilder.AddConstant(fieldDefinitionHandle, symbol.ConstantValue);
            }
        }

        public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            if (!IsPublicNode(node))
            {
                return;
            }

            var symbol = Model.GetDeclaredSymbol(node);
            currentTypeDeclaration = new TypeDeclaration(symbol);

            base.VisitDelegateDeclaration(node);

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
                GetNamespace(),
                node.Identifier.ValueText,
                GetTypeReference("System", "MulticastDelegate", "mscorlib"));
            currentTypeDeclaration.Handle = typeDefinitionHandle;
            AddGuidAttribute(typeDefinitionHandle, symbol.ToString());
        }

        public void AddEventDeclaration(string eventName, ITypeSymbol eventType, ISymbol symbol, bool isInterfaceParent)
        {
            Logger.Log("defining event " + eventName + " with type " + eventType.ToString());

            var delegateSymbolType = eventType as INamedTypeSymbol;
            EntityHandle typeReferenceHandle = GetTypeSpecification(delegateSymbolType);
            EntityHandle eventRegistrationTokenTypeHandle = GetTypeReference("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract");
            Symbol eventRegistrationToken = new Symbol(eventRegistrationTokenTypeHandle);

            var eventDefinitionHandle = metadataBuilder.AddEvent(
                EventAttributes.None,
                metadataBuilder.GetOrAddString(eventName),
                typeReferenceHandle);
            currentTypeDeclaration.AddEvent(symbol, eventDefinitionHandle);

            string addMethodName = "add_" + eventName;
            var addMethod = AddMethodDefinition(
                addMethodName,
                new Parameter[] { new Parameter(delegateSymbolType, "handler", ParameterAttributes.In) },
                eventRegistrationToken,
                false,
                isInterfaceParent,
                true);
            currentTypeDeclaration.AddMethod(symbol, addMethodName, addMethod);

            metadataBuilder.AddMethodSemantics(
                eventDefinitionHandle,
                MethodSemanticsAttributes.Adder,
                addMethod);

            string removeMethodName = "remove_" + eventName;
            var removeMethod = AddMethodDefinition(
                removeMethodName,
                new Parameter[] { new Parameter(eventRegistrationToken, "token", ParameterAttributes.In) },
                null,
                false,
                isInterfaceParent,
                true);
            currentTypeDeclaration.AddMethod(symbol, removeMethodName, removeMethod);

            metadataBuilder.AddMethodSemantics(
                eventDefinitionHandle,
                MethodSemanticsAttributes.Remover,
                removeMethod);
        }

        public void AddEventDeclaration(IEventSymbol @event, bool isInterfaceParent)
        {
            AddEventDeclaration(@event.Name, @event.Type, @event, isInterfaceParent);
        }

        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            if (!IsPublicNode(node))
            {
                return;
            }

            base.VisitEventFieldDeclaration(node);

            foreach (var declaration in node.Declaration.Variables)
            {
                var eventSymbol = Model.GetDeclaredSymbol(declaration) as IEventSymbol;
                if (currentTypeDeclaration.CustomMappedSymbols.Contains(eventSymbol))
                {
                    Logger.Log("event skipped");
                    continue;
                }

                AddEventDeclaration(eventSymbol, node.Parent is InterfaceDeclarationSyntax);
            }
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            hasConstructor = true;

            if (!IsPublicNode(node))
            {
                return;
            }

            base.VisitConstructorDeclaration(node);

            var symbol = Model.GetDeclaredSymbol(node);
            Parameter[] parameters = Parameter.GetParameters(node.ParameterList, Model);
            var methodDefinitionHandle = AddMethodDefinition(
                ".ctor",
                parameters,
                null,
                false,
                false,
                true,
                true,
                true);
            currentTypeDeclaration.AddMethod(symbol, ".ctor", methodDefinitionHandle);
            hasDefaultConstructor |= (parameters.Length == 0);
        }

        void AddMethodDeclaration(IMethodSymbol method, bool isInterfaceParent)
        {
            Logger.Log("add method from symbol: " + method.Name);

            string methodName = method.MethodKind == MethodKind.Constructor ? ".ctor" : method.Name;
            Parameter[] parameters = Parameter.GetParameters(method);
            var methodDefinitionHandle = AddMethodDefinition(
                methodName,
                parameters,
                new Symbol(method.ReturnType),
                !isInterfaceParent && method.IsStatic,
                isInterfaceParent,
                method.MethodKind == MethodKind.Constructor,
                true,
                true);
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

        void AddExternalType(INamedTypeSymbol type)
        {
            // TODO: check if custom projected interface
            // TODO: block or warn type names with namespaces not meeting WinRT requirements.
            // TODO: synthesized interfaces and default interface impl.
            // TODO: extends

            Logger.Log("external type: " + type.ToString());

            var typeDeclaration = new TypeDeclaration(type);
            currentTypeDeclaration = typeDeclaration;
            bool isInterface = type.TypeKind == TypeKind.Interface;

            foreach (var member in type.GetMembers())
            {
                if (member is IMethodSymbol method &&
                    (method.MethodKind == MethodKind.Ordinary || method.MethodKind == MethodKind.Constructor))
                {
                    AddMethodDeclaration(method, isInterface);
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
                }
            }

            TypeAttributes typeAttributes =
                TypeAttributes.Public |
                TypeAttributes.WindowsRuntime |
                TypeAttributes.AutoLayout |
                TypeAttributes.AnsiClass |
                TypeAttributes.Interface |
                TypeAttributes.Abstract;

            var typeDefinitionHandle = AddTypeDefinition(
                typeAttributes,
                type.ContainingNamespace.ToString(),
                type.Name,
                default);
            typeDeclaration.Handle = typeDefinitionHandle;
            typeDefinitionMapping[type.ToString()] = typeDeclaration;

            foreach (var implementedInterface in GetInterfaces(type))
            {
                var interfaceImplHandle = metadataBuilder.AddInterfaceImplementation(
                    typeDefinitionHandle,
                    GetTypeReference(implementedInterface));
                typeDeclaration.AddInterfaceImpl(implementedInterface, interfaceImplHandle);
            }

            if (isInterface)
            {
                AddGuidAttribute(typeDefinitionHandle, type.ToString());
                AddOverloadAttributeForInterfaceMethods(typeDeclaration);
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

        public void AddEventReference(string eventName, ITypeSymbol eventType, INamedTypeSymbol parent)
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
            currentTypeDeclaration.AddMethodReference(eventType, addMethodReference);

            var removeMethodReference = AddMethodReference(
                "remove_" + eventName,
                new Parameter[] { new Parameter(eventRegistrationToken, "token", ParameterAttributes.In) },
                null,
                parent,
                false);
            currentTypeDeclaration.AddMethodReference(eventType, removeMethodReference);
        }

        public void AddEventReference(IEventSymbol @event)
        {
            AddEventReference(@event.Name, @event.Type, @event.ContainingType);
        }

        void AddProjectedType(INamedTypeSymbol type, string projectedTypeOverride = null)
        {
            currentTypeDeclaration = new TypeDeclaration(type);

            foreach (var member in type.GetMembers())
            {
                if (member is IMethodSymbol method &&
                    (method.MethodKind == MethodKind.Ordinary || method.MethodKind == MethodKind.Constructor))
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

            typeDefinitionMapping[projectedTypeOverride ?? QualifiedName(type)] = currentTypeDeclaration;
        }

        void AddMappedType(INamedTypeSymbol type)
        {
            currentTypeDeclaration = new TypeDeclaration(type);
            WriteCustomMappedTypeMembers(type, false);
            typeDefinitionMapping[QualifiedName(type)] = currentTypeDeclaration;
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

        void AddAttributeIfAny(ISymbol symbol, TypeDeclaration classDeclaration)
        {
            if (classDeclaration.SymbolsWithAttributes.Contains(symbol))
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
                    AddAttributeIfAny(classMember.Key, classDeclaration);
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

                    AddAttributeIfAny(classMember.Key, classDeclaration);
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

        void AddType(INamedTypeSymbol type)
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
            else
            {
                AddExternalType(type);
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
                if(typeDeclaration.ClassInterfaceMemberMapping.ContainsKey(node))
                {
                    attributes.UnionWith(typeDeclaration.ClassInterfaceMemberMapping[node].GetAttributes());
                }
                AddCustomAttributes(attributes, parentHandle);
            }
        }

        public void FinalizeGeneration()
        {
            var classTypeDeclarations = typeDefinitionMapping.Values
                .Where(declaration => declaration.Node is INamedTypeSymbol symbol && symbol.TypeKind == TypeKind.Class)
                .ToList();
            foreach (var classTypeDeclaration in classTypeDeclarations)
            {
                INamedTypeSymbol classSymbol = classTypeDeclaration.Node as INamedTypeSymbol;

                foreach (var implementedInterface in GetInterfaces(classSymbol))
                {
                    var implementedInterfaceQualifiedName = QualifiedName(implementedInterface);
                    if (!typeDefinitionMapping.ContainsKey(implementedInterfaceQualifiedName))
                    {
                        AddType(implementedInterface);
                    }

                    var interfaceTypeDeclaration = typeDefinitionMapping[implementedInterfaceQualifiedName];
                    if (MappedCSharpTypes.ContainsKey(implementedInterfaceQualifiedName))
                    {
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

            var interfaceDeclarations = typeDefinitionMapping.Values
                .Where(declaration => declaration.Node is INamedTypeSymbol symbol && symbol.TypeKind == TypeKind.Interface)
                .ToList();
            foreach (var interfaceDeclaration in interfaceDeclarations)
            {
                INamedTypeSymbol interfaceSymbol = interfaceDeclaration.Node as INamedTypeSymbol;
                if (typeDefinitionMapping[QualifiedName(interfaceSymbol)].Handle != default && GetVersion(interfaceSymbol) == -1)
                {
                    AddDefaultVersionAttribute(typeDefinitionMapping[QualifiedName(interfaceSymbol)].Handle);
                }
            }

            var typeDeclarationsWithAttributes = typeDefinitionMapping.Values
                .Where(declaration => !declaration.IsSynthesizedInterface && declaration.SymbolsWithAttributes.Any())
                .ToList();
            foreach (var typeDeclaration in typeDeclarationsWithAttributes)
            {
                AddCustomAttributes(typeDeclaration);

                if (typeDeclaration.Node is INamedTypeSymbol symbol && symbol.TypeKind == TypeKind.Class)
                {
                    if(!string.IsNullOrEmpty(typeDeclaration.DefaultInterface))
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

        public bool IsPublicNode(MemberDeclarationSyntax node)
        {
            return node.Modifiers.Any(modifier => modifier.ValueText == "public") ||
                (node.Parent is InterfaceDeclarationSyntax && node.Modifiers.Count == 0);
        }

        public bool IsPrivateNode(AccessorDeclarationSyntax node)
        {
            return node.Modifiers.Any(modifier => modifier.ValueText == "private");
        }

        public bool IsStaticNode(MemberDeclarationSyntax node)
        {
            return node.Modifiers.Any(modifier => modifier.ValueText == "static");
        }

        public bool IsSealedNode(MemberDeclarationSyntax node)
        {
            return node.Modifiers.Any(modifier => modifier.ValueText == "sealed");
        }

        public string GetNamespace()
        {
            return string.Join(".", namespaces);
        }

        public string QualifiedName(string identifier)
        {
            return QualifiedName(GetNamespace(), identifier);
        }

        public string QualifiedName(string @namespace, string identifier)
        {
            return string.Join(".", @namespace, identifier);
        }

        public static string GetGenericName(ISymbol symbol)
        {
            string name = symbol.Name;
            if (symbol is INamedTypeSymbol namedType && namedType.TypeArguments.Length != 0)
            {
                name += "`" + namedType.TypeArguments.Length;
            }
            return name;
        }

        public string QualifiedName(ISymbol symbol)
        {
            return QualifiedName(symbol.ContainingNamespace.ToString(), GetGenericName(symbol));
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            namespaces.Push(node.Name.ToString());
            base.VisitNamespaceDeclaration(node);
            namespaces.Pop();
        }
    }
}