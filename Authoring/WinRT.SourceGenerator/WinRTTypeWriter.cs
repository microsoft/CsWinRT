using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

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
            :this(new Symbol(type), name, attributes)
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

        public Dictionary<ISymbol, List<MethodDefinitionHandle>> MethodDefinitions;
        public Dictionary<ISymbol, List<EntityHandle>> MethodReferences;
        public Dictionary<ISymbol, FieldDefinitionHandle> FieldDefinitions;
        public Dictionary<ISymbol, PropertyDefinitionHandle> PropertyDefinitions;
        public Dictionary<ISymbol, EventDefinitionHandle> EventDefinitions;

        public TypeDeclaration(ISymbol node)
        {
            Node = node;
            MethodDefinitions = new Dictionary<ISymbol, List<MethodDefinitionHandle>>();
            MethodReferences = new Dictionary<ISymbol, List<EntityHandle>>();
            FieldDefinitions = new Dictionary<ISymbol, FieldDefinitionHandle>();
            PropertyDefinitions = new Dictionary<ISymbol, PropertyDefinitionHandle>();
            EventDefinitions = new Dictionary<ISymbol, EventDefinitionHandle>();
        }

        public override string ToString()
        {
            return Node.ToString();
        }

        public void AddMethod(ISymbol node, MethodDefinitionHandle handle)
        {
            if(!MethodDefinitions.ContainsKey(node))
            {
                MethodDefinitions[node] = new List<MethodDefinitionHandle>();
                MethodReferences[node] = new List<EntityHandle>();
            }

            MethodDefinitions[node].Add(handle);
            MethodReferences[node].Add(handle);
        }

        public void AddMethodReference(ISymbol node, MemberReferenceHandle handle)
        {
            if (!MethodReferences.ContainsKey(node))
            {
                MethodReferences[node] = new List<EntityHandle>();
            }

            MethodReferences[node].Add(handle);
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
    }

    class WinRTTypeWriter : CSharpSyntaxWalker
    {
        public SemanticModel Model;

        private readonly string assembly;
        private readonly string version;

        private readonly Stack<string> namespaces = new Stack<string>();
        private readonly Dictionary<string, TypeReferenceHandle> typeReferenceMapping;
        private readonly Dictionary<string, EntityHandle> assemblyReferenceMapping;
        private readonly List<ISymbol> nodesWithAttributes;
        private readonly MetadataBuilder metadataBuilder;
        private bool hasConstructor;

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
                metadataBuilder.GetOrAddString(assembly),
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
                    AssemblyFlags.ContentTypeMask,
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

        private EntityHandle GetTypeReference(ISymbol symbol)
        {
            string @namespace = symbol.ContainingNamespace.ToString();
            string name = symbol.Name;

            string assembly;
            var winrtTypeAttribute = symbol.GetAttributes().
                Where(attribute => attribute.AttributeClass.Name == "WindowsRuntimeTypeAttribute");
            if (winrtTypeAttribute.Any())
            {
                assembly = (string) winrtTypeAttribute.First().ConstructorArguments[0].Value;
            }
            else
            {
                assembly = symbol.ContainingAssembly.Name;
            }

            return GetTypeReference(@namespace, name, assembly);
        }

        private void EncodeSymbol(Symbol symbol, SignatureTypeEncoder typeEncoder)
        {
            EntityHandle typeReference = symbol.IsHandle() ? symbol.Handle : GetTypeReference(symbol.Type);
            typeEncoder.Type(typeReference, false);
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
                    parameters.Length,
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
                node.Identifier.ValueText,
                parameters,
                new Symbol(methodSymbol.ReturnType),
                !IsStaticNode(node),
                node.Parent is InterfaceDeclarationSyntax);
            currentTypeDeclaration.AddMethod(methodSymbol, methodDefinitionHandle);
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
                var setMethod = AddMethodDefinition(
                    "put_" + property.Name,
                    new Parameter[] { new Parameter(property.Type, "value", ParameterAttributes.In) },
                    null,
                    false,
                    isInterfaceParent,
                    true);
                currentTypeDeclaration.AddMethod(property, setMethod);

                metadataBuilder.AddMethodSemantics(
                    propertyDefinitonHandle,
                    MethodSemanticsAttributes.Setter,
                    setMethod);
            }

            var getMethod = AddMethodDefinition(
                "get_" + property.Name,
                new Parameter[0],
                new Symbol(property.Type),
                false,
                isInterfaceParent,
                true);
            currentTypeDeclaration.AddMethod(property, getMethod);

            metadataBuilder.AddMethodSemantics(
                propertyDefinitonHandle,
                MethodSemanticsAttributes.Getter,
                getMethod);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
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

            if (symbol.SetMethod != null)
            {
                var setMethod = AddMethodDefinition(
                    "put_" + symbol.Name,
                    new Parameter[] { new Parameter(symbol.Type, "value", ParameterAttributes.In ) },
                    null,
                    false,
                    node.Parent is InterfaceDeclarationSyntax,
                    true);
                currentTypeDeclaration.AddMethod(symbol, setMethod);

                metadataBuilder.AddMethodSemantics(
                    propertyDefinitonHandle,
                    MethodSemanticsAttributes.Setter,
                    setMethod);
            }

            var getMethod = AddMethodDefinition(
                "get_" + symbol.Name,
                new Parameter[0],
                new Symbol(symbol.Type),
                false,
                node.Parent is InterfaceDeclarationSyntax,
                true);
            currentTypeDeclaration.AddMethod(symbol, getMethod);

            metadataBuilder.AddMethodSemantics(
                propertyDefinitonHandle,
                MethodSemanticsAttributes.Getter,
                getMethod);
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

                // extens
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
                else
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
                    metadataBuilder.AddInterfaceImplementation(
                        typeDefinitionHandle,
                        GetTypeReference(implementedInterface));
                }
            }

            typeDefinitionMapping[QualifiedName(node.Identifier.ValueText)] = currentTypeDeclaration;
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            ProcessTypeDeclaration(node, () => base.VisitInterfaceDeclaration(node));
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            void processClassDeclaration()
            {
                hasConstructor = false;
                base.VisitClassDeclaration(node);

                // implicit constructor if none defined
                if(!hasConstructor)
                {
                    var methodDefinitionHandle = AddMethodDefinition(
                        ".ctor",
                        new Parameter[0],
                        null,
                        false,
                        false,
                        true,
                        true,
                        true);
                    var symbol = Model.GetDeclaredSymbol(node);
                    currentTypeDeclaration.AddMethod(symbol, methodDefinitionHandle);
                }
            }

            ProcessTypeDeclaration(node, processClassDeclaration);

            if (IsPublicNode(node))
            {
                AddSynthesizedInterfaces(currentTypeDeclaration);
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

        private void EncodeNamedArguments(INamedTypeSymbol attributeType, IList<KeyValuePair<string, TypedConstant>> namedArguments, CustomAttributeNamedArgumentsEncoder argumentsEncoder)
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
                argumentsEncoder.AddArgument().Scalar().Constant(argument);
            }
        }

        public void AddDefaultVersionAttribute(EntityHandle parentHandle, int version = -1)
        {
            if(version == -1)
            {
                version = Version.Parse(this.version).Major;
            }

            List<object> arguments = new List<object>
            {
                version
            };

            AddCustomAttributes("Windows.Foundation.Metadata.VersionAttribute", arguments, parentHandle);
        }

        private void AddCustomAttributes(string attributeTypeName, IList<object> primitiveValues, EntityHandle parentHandle)
        {
            var attributeType = Model.Compilation.GetTypeByMetadataName(attributeTypeName);
            Logger.Log("attribute type found " + attributeType);
            if (!typeDefinitionMapping.ContainsKey(attributeTypeName))
            {
                Logger.Log("adding attribute type");
                AddType(attributeType);
            }

            Logger.Log("# constructor found: " + attributeType.Constructors.Length);
            var matchingConstructor = attributeType.Constructors.Where(constructor => constructor.Parameters.Length == primitiveValues.Count);

            Logger.Log("# matching constructor found: " + matchingConstructor.Count());
            Logger.Log("matching constructor found: " + matchingConstructor.First());

            var constructorReference = typeDefinitionMapping[attributeTypeName].MethodReferences[matchingConstructor.First()];
            Logger.Log("found constructor handle: " + constructorReference.Count);

            var attributeSignature = new BlobBuilder();
            new BlobEncoder(attributeSignature)
                .CustomAttributeSignature(
                    fixedArguments => EncodeFixedArguments(primitiveValues, fixedArguments),
                    namedArguments => { }
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
                base.VisitEnumDeclaration(node);

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
            }

            ProcessTypeDeclaration(node, processEnumDeclaration);

            // TODO:System.flags attribute
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

            var addMethod = AddMethodDefinition(
                "add_" + @event.Name,
                new Parameter[] { new Parameter(delegateSymbolType, "handler", ParameterAttributes.In) },
                eventRegistrationToken,
                false,
                isInterfaceParent,
                true);
            currentTypeDeclaration.AddMethod(@event, addMethod);

            metadataBuilder.AddMethodSemantics(
                eventDefinitionHandle,
                MethodSemanticsAttributes.Adder,
                addMethod);

            var removeMethod = AddMethodDefinition(
                "remove_" + @event.Name,
                new Parameter[] { new Parameter(eventRegistrationToken, "token", ParameterAttributes.In) },
                null,
                false,
                isInterfaceParent,
                true);
            currentTypeDeclaration.AddMethod(@event, removeMethod);

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

                var addMethod = AddMethodDefinition(
                    "add_" + declaration.Identifier.ValueText,
                    new Parameter[] { new Parameter(delegateSymbolType, "handler", ParameterAttributes.In) },
                    eventRegistrationToken,
                    false,
                    node.Parent is InterfaceDeclarationSyntax,
                    true);
                currentTypeDeclaration.AddMethod(eventSymbol, addMethod);

                metadataBuilder.AddMethodSemantics(
                    eventDefinitionHandle,
                    MethodSemanticsAttributes.Adder,
                    addMethod);

                var removeMethod = AddMethodDefinition(
                    "remove_" + declaration.Identifier.ValueText,
                    new Parameter[] { new Parameter(eventRegistrationToken, "token", ParameterAttributes.In) },
                    null,
                    false,
                    node.Parent is InterfaceDeclarationSyntax,
                    true);
                currentTypeDeclaration.AddMethod(eventSymbol, removeMethod);

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
            currentTypeDeclaration.AddMethod(symbol, methodDefinitionHandle);
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
            currentTypeDeclaration.AddMethod(method, methodDefinitionHandle);
        }

        void AddFactoryMethod(INamedTypeSymbol classSymbol, IMethodSymbol method)
        {
            Logger.Log("ading factory method: " + method.Name);

            int numParameters = method.Parameters.Count();
            Parameter[] parameters = new Parameter[numParameters];
            for (int idx = 0; idx < numParameters; idx++)
            {
                parameters[idx] = new Parameter(method.Parameters[idx].Type, method.Parameters[idx].Name, ParameterAttributes.In);
            }

            var methodDefinitionHandle = AddMethodDefinition(
                "Create" + classSymbol.Name,
                parameters,
                new Symbol(classSymbol),
                false,
                true,
                false,
                true,
                true);
            currentTypeDeclaration.AddMethod(method, methodDefinitionHandle);
        }

        void AddExternalType(INamedTypeSymbol type)
        {
            // TODO: check if custom projected interface
            // TODO: block or warn type names with namespaces not meeting WinRT requirements.

            currentTypeDeclaration = new TypeDeclaration(type);
            bool isInterfaceParent = type.TypeKind == TypeKind.Interface;

            foreach (var member in type.GetMembers())
            {
                if(member is IMethodSymbol method &&
                    (method.MethodKind == MethodKind.Ordinary || method.MethodKind == MethodKind.Constructor))
                {
                    AddMethodDeclaration(method, isInterfaceParent);
                }
                else if(member is IPropertySymbol property)
                {
                    AddPropertyDeclaration(property, isInterfaceParent);
                }
                else if(member is IEventSymbol @event)
                {
                    AddEventDeclaration(@event, isInterfaceParent);
                }
                else
                {
                    Logger.Log("member not recognized:  " + member.Kind + " name: " + member.Name);
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
            currentTypeDeclaration.Handle = typeDefinitionHandle;

            foreach (var implementedInterface in type.AllInterfaces.OrderBy(implementedInterface => implementedInterface.ToString()))
            {
                metadataBuilder.AddInterfaceImplementation(
                    typeDefinitionHandle,
                    GetTypeReference(implementedInterface));
            }

            typeDefinitionMapping[type.ToString()] = currentTypeDeclaration;
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
                    parameters.Length,
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
        }

        void AddSynthesizedInterface(
            TypeDeclaration classDeclaration,
            SynthesizedInterfaceType interfaceType,
            HashSet<ISymbol> classMembersFromInterfaces)
        {
            currentTypeDeclaration = new TypeDeclaration(null);

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
                else if((interfaceType == SynthesizedInterfaceType.Default && !classMembersFromInterfaces.Contains(classMember.Key)) || 
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
                currentTypeDeclaration.Handle = typeDefinitionHandle;

                string qualifiedInterfaceName = QualifiedName(classDeclaration.Node.ContainingNamespace.ToString(), interfaceName);
                typeDefinitionMapping[qualifiedInterfaceName] = currentTypeDeclaration;

                if(interfaceType == SynthesizedInterfaceType.Default)
                {
                    classDeclaration.DefaultInterface = qualifiedInterfaceName;
                    metadataBuilder.AddInterfaceImplementation(
                        classDeclaration.Handle,
                        GetTypeReference(classDeclaration.Node.ContainingNamespace.ToString(), interfaceName, assembly));
                }

                AddDefaultVersionAttribute(typeDefinitionHandle, GetVersion(classSymbol, true));
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
            // TODO: Windows startwith is hack for missing attribute on attributes.
            if (isProjectedType || type.ToString().StartsWith("Windows."))
            {
                AddProjectedType(type);
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

                    foreach (var interfaceMember in typeDefinitionMapping[implementedInterface.ToString()].MethodReferences)
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
                        }
                    }

                    if (GetVersion(implementedInterface) == -1)
                    {
                        AddDefaultVersionAttribute(typeDefinitionMapping[implementedInterface.ToString()].Handle);

                    }
                }

                if (classTypeDeclaration.DefaultInterface != null)
                {
                    foreach (var interfaceMember in typeDefinitionMapping[classTypeDeclaration.DefaultInterface].MethodReferences)
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
                        }
                    }
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
