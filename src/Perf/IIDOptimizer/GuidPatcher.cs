using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;


namespace GuidPatch
{
    class GuidPatcher
    {
        private readonly AssemblyDefinition assembly;
        private readonly AssemblyDefinition winRTRuntimeAssembly;
        private readonly TypeDefinition guidType;
        private readonly GenericInstanceType readOnlySpanOfByte;
        private readonly MethodReference readOnlySpanOfByteCtor;
        private readonly MethodReference guidCtor;
        private readonly TypeDefinition? guidAttributeType;
        private readonly MethodDefinition getTypeFromHandleMethod;
        private readonly TypeDefinition? guidGeneratorType;
        private readonly MethodDefinition? getIidMethod;
        private readonly MethodDefinition? createIidMethod;
        private readonly MethodDefinition? getHelperTypeMethod;

        private readonly Dictionary<TypeReference, MethodDefinition> ClosedTypeGuidDataMapping = new Dictionary<TypeReference, MethodDefinition>();
        private readonly TypeDefinition guidImplementationDetailsType;
        private readonly TypeDefinition guidDataBlockType;
        private readonly SignatureGenerator signatureGenerator;
        private readonly Dictionary<string, MethodReference> methodCache;

        public GuidPatcher(AssemblyDefinition winRTRuntime, AssemblyDefinition targetAssembly)
        {
            assembly = targetAssembly;

            winRTRuntimeAssembly = winRTRuntime;

            guidImplementationDetailsType = new TypeDefinition(null, "<GuidPatcherImplementationDetails>", TypeAttributes.AutoClass | TypeAttributes.Sealed, assembly.MainModule.TypeSystem.Object);

            assembly.MainModule.Types.Add(guidImplementationDetailsType);

            guidDataBlockType = CecilExtensions.GetOrCreateDataBlockType(guidImplementationDetailsType, Unsafe.SizeOf<Guid>());

            var systemType = new TypeReference("System", "Type", assembly.MainModule, assembly.MainModule.TypeSystem.CoreLibrary).Resolve();

            guidType = new TypeReference("System", "Guid", assembly.MainModule, assembly.MainModule.TypeSystem.CoreLibrary).Resolve();

            readOnlySpanOfByte = new GenericInstanceType(new TypeReference("System", "ReadOnlySpan`1", assembly.MainModule, assembly.MainModule.TypeSystem.CoreLibrary, true))
            {
                GenericArguments =
                {
                    assembly.MainModule.TypeSystem.Byte
                }
            };

            readOnlySpanOfByteCtor = assembly.MainModule.ImportReference(new MethodReference(".ctor", assembly.MainModule.TypeSystem.Void, readOnlySpanOfByte)
            {
                Parameters =
                {
                    new ParameterDefinition(new PointerType(assembly.MainModule.TypeSystem.Void)),
                    new ParameterDefinition(assembly.MainModule.TypeSystem.Int32),
                },
                HasThis = true
            });

            guidCtor = assembly.MainModule.ImportReference(guidType.Methods.First(m => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Resolve() == readOnlySpanOfByte.Resolve()));

            getTypeFromHandleMethod = systemType.Methods.First(m => string.CompareOrdinal(m.Name, "GetTypeFromHandle") == 0);

            guidGeneratorType = null;

            TypeDefinition? typeExtensionsType = null;

            // Use the type definition if we are patching WinRT.Runtime, otherwise lookup the types as references 
            if (string.CompareOrdinal(assembly.Name.Name, "WinRT.Runtime") == 0)
            {
                guidGeneratorType = winRTRuntimeAssembly.MainModule.Types.Where(typeDef => string.CompareOrdinal(typeDef.Name, "GuidGenerator") == 0).First();
                typeExtensionsType = winRTRuntimeAssembly.MainModule.Types.Where(typeDef => string.CompareOrdinal(typeDef.Name, "TypeExtensions") == 0).First();
            }

            foreach (var asm in assembly.MainModule.AssemblyReferences)
            {
                if (string.CompareOrdinal(asm.Name, "WinRT.Runtime") == 0)
                {
                    guidGeneratorType =
                        new TypeReference("WinRT", "GuidGenerator", assembly.MainModule, asm).Resolve();
                    typeExtensionsType = new TypeReference("WinRT", "TypeExtensions", assembly.MainModule, asm).Resolve();
                }
                else if (string.CompareOrdinal(asm.Name, "System.Runtime.InteropServices") == 0)
                {
                    guidAttributeType = new TypeReference("System.Runtime.InteropServices", "GuidAttribute", assembly.MainModule, asm).Resolve();
                }
            }

            if (guidGeneratorType is not null && typeExtensionsType is not null)
            {
                getIidMethod = guidGeneratorType.Methods.First(m => String.CompareOrdinal(m.Name, "GetIID") == 0);
                createIidMethod = guidGeneratorType.Methods.First(m => String.CompareOrdinal(m.Name, "CreateIID") == 0);
                getHelperTypeMethod = typeExtensionsType.Methods.First(m => String.CompareOrdinal(m.Name, "GetHelperType") == 0);
            }

            signatureGenerator = new SignatureGenerator(assembly, guidAttributeType!, winRTRuntimeAssembly);
            methodCache = new Dictionary<string, MethodReference>();
        }

        public int ProcessAssembly()
        {
            if (guidGeneratorType is null || guidAttributeType is null)
            {
                return 0;
            }

            int numPatches = 0;
            var methods = from module in assembly.Modules
                          from type in module.Types
                          from method in type.Methods
                          where method.HasBody
                          select method;

            foreach (var method in methods)
            {
                numPatches += ProcessMethodBody(method.Body, getTypeFromHandleMethod, getIidMethod!, createIidMethod!);
            }

            return numPatches;
        }

        public void SaveAssembly(string targetDirectory)
        {
            var writerParameters = new WriterParameters
            {
                WriteSymbols = true
            };

            assembly.Write($"{targetDirectory}{Path.DirectorySeparatorChar}{assembly.Name.Name}.dll", writerParameters);
        }

        enum State
        {
            Start,
            Ldtoken,
            GetTypeFromHandle,
            GetHelperTypeOptional
        }

        private bool PatchNonGenericTypeIID(MethodBody body, int startILIndex, TypeReference type, int numberOfInstructionsToOverwrite)
        {
            if (numberOfInstructionsToOverwrite < 2)
            {
                return false;
            }

            if (!ClosedTypeGuidDataMapping.TryGetValue(type, out var guidDataMethod))
            {
                Guid? guidValue = type.ReadGuidFromAttribute(guidAttributeType!, winRTRuntimeAssembly);
                if (guidValue == null)
                {
                    return false;
                }

                guidDataMethod = CecilExtensions.CreateIIDDataGetter(type, guidValue.Value, guidDataBlockType, guidImplementationDetailsType, readOnlySpanOfByte, readOnlySpanOfByteCtor);

                guidImplementationDetailsType.Methods.Add(guidDataMethod);
                ClosedTypeGuidDataMapping[type] = guidDataMethod;
            }

            ReplaceWithCallToGuidDataGetter(body, startILIndex, numberOfInstructionsToOverwrite, guidDataMethod);

            return true;
        }

        private bool PatchGenericTypeIID(MethodBody body, int startILIndex, TypeReference type, int numberOfInstructionsToOverwrite)
        {
            SignaturePart rootSignaturePart = signatureGenerator.GetSignatureParts(type);
            var methodName = $"<IIDData>{type.FullName}";

            if (!methodCache.TryGetValue(methodName, out var guidDataMethodReference))
            {
                var guidDataMethod = new MethodDefinition(methodName, MethodAttributes.Assembly | MethodAttributes.Static, readOnlySpanOfByte);

                guidImplementationDetailsType.Methods.Add(guidDataMethod);

                var emitter = new SignatureEmitter(type, guidDataMethod);
                VisitSignature(rootSignaturePart, emitter);

                emitter.EmitGuidGetter(guidDataBlockType, guidImplementationDetailsType, readOnlySpanOfByte, readOnlySpanOfByteCtor, guidGeneratorType!);

                guidDataMethodReference = guidDataMethod;
                if (guidDataMethodReference.HasGenericParameters)
                {
                    var genericGuidDataMethodReference = new GenericInstanceMethod(guidDataMethodReference);
                    foreach (var param in guidDataMethodReference.GenericParameters)
                    {
                        genericGuidDataMethodReference.GenericArguments.Add(emitter.GenericParameterMapping[param]);
                    }
                    guidDataMethodReference = genericGuidDataMethodReference;
                }

                methodCache[methodName] = guidDataMethodReference;
            }

            ReplaceWithCallToGuidDataGetter(body, startILIndex, numberOfInstructionsToOverwrite, guidDataMethodReference);
            return true;
        }

        private void ReplaceWithCallToGuidDataGetter(MethodBody body, int startILIndex, int numberOfInstructionsToOverwrite, MethodReference guidDataMethod)
        {
            var il = body.GetILProcessor();
            il.Replace(startILIndex, Instruction.Create(OpCodes.Call, guidDataMethod));
            il.Replace(startILIndex + 1, Instruction.Create(OpCodes.Newobj, guidCtor));
            for (int i = 2; i < numberOfInstructionsToOverwrite; i++)
            {
                il.Replace(startILIndex + i, Instruction.Create(OpCodes.Nop));
            }
        }

        private void VisitSignature(SignaturePart rootSignaturePart, SignatureEmitter emitter)
        {
            switch (rootSignaturePart)
            {
                case BasicSignaturePart basic:
                    {
                        emitter.PushString(basic.Type switch
                        {
                            SignatureType.@string => "string",
                            SignatureType.iinspectable => "cinterface(IInspectable)",
                            _ => basic.Type.ToString()
                        });
                    }
                    break;
                case SignatureWithChildren group:
                    {
                        emitter.PushString($"{group.GroupingName}(");
                        emitter.PushString(group.ThisEntitySignature);
                        foreach (var item in group.ChildrenSignatures)
                        {
                            emitter.PushString(";");
                            VisitSignature(item, emitter);
                        }
                        emitter.PushString(")");
                    }
                    break;
                case GuidSignature guid:
                    {
                        emitter.PushString(guid.IID.ToString("B"));
                    }
                    break;
                case NonGenericDelegateSignature del:
                    {
                        emitter.PushString($"delegate({del.DelegateIID:B}");
                    }
                    break;
                case UninstantiatedGeneric gen:
                    {
                        emitter.PushGenericParameter(gen.OriginalGenericParameter);
                    }
                    break;
                case CustomSignatureMethod custom:
                    {
                        emitter.PushCustomSignature(custom.Method);
                    }
                    break;
                default:
                    break;
            }
        }

        private int ProcessMethodBody(MethodBody body, MethodDefinition getTypeFromHandleMethod, MethodDefinition getIidMethod, MethodDefinition createIidMethod)
        {
            int numberOfReplacements = 0;
            TypeReference? type = null;
            State state = State.Start;
            int startIlIndex = -1;
            int numberOfInstructionsToOverwrite = 3;
            for (int i = 0; i < body.Instructions.Count; i++)
            {
                var instruction = body.Instructions[i];
                switch (state)
                {
                    case State.Start:
                        if (instruction.OpCode.Code != Code.Ldtoken)
                        {
                            continue;
                        }
                        // Do safe cast in case we are given a FieldReference and
                        // skip if so since we dont think we'll get a RuntimeTypeHandle for it 
                        var typeMaybe = instruction.Operand as TypeReference;
                        if (typeMaybe != null && !typeMaybe.IsGenericParameter)
                        {
                            state = State.Ldtoken;
                            type = typeMaybe;
                            startIlIndex = i;
                            numberOfInstructionsToOverwrite = 3;
                        }
                        break;
                    case State.Ldtoken:
                        {
                            if (instruction.OpCode.Code != Code.Call)
                            {
                                state = State.Start;
                                type = null;
                                continue;
                            }
                            var method = ((MethodReference)instruction.Operand).Resolve();
                            if (method == getTypeFromHandleMethod)
                            {
                                state = State.GetTypeFromHandle;
                            }
                        }
                        break;
                    case State.GetTypeFromHandle:
                        {
                            if (instruction.OpCode.Code != Code.Call)
                            {
                                state = State.Start;
                                type = null;
                                continue;
                            }
                            var method = ((MethodReference)instruction.Operand).Resolve();
                            if (method == getHelperTypeMethod)
                            {
                                numberOfInstructionsToOverwrite++;
                                state = State.GetHelperTypeOptional;
                                continue;
                            }
                            else
                            {
                                goto case State.GetHelperTypeOptional;
                            }
                        }
                    case State.GetHelperTypeOptional:
                        {
                            if (instruction.OpCode.Code != Code.Call)
                            {
                                state = State.Start;
                                type = null;
                                continue;
                            }
                            var method = ((MethodReference)instruction.Operand).Resolve();
                            if (method == getIidMethod || method == createIidMethod)
                            {
                                try
                                {
                                    bool didPatch = false;
                                    if (type!.IsGenericInstance)
                                    {
                                        didPatch = PatchGenericTypeIID(body, startIlIndex, type, numberOfInstructionsToOverwrite);
                                    }
                                    else
                                    {
                                        didPatch = PatchNonGenericTypeIID(body, startIlIndex, type, numberOfInstructionsToOverwrite);
                                    }

                                    if (didPatch)
                                    {
                                        numberOfReplacements++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Exception thrown during patching {body.Method.FullName}: {ex}");
                                }
                            }

                            // Reset after patching or if we realized this is not the signature to patch.
                            state = State.Start;
                            type = null;
                            startIlIndex = -1;
                        }
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
            return numberOfReplacements;
        }
    }
}