using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
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

        public Parameter(Symbol type, string name, ParameterAttributes attributes)
        {
            Type = type;
            Name = name;
            Attributes = attributes;
        }

        public Parameter(ITypeSymbol type, string name, ParameterAttributes attributes)
            : this(new Symbol(type), name, attributes)
        {
        }

        public Parameter(EntityHandle type, string name, ParameterAttributes attributes)
            : this(new Symbol(type), name, attributes)
        {
        }

        public Parameter(ParameterSyntax parameter, ParameterAttributes attributes, SemanticModel model)
        {
            Type = new Symbol(model.GetDeclaredSymbol(parameter).Type);
            Name = parameter.Identifier.ValueText;
            Attributes = attributes;
        }
    }

    class Symbol
    {
        public ITypeSymbol Type;
        public EntityHandle Handle;

        public Symbol(ITypeSymbol type)
        {
            Type = type;
            Handle = default;
        }

        public Symbol(EntityHandle handle)
        {
            Type = default;
            Handle = handle;
        }

        public bool IsHandle()
        {
            return Handle != default;
        }
    }

    class TypeDeclaration
    {
        public readonly ISymbol Node;
        public TypeDefinitionHandle Handle;
        public string DefaultInterface;
        public string StaticInterface;

        public Dictionary<ISymbol, List<MethodDefinitionHandle>> MethodDefinitions = new Dictionary<ISymbol, List<MethodDefinitionHandle>>();
        public Dictionary<ISymbol, List<EntityHandle>> MethodReferences = new Dictionary<ISymbol, List<EntityHandle>>();
        public Dictionary<ISymbol, FieldDefinitionHandle> FieldDefinitions = new Dictionary<ISymbol, FieldDefinitionHandle>();
        public Dictionary<ISymbol, PropertyDefinitionHandle> PropertyDefinitions = new Dictionary<ISymbol, PropertyDefinitionHandle>();
        public Dictionary<ISymbol, EventDefinitionHandle> EventDefinitions = new Dictionary<ISymbol, EventDefinitionHandle>();
        public Dictionary<ISymbol, InterfaceImplementationHandle> InterfaceImplDefinitions = new Dictionary<ISymbol, InterfaceImplementationHandle>();
        public Dictionary<string, List<ISymbol>> MethodsByName = new Dictionary<string, List<ISymbol>>();
        public Dictionary<ISymbol, string> OverloadedMethods = new Dictionary<ISymbol, string>();

        public TypeDeclaration(ISymbol node)
        {
            Node = node;
            Handle = default;
        }

        public override string ToString()
        {
            return Node.ToString();
        }

        public void AddMethod(ISymbol node, string name, MethodDefinitionHandle handle)
        {
            if(!MethodDefinitions.ContainsKey(node))
            {
                MethodDefinitions[node] = new List<MethodDefinitionHandle>();
                MethodReferences[node] = new List<EntityHandle>();
            }

            if(!MethodsByName.ContainsKey(name))
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
        private struct MappedType
        {
            public readonly string @namespace;
            public readonly string name;
            public readonly string assembly;

            public MappedType(string @namespace, string name, string assembly)
            {
                this.@namespace = @namespace;
                this.name = name;
                this.assembly = assembly;
            }
        }

        private static readonly Dictionary<string, MappedType> MappedCSharpTypes = new Dictionary<string, MappedType>()
        {
            { "System.Nullable", new MappedType("Windows.Foundation", "IReference`1", "Windows.Foundation.FoundationContract" ) },
            { "System.FlagsAttribute", new MappedType("System", "FlagsAttribute", "mscorlib" ) }
        };

        public SemanticModel Model;

        private readonly string assembly;
        private readonly string version;

        private readonly Stack<string> namespaces = new Stack<string>();
        private readonly Dictionary<string, TypeReferenceHandle> typeReferenceMapping;
        private readonly Dictionary<string, EntityHandle> assemblyReferenceMapping;
        private readonly List<ISymbol> nodesWithAttributes;
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
            nodesWithAttributes = new List<ISymbol>();

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
            if(assembly == "mscorlib")
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

        public string GetAssemblyForWinRTType(ISymbol type)
        {
            var winrtTypeAttribute = type.GetAttributes().
                Where(attribute => attribute.AttributeClass.Name == "WindowsRuntimeTypeAttribute");
            if (winrtTypeAttribute.Any())
            {
                return (string) winrtTypeAttribute.First().ConstructorArguments[0].Value;
            }

            return null;
        }

        private EntityHandle GetTypeReference(ISymbol symbol)
        {
            string @namespace = symbol.ContainingNamespace.ToString();
            string name = symbol.Name;
            string fullType = QualifiedName(@namespace, name);

            var assembly = GetAssemblyForWinRTType(symbol);
            if (assembly == null)
            {   
                if (MappedCSharpTypes.ContainsKey(fullType))
                {
                    Logger.Log("Mapping known type: " + fullType);
                    var newType = MappedCSharpTypes[fullType];
                    @namespace = newType.@namespace;
                    name = newType.name;
                    assembly = newType.assembly;

                    Logger.Log("Mapping " + fullType + " to " + QualifiedName(@namespace, name) + " from " + assembly);
                }
                else
                {
                    assembly = symbol.ContainingAssembly.Name;
                }
            }

            return GetTypeReference(@namespace, name, assembly);
        }

        private void EncodeSymbol(Symbol symbol, SignatureTypeEncoder typeEncoder)
        {
            if (symbol.IsHandle())
            {
                typeEncoder.Type(symbol.Handle, false);
            }
            else if(symbol.Type is INamedTypeSymbol namedType && namedType.TypeArguments.Length != 0)
            {
                Logger.Log("generic type");
                var genericType = typeEncoder.GenericInstantiation(GetTypeReference(symbol.Type), namedType.TypeArguments.Length, false);
                foreach (var typeArgument in namedType.TypeArguments)
                {
                    EncodeSymbol(new Symbol(typeArgument), genericType.AddArgument());
                }
            }
            else if(symbol.Type.SpecialType != SpecialType.None)
            {
                EncodeSpecialType(symbol.Type.SpecialType, typeEncoder);
            }
            else
            {
                typeEncoder.Type(GetTypeReference(symbol.Type), symbol.Type.TypeKind == TypeKind.Enum);
            }
        }

        private void EncodeReturnType(Symbol symbol, ReturnTypeEncoder returnTypeEncoder)
        {
            if(symbol == null)
            {
                returnTypeEncoder.Void();
            }
            else if(symbol.IsHandle() || symbol.Type.SpecialType == SpecialType.None)
            {
                EncodeSymbol(symbol, returnTypeEncoder.Type());
            }
            else if(symbol.Type.SpecialType == SpecialType.System_Void)
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

                if (!parameterType.IsHandle() && parameterType.Type.SpecialType != SpecialType.None)
                {
                    EncodeSpecialType(parameterType.Type.SpecialType, parameterTypeEncoder.Type());
                }
                else
                {
                    EncodeSymbol(parameterType, parameterTypeEncoder.Type());
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
            else if(isStatic)
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
            if(!IsPublicNode(node))
            {
                return;
            }

            base.VisitMethodDeclaration(node);

            int numParameters = node.ParameterList.Parameters.Count;
            Parameter[] parameters = new Parameter[numParameters];
            for(int idx = 0; idx < numParameters; idx++)
            {
                parameters[idx] = new Parameter(node.ParameterList.Parameters[idx], ParameterAttributes.In, Model);
            }

            var methodSymbol = Model.GetDeclaredSymbol(node);
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

            if (symbol.SpecialType != SpecialType.None)
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

        public void AddPropertyDeclaration(IPropertySymbol property, bool isInterfaceParent)
        {
            Logger.Log("defining property " + property.Name);
            Logger.Log("parent: " + property.ContainingType.Name);

            var propertySignature = new BlobBuilder();
            new BlobEncoder(propertySignature)
                .PropertySignature(true)
                .Parameters(
                    0,
                    returnType => EncodeReturnType(new Symbol(property.Type), returnType),
                    parameters => { }
                );

            var propertyDefinitonHandle = metadataBuilder.AddProperty(
                PropertyAttributes.None,
                metadataBuilder.GetOrAddString(property.Name),
                metadataBuilder.GetOrAddBlob(propertySignature));
            currentTypeDeclaration.AddProperty(property, propertyDefinitonHandle);

            if (property.SetMethod != null)
            {
                string setMethodName = "put_" + property.Name;
                var setMethod = AddMethodDefinition(
                    setMethodName,
                    new Parameter[] { new Parameter(property.Type, "value", ParameterAttributes.In) },
                    null,
                    false,
                    isInterfaceParent,
                    true);
                currentTypeDeclaration.AddMethod(property, setMethodName, setMethod);

                metadataBuilder.AddMethodSemantics(
                    propertyDefinitonHandle,
                    MethodSemanticsAttributes.Setter,
                    setMethod);
            }

            string getMethodName = "get_" + property.Name;
            var getMethod = AddMethodDefinition(
                getMethodName,
                new Parameter[0],
                new Symbol(property.Type),
                false,
                isInterfaceParent,
                true);
            currentTypeDeclaration.AddMethod(property, getMethodName, getMethod);

            metadataBuilder.AddMethodSemantics(
                propertyDefinitonHandle,
                MethodSemanticsAttributes.Getter,
                getMethod);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (!IsPublicNode(node))
            {
                return;
            }

            base.VisitPropertyDeclaration(node);

            var symbol = Model.GetDeclaredSymbol(node);

            var propertySignature = new BlobBuilder();
            new BlobEncoder(propertySignature)
                .PropertySignature(true)
                .Parameters(
                    0,
                    returnType => EncodeReturnType(new Symbol(symbol.Type), returnType),
                    parameters => { }
                );

            var propertyDefinitonHandle = metadataBuilder.AddProperty(
                PropertyAttributes.None,
                metadataBuilder.GetOrAddString(node.Identifier.ValueText),
                metadataBuilder.GetOrAddBlob(propertySignature));
            currentTypeDeclaration.AddProperty(symbol, propertyDefinitonHandle);

            var isGetPublic = true;
            var isSetPublic = true;

            /*
            TODO: support for private set with default interface support

            if (node.AccessorList != null)
            {
                foreach (var accessor in node.AccessorList.Accessors)
                {
                    if(accessor.Keyword.Kind() == SyntaxKind.GetKeyword)
                    {
                        isGetPublic = !IsPrivateNode(accessor);
                    }
                    else if(accessor.Keyword.Kind() == SyntaxKind.SetKeyword)
                    {
                        isSetPublic = !IsPrivateNode(accessor);
                    }
                }
            }
            */

            if (symbol.SetMethod != null && isSetPublic)
            {
                string setMethodName = "put_" + symbol.Name;
                var setMethod = AddMethodDefinition(
                    setMethodName,
                    new Parameter[] { new Parameter(symbol.Type, "value", ParameterAttributes.In ) },
                    null,
                    false,
                    node.Parent is InterfaceDeclarationSyntax,
                    true);
                currentTypeDeclaration.AddMethod(symbol, setMethodName, setMethod);

                metadataBuilder.AddMethodSemantics(
                    propertyDefinitonHandle,
                    MethodSemanticsAttributes.Setter,
                    setMethod);
            }

            if (!symbol.IsWriteOnly && isGetPublic)
            {
                string getMethodName = "get_" + symbol.Name;
                var getMethod = AddMethodDefinition(
                    getMethodName,
                    new Parameter[0],
                    new Symbol(symbol.Type),
                    false,
                    node.Parent is InterfaceDeclarationSyntax,
                    true);
                currentTypeDeclaration.AddMethod(symbol, getMethodName, getMethod);

                metadataBuilder.AddMethodSemantics(
                    propertyDefinitonHandle,
                    MethodSemanticsAttributes.Getter,
                    getMethod);
            }
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

        private void ProcessTypeDeclaration(BaseTypeDeclarationSyntax node, Action visitTypeDeclaration)
        {
            if(!IsPublicNode(node))
            {
                return;
            }

            var symbol = Model.GetDeclaredSymbol(node);
            currentTypeDeclaration = new TypeDeclaration(symbol);

            visitTypeDeclaration();

            TypeAttributes typeAttributes =
                TypeAttributes.Public |
                TypeAttributes.WindowsRuntime |
                TypeAttributes.AutoLayout |
                TypeAttributes.AnsiClass;

            if (IsSealedNode(node) || 
                (node is EnumDeclarationSyntax ||
                    node is StructDeclarationSyntax))
            {
                typeAttributes |= TypeAttributes.Sealed;
            }

            EntityHandle baseType = default;
            if(node is InterfaceDeclarationSyntax)
            {
                typeAttributes |= 
                    TypeAttributes.Interface |
                    TypeAttributes.Abstract;
            }
            else if(node is ClassDeclarationSyntax)
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
                        if(typeSymbol.TypeKind == TypeKind.Class)
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
            else if(node is StructDeclarationSyntax)
            {
                typeAttributes |= TypeAttributes.SequentialLayout;
                baseType = GetTypeReference("System", "ValueType", "mscorlib");
            }
            else if(node is EnumDeclarationSyntax)
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
                foreach (var implementedInterface in symbol.AllInterfaces.
                    OrderBy(implementedInterface => implementedInterface.ToString()))
                {
                    var interfaceImplHandle = metadataBuilder.AddInterfaceImplementation(
                        typeDefinitionHandle,
                        GetTypeReference(implementedInterface));
                    currentTypeDeclaration.AddInterfaceImpl(implementedInterface, interfaceImplHandle);
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
                if(!hasConstructor)
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
                if(classDeclaration.DefaultInterface == null && classSymbol.Interfaces.Length != 0)
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
                    foreach(var arrayConstant in constant.Values)
                    {
                        EncodeTypedConstant(arrayConstant, arrayEncoder.AddLiteral());
                    }
                    break;
                }
            }

        }

        private void EncodeFixedArguments(IList<TypedConstant> arguments, FixedArgumentsEncoder argumentsEncoder)
        {
            foreach(var argument in arguments)
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

            if(fieldSymbol.Type.SpecialType == SpecialType.System_Object)
            {
                encoder.Object();
            }
            else if(fieldSymbol.Type.SpecialType == SpecialType.System_Array)
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
                if(argument is string type)
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
            if(version == -1)
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

            if(factoryInterface != null)
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

        private void AddCustomAttributes(IList<AttributeData> attributes, EntityHandle parentHandle)
        {
            foreach (var attribute in attributes)
            {
                var attributeType = attribute.AttributeClass;

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
            if(node.Target != null && node.Target.Identifier.ValueText == "assembly")
            {
                return;
            }

            var parentSymbol = Model.GetDeclaredSymbol(node.Parent);
            nodesWithAttributes.Add(parentSymbol);
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

            int numParameters = node.ParameterList.Parameters.Count;
            Parameter[] parameters = new Parameter[numParameters];
            for (int idx = 0; idx < numParameters; idx++)
            {
                parameters[idx] = new Parameter(node.ParameterList.Parameters[idx], ParameterAttributes.In, Model);
            }

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
        
        public void AddEventDeclaration(IEventSymbol @event, bool isInterfaceParent)
        {
            Logger.Log("defining event " + @event.Name);

            var delegateSymbolType = @event.Type;
            EntityHandle typeReferenceHandle = GetTypeReference(delegateSymbolType);
            EntityHandle eventRegistrationTokenTypeHandle = GetTypeReference("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract");
            Symbol eventRegistrationToken = new Symbol(eventRegistrationTokenTypeHandle);

            var eventDefinitionHandle = metadataBuilder.AddEvent(
                EventAttributes.None,
                metadataBuilder.GetOrAddString(@event.Name),
                typeReferenceHandle);
            currentTypeDeclaration.AddEvent(@event, eventDefinitionHandle);

            string addMethodName = "add_" + @event.Name;
            var addMethod = AddMethodDefinition(
                addMethodName,
                new Parameter[] { new Parameter(delegateSymbolType, "handler", ParameterAttributes.In) },
                eventRegistrationToken,
                false,
                isInterfaceParent,
                true);
            currentTypeDeclaration.AddMethod(@event, addMethodName, addMethod);

            metadataBuilder.AddMethodSemantics(
                eventDefinitionHandle,
                MethodSemanticsAttributes.Adder,
                addMethod);

            string removeMethodName = "remove_" + @event.Name;
            var removeMethod = AddMethodDefinition(
                removeMethodName,
                new Parameter[] { new Parameter(eventRegistrationToken, "token", ParameterAttributes.In) },
                null,
                false,
                isInterfaceParent,
                true);
            currentTypeDeclaration.AddMethod(@event, removeMethodName, removeMethod);

            metadataBuilder.AddMethodSemantics(
                eventDefinitionHandle,
                MethodSemanticsAttributes.Remover,
                removeMethod);
        }

        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            if (!IsPublicNode(node))
            {
                return;
            }

            base.VisitEventFieldDeclaration(node);

            var delegateSymbolType = Model.GetTypeInfo(node.Declaration.Type).Type;
            EntityHandle typeReferenceHandle = GetTypeReference(delegateSymbolType);
            EntityHandle eventRegistrationTokenTypeHandle = GetTypeReference("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract");
            Symbol eventRegistrationToken = new Symbol(eventRegistrationTokenTypeHandle);

            foreach (var declaration in node.Declaration.Variables)
            {
                var eventSymbol = Model.GetDeclaredSymbol(declaration);
                var eventDefinitionHandle = metadataBuilder.AddEvent(
                    EventAttributes.None,
                    metadataBuilder.GetOrAddString(declaration.Identifier.ValueText),
                    typeReferenceHandle);
                currentTypeDeclaration.AddEvent(eventSymbol, eventDefinitionHandle);

                string addMethodName = "add_" + declaration.Identifier.ValueText;
                var addMethod = AddMethodDefinition(
                    addMethodName,
                    new Parameter[] { new Parameter(delegateSymbolType, "handler", ParameterAttributes.In) },
                    eventRegistrationToken,
                    false,
                    node.Parent is InterfaceDeclarationSyntax,
                    true);
                currentTypeDeclaration.AddMethod(eventSymbol, addMethodName, addMethod);

                metadataBuilder.AddMethodSemantics(
                    eventDefinitionHandle,
                    MethodSemanticsAttributes.Adder,
                    addMethod);

                string removeMethodName = "remove_" + declaration.Identifier.ValueText;
                var removeMethod = AddMethodDefinition(
                    removeMethodName,
                    new Parameter[] { new Parameter(eventRegistrationToken, "token", ParameterAttributes.In) },
                    null,
                    false,
                    node.Parent is InterfaceDeclarationSyntax,
                    true);
                currentTypeDeclaration.AddMethod(eventSymbol, removeMethodName, removeMethod);

                metadataBuilder.AddMethodSemantics(
                    eventDefinitionHandle,
                    MethodSemanticsAttributes.Remover,
                    removeMethod);
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
            int numParameters = node.ParameterList.Parameters.Count;
            Parameter[] parameters = new Parameter[numParameters];
            for (int idx = 0; idx < numParameters; idx++)
            {
                parameters[idx] = new Parameter(node.ParameterList.Parameters[idx], ParameterAttributes.In, Model);
            }

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
            hasDefaultConstructor |= (numParameters == 0);
        }

        void AddMethodDeclaration(IMethodSymbol method, bool isInterfaceParent)
        {
            Logger.Log("add method from symbol: " + method.Name);

            int numParameters = method.Parameters.Count();
            Parameter[] parameters = new Parameter[numParameters];
            for (int idx = 0; idx < numParameters; idx++)
            {
                parameters[idx] = new Parameter(method.Parameters[idx].Type, method.Parameters[idx].Name, ParameterAttributes.In);
            }

            string methodName = method.MethodKind == MethodKind.Constructor ? ".ctor" : method.Name;
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

            int numParameters = method.Parameters.Count();
            Parameter[] parameters = new Parameter[numParameters];
            for (int idx = 0; idx < numParameters; idx++)
            {
                parameters[idx] = new Parameter(method.Parameters[idx].Type, method.Parameters[idx].Name, ParameterAttributes.In);
            }

            string methodName = "Create" + classSymbol.Name;
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

            var typeDeclaration = new TypeDeclaration(type);
            currentTypeDeclaration = typeDeclaration;
            bool isInterface = type.TypeKind == TypeKind.Interface;

            foreach (var member in type.GetMembers())
            {
                if(member is IMethodSymbol method &&
                    (method.MethodKind == MethodKind.Ordinary || method.MethodKind == MethodKind.Constructor))
                {
                    AddMethodDeclaration(method, isInterface);
                }
                else if(member is IPropertySymbol property)
                {
                    AddPropertyDeclaration(property, isInterface);
                }
                else if(member is IEventSymbol @event)
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

            foreach (var implementedInterface in type.AllInterfaces.OrderBy(implementedInterface => implementedInterface.ToString()))
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
            ISymbol parentType,
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
                GetTypeReference(parentType),
                metadataBuilder.GetOrAddString(name),
                metadataBuilder.GetOrAddBlob(methodSignature)
            );
            return referenceHandle;
        }

        MemberReferenceHandle AddMethodReference(IMethodSymbol method)
        {
            Logger.Log("adding method reference: " + method.Name);

            bool isInterfaceParent = method.ContainingType.TypeKind == TypeKind.Interface;
            int numParameters = method.Parameters.Count();
            Parameter[] parameters = new Parameter[numParameters];
            for (int idx = 0; idx < numParameters; idx++)
            {
                parameters[idx] = new Parameter(method.Parameters[idx].Type, method.Parameters[idx].Name, ParameterAttributes.In);
            }

            string methodName = method.MethodKind == MethodKind.Constructor ? ".ctor" : method.Name;

            var referenceHandle = AddMethodReference(
                methodName,
                parameters,
                new Symbol(method.ReturnType),
                method.ContainingType,
                !isInterfaceParent && method.IsStatic);
            currentTypeDeclaration.AddMethodReference(method, referenceHandle);
            return referenceHandle;
        }

        public void AddPropertyReference(IPropertySymbol property)
        {
            Logger.Log("adding property reference: " + property.Name);

            if (property.SetMethod != null)
            {
                var setMethodReference = AddMethodReference(
                    "put_" + property.Name,
                    new Parameter[] { new Parameter(property.Type, "value", ParameterAttributes.In) },
                    null,
                    property.ContainingType,
                    false);
                currentTypeDeclaration.AddMethodReference(property, setMethodReference);
            }

            var getMethodReference = AddMethodReference(
                "get_" + property.Name,
                new Parameter[0],
                new Symbol(property.Type),
                property.ContainingType,
                false);
            currentTypeDeclaration.AddMethodReference(property, getMethodReference);
        }

        public void AddEventReference(IEventSymbol @event)
        {
            Logger.Log("adding event reference:  " + @event.Name);

            var delegateSymbolType = @event.Type;
            EntityHandle eventRegistrationTokenTypeHandle = GetTypeReference("Windows.Foundation", "EventRegistrationToken", "Windows.Foundation.FoundationContract");
            Symbol eventRegistrationToken = new Symbol(eventRegistrationTokenTypeHandle);

            var addMethodReference = AddMethodReference(
                "add_" + @event.Name,
                new Parameter[] { new Parameter(delegateSymbolType, "handler", ParameterAttributes.In) },
                eventRegistrationToken,
                @event.ContainingType,
                false);
            currentTypeDeclaration.AddMethodReference(delegateSymbolType, addMethodReference);

            var removeMethodReference = AddMethodReference(
                "remove_" + @event.Name,
                new Parameter[] { new Parameter(eventRegistrationToken, "token", ParameterAttributes.In) },
                null,
                @event.ContainingType,
                false);
            currentTypeDeclaration.AddMethodReference(delegateSymbolType, removeMethodReference);
        }

        void AddProjectedType(INamedTypeSymbol type)
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

            typeDefinitionMapping[type.ToString()] = currentTypeDeclaration;
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
                foreach(var interfaceMember in @interface.GetMembers())
                {
                    classMembersFromInterfaces.Add(classSymbol.FindImplementationForInterfaceMember(interfaceMember));
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

        void AddSynthesizedInterface(
            TypeDeclaration classDeclaration,
            SynthesizedInterfaceType interfaceType,
            HashSet<ISymbol> classMembersFromInterfaces)
        {
            var typeDeclaration = new TypeDeclaration(null);
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
                }
                else if((interfaceType == SynthesizedInterfaceType.Default && !classMember.Key.IsStatic && 
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

            if (hasTypes)
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

                if(interfaceType == SynthesizedInterfaceType.Default)
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
                    AddActivatableAttribute(classDeclaration.Handle, (uint) GetVersion(classSymbol, true), qualifiedInterfaceName);
                }
                else if(interfaceType == SynthesizedInterfaceType.Static)
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
            if(!versionAttribute.Any())
            {
                return setDefaultIfNotSet  ? Version.Parse(this.version).Major : - 1;
            }

            uint version = (uint) versionAttribute.First().ConstructorArguments[0].Value;
            return (int) version;
        }

        void AddType(INamedTypeSymbol type)
        {
            bool isProjectedType = type.GetAttributes().
                Any(attribute => attribute.AttributeClass.Name == "WindowsRuntimeTypeAttribute");
            if (isProjectedType)
            {
                AddProjectedType(type);
            }
            else if(MappedCSharpTypes.ContainsKey(type.ToString()))
            {
                var mappedType = MappedCSharpTypes[type.ToString()];
                AddProjectedType(Model.Compilation.GetTypeByMetadataName(QualifiedName(mappedType.@namespace, mappedType.name)));
            }
            else
            {
                AddExternalType(type);
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

                foreach (var implementedInterface in classSymbol.AllInterfaces)
                {
                    if (!typeDefinitionMapping.ContainsKey(implementedInterface.ToString()))
                    {
                        AddType(implementedInterface);
                    }

                    var interfaceTypeDeclaration = typeDefinitionMapping[implementedInterface.ToString()];
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
                if (typeDefinitionMapping[interfaceSymbol.ToString()].Handle != default && GetVersion(interfaceSymbol) == -1)
                {
                    AddDefaultVersionAttribute(typeDefinitionMapping[interfaceSymbol.ToString()].Handle);
                }
            }

            foreach (var node in nodesWithAttributes)
            {
                EntityHandle parentHandle = default;
                if(node is INamedTypeSymbol namedType)
                {
                    parentHandle = typeDefinitionMapping[namedType.ToString()].Handle;
                }
                else if (node is IMethodSymbol method)
                {
                    parentHandle = typeDefinitionMapping[method.ContainingType.ToString()].MethodDefinitions[method][0];
                }
                else if (node is IPropertySymbol property)
                {
                    parentHandle = typeDefinitionMapping[property.ContainingType.ToString()].PropertyDefinitions[property];
                }
                else if (node is IEventSymbol @event)
                {
                    parentHandle = typeDefinitionMapping[@event.ContainingType.ToString()].EventDefinitions[@event];
                }
                else
                {
                    Logger.Log("node not recognized " + node.Kind + " name: " + node.Name);
                }

                AddCustomAttributes(node.GetAttributes(), parentHandle);
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

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            namespaces.Push(node.Name.ToString());
            base.VisitNamespaceDeclaration(node);
            namespaces.Pop();
        }
    }
}
