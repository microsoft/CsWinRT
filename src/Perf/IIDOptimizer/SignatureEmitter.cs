using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace GuidPatch
{
    sealed class SignatureEmitter
    {
        private static Guid WinRTPinterfaceNamespace = new("d57af411-737b-c042-abae-878b1e16adee");
        private StringBuilder? currentStringBuilder;
        private readonly List<SignatureStep> signatureSteps = new();
        private readonly Dictionary<GenericParameter, GenericParameter> originalGenericParameterToGetterParameterMapping = new();
        private readonly Dictionary<GenericParameter, GenericParameter> getterParameterToOriginalGenericParameterMapping = new();
        private readonly TypeReference describedType;
        private readonly MethodDefinition guidDataGetterMethod;

        // OptimizerDir is the path our current process should use to write logs and patched DLLs to
        public string OptimizerDir
        {
            get { return "obj\\IIDOptimizer"; }
        }
        public IReadOnlyDictionary<GenericParameter, GenericParameter> GenericParameterMapping => getterParameterToOriginalGenericParameterMapping;

        record SignatureStep;

        sealed record StringStep(string StaticSignatureString) : SignatureStep;

        sealed record RuntimeGenericSignatureStep(GenericParameter OriginalTypeParameter, GenericParameter NewTypeParameter) : SignatureStep;

        sealed record RuntimeCustomSignatureStep(MethodReference method) : SignatureStep;

        public SignatureEmitter(TypeReference describedType, MethodDefinition guidDataGetterMethod)
        {
            this.describedType = describedType;
            this.guidDataGetterMethod = guidDataGetterMethod;
        }

        public void PushString(string str)
        {
            if (currentStringBuilder is null)
            {
                currentStringBuilder = new StringBuilder();
            }
            currentStringBuilder.Append(str);
        }

        public void PushGenericParameter(GenericParameter typeParameter)
        {
            if (!originalGenericParameterToGetterParameterMapping.TryGetValue(typeParameter, out var localTypeParameter))
            {
                originalGenericParameterToGetterParameterMapping[typeParameter] = localTypeParameter = new GenericParameter(guidDataGetterMethod);
                getterParameterToOriginalGenericParameterMapping[localTypeParameter] = typeParameter;
                guidDataGetterMethod.GenericParameters.Add(localTypeParameter);
            }
            if (currentStringBuilder is not null)
            {
                signatureSteps.Add(new StringStep(currentStringBuilder.ToString()));
                currentStringBuilder = null;
            }
            signatureSteps.Add(new RuntimeGenericSignatureStep(typeParameter, localTypeParameter));
        }

        public void PushCustomSignature(MethodReference customSignatureMethod)
        {
            if (currentStringBuilder is not null)
            {
                signatureSteps.Add(new StringStep(currentStringBuilder.ToString()));
                currentStringBuilder = null;
            }
            signatureSteps.Add(new RuntimeCustomSignatureStep(customSignatureMethod));
        }

        public void EmitGuidGetter(
            TypeDefinition guidDataBlockType,
            TypeDefinition implementationDetailsType,
            TypeReference readOnlySpanofByte,
            MethodReference readOnlySpanOfByteCtor,
            TypeDefinition guidGeneratorType)
        {
            if (currentStringBuilder is not null)
            {
                signatureSteps.Add(new StringStep(currentStringBuilder.ToString()));
                currentStringBuilder = null;
            }

            // TODO: Emit IID Generation.
            if (signatureSteps.Count == 1 && signatureSteps[0] is StringStep str)
            {
                GenerateGuidFromSimpleSignature(str, guidDataBlockType, implementationDetailsType, readOnlySpanofByte, readOnlySpanOfByteCtor);
            }
            else
            {
                GenerateGuidFactoryFromComplexSignature(implementationDetailsType, readOnlySpanofByte, readOnlySpanOfByteCtor, guidGeneratorType);
            }
        }

        private void GenerateGuidFromSimpleSignature(StringStep stringStep, TypeDefinition guidDataBlockType, TypeDefinition implementationDetailsType, TypeReference readOnlySpanOfByte, MethodReference readOnlySpanOfByteCtor)
        {
            var maxBytes = Encoding.UTF8.GetMaxByteCount(stringStep.StaticSignatureString.Length);

            Span<byte> data = new byte[Unsafe.SizeOf<Guid>() + maxBytes];
            WinRTPinterfaceNamespace.TryWriteBytes(data);
            var numBytes = Encoding.UTF8.GetBytes(stringStep.StaticSignatureString, data[Unsafe.SizeOf<Guid>()..]);
            data = data[..(Unsafe.SizeOf<Guid>() + numBytes)];

            Debug.Assert(SHA1.Create().HashSize == 160);

            Span<byte> hash = stackalloc byte[160];
            SHA1.HashData(data, hash);

            if (BitConverter.IsLittleEndian)
            {
                // swap bytes of int a
                byte t = hash[0];
                hash[0] = hash[3];
                hash[3] = t;
                t = hash[1];
                hash[1] = hash[2];
                hash[2] = t;
                // swap bytes of short b
                t = hash[4];
                hash[4] = hash[5];
                hash[5] = t;
                // swap bytes of short c and encode rfc time/version field
                t = hash[6];
                hash[6] = hash[7];
                hash[7] = (byte)((t & 0x0f) | (5 << 4));
                // encode rfc clock/reserved field
                hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
            }

            var iid = new Guid(hash[0..16]);

            CecilExtensions.WriteIIDDataGetterBody(guidDataGetterMethod, describedType, iid, guidDataBlockType, implementationDetailsType, readOnlySpanOfByteCtor);
        }

        private void GenerateGuidFactoryFromComplexSignature(TypeDefinition implementationDetailsType, TypeReference readOnlySpanOfByte, MethodReference readOnlySpanOfBytePtrCtor, TypeDefinition guidGeneratorType)
        {
            var module = implementationDetailsType.Module;

            var readOnlySpanOfByteArrayCtor = module.ImportReference(
                new MethodReference(".ctor", module.TypeSystem.Void, readOnlySpanOfByte)
                {
                    Parameters = { new ParameterDefinition(new ArrayType(readOnlySpanOfByte.Resolve().GenericParameters[0])) },
                    HasThis = true
                },
                readOnlySpanOfByte);

            // Create generic class with static array field to cache result.
            var cacheType = new TypeDefinition(null, $"<SignatureCache>{describedType.FullName}", TypeAttributes.NestedPrivate | TypeAttributes.Abstract | TypeAttributes.Sealed, module.ImportReference(module.TypeSystem.Object));

            Dictionary<GenericParameter, GenericParameter> getterMethodGensToCacheTypeGens = new();

            for (int i = 0; i < guidDataGetterMethod.GenericParameters.Count; i++)
            {
                cacheType.GenericParameters.Add(new GenericParameter(cacheType));
                getterMethodGensToCacheTypeGens[guidDataGetterMethod.GenericParameters[i]] = cacheType.GenericParameters[i];
            }

            TypeReference instantiatedCacheType = cacheType;
            TypeReference selfInstantiatedCacheType = cacheType;

            if (cacheType.GenericParameters.Count != 0)
            {
                var instantiatedCacheTypeTemp = new GenericInstanceType(cacheType);
                foreach (var arg in guidDataGetterMethod.GenericParameters)
                {
                    instantiatedCacheTypeTemp.GenericArguments.Add(arg);
                }
                instantiatedCacheType = instantiatedCacheTypeTemp;

                var selfInstantiatedCacheTypeTemp = new GenericInstanceType(cacheType);
                foreach (var param in cacheType.GenericParameters)
                {
                    selfInstantiatedCacheTypeTemp.GenericArguments.Add(param);
                }
                selfInstantiatedCacheType = selfInstantiatedCacheTypeTemp;
            }

            var cacheField = new FieldDefinition("iidData", FieldAttributes.Static | FieldAttributes.Assembly, new ArrayType(module.ImportReference(module.TypeSystem.Byte)));
            cacheType.Fields.Add(cacheField);
            implementationDetailsType.NestedTypes.Add(cacheType);

            var staticCtor = new MethodDefinition(".cctor", MethodAttributes.Static | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.Private, module.TypeSystem.Void);

            cacheType.Methods.Add(staticCtor);

            // In the body of the getter method, return the cache data
            var getterIL = guidDataGetterMethod.Body.GetILProcessor();
            getterIL.Emit(OpCodes.Ldsfld, new FieldReference(cacheField.Name, cacheField.FieldType, instantiatedCacheType));
            getterIL.Emit(OpCodes.Newobj, readOnlySpanOfByteArrayCtor);
            getterIL.Emit(OpCodes.Ret);

            // In the static constructor, calculate the guid bytes.
            var il = staticCtor.Body.GetILProcessor();
            var signatureParts = new VariableDefinition[signatureSteps.Count];

            var systemType = module.ImportReference(
                new TypeReference("System", "Type", module, module.TypeSystem.CoreLibrary));

            var getTypeFromHandleMethod = module.ImportReference(systemType.Resolve().Methods.First(m => string.CompareOrdinal(m.Name, "GetTypeFromHandle") == 0));
            var getSignatureMethod = module.ImportReference(
                new MethodReference("GetSignature", module.TypeSystem.String, guidGeneratorType)
                {
                    Parameters = { new ParameterDefinition(systemType) },
                    HasThis = false
                });

            var encodingType = CecilExtensions.FindTypeReference(module, "System.Text", "Encoding", "System.Runtime", false)!;
            var utf8EncodingGetter = module.ImportReference(new MethodReference("get_UTF8", encodingType, encodingType));
            var encodingGetBytes = module.ImportReference(
                new MethodReference("GetBytes", new ArrayType(module.TypeSystem.Byte), encodingType)
                {
                    Parameters = { new ParameterDefinition(module.TypeSystem.String) },
                    HasThis = true
                });

            var fullSignatureLength = new VariableDefinition(module.TypeSystem.Int32);
            staticCtor.Body.Variables.Add(fullSignatureLength);
            il.Emit(OpCodes.Ldc_I4, 16);
            il.Emit(OpCodes.Stloc, fullSignatureLength);

            for (int i = 0; i < signatureSteps.Count; i++)
            {
                signatureParts[i] = new VariableDefinition(readOnlySpanOfByte);
                staticCtor.Body.Variables.Add(signatureParts[i]);
                switch (signatureSteps[i])
                {
                    case StringStep(string str):
                        {
                            byte[] segmentBytes = Encoding.UTF8.GetBytes(str);
                            var staticDataField = new FieldDefinition($"<IIDDataField>{describedType.FullName}<SignatureDataPart={i}>", FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.Static | FieldAttributes.HasFieldRVA, CecilExtensions.GetOrCreateDataBlockType(implementationDetailsType, segmentBytes.Length))
                            {
                                InitialValue = segmentBytes
                            };
                            implementationDetailsType.Fields.Add(staticDataField);

                            // Load a ReadOnlySpan<byte> of the signature segment into the local for this step.
                            il.Emit(OpCodes.Ldsflda, new FieldReference(staticDataField.Name, staticDataField.FieldType, implementationDetailsType));
                            il.Emit(OpCodes.Ldc_I4, segmentBytes.Length);
                            il.Emit(OpCodes.Newobj, readOnlySpanOfBytePtrCtor);
                            il.Emit(OpCodes.Stloc, signatureParts[i]);
                            // signatureLength += staticData.Length
                            il.Emit(OpCodes.Ldloc, fullSignatureLength);
                            il.Emit(OpCodes.Ldc_I4, segmentBytes.Length);
                            il.Emit(OpCodes.Add_Ovf);
                            il.Emit(OpCodes.Stloc, fullSignatureLength);
                        }
                        break;
                    case RuntimeGenericSignatureStep(_, GenericParameter localTypeParameter):
                        {
                            // byte[] bytes = Encoding.UTF8.GetBytes(GetSignature(typeof(localTypeParameter)))
                            il.Emit(OpCodes.Call, utf8EncodingGetter);
                            il.Emit(OpCodes.Ldtoken, getterMethodGensToCacheTypeGens[localTypeParameter]);
                            il.Emit(OpCodes.Call, getTypeFromHandleMethod);
                            il.Emit(OpCodes.Call, getSignatureMethod);
                            il.Emit(OpCodes.Callvirt, encodingGetBytes);
                            il.Emit(OpCodes.Dup);
                            // <locals[i]> = new ReadOnlySpan<byte>(bytes);
                            il.Emit(OpCodes.Newobj, readOnlySpanOfByteArrayCtor);
                            il.Emit(OpCodes.Stloc, signatureParts[i]);
                            // signatureLength += bytes.Length
                            il.Emit(OpCodes.Ldlen);
                            il.Emit(OpCodes.Ldloc, fullSignatureLength);
                            il.Emit(OpCodes.Add_Ovf);
                            il.Emit(OpCodes.Stloc, fullSignatureLength);
                        }
                        break;
                    case RuntimeCustomSignatureStep(MethodReference customSignatureMethod):
                        {
                            // byte[] bytes = Encoding.UTF8.GetBytes(customSignatureMethod())
                            il.Emit(OpCodes.Call, utf8EncodingGetter);
                            il.Emit(OpCodes.Call, customSignatureMethod);
                            il.Emit(OpCodes.Callvirt, encodingGetBytes);
                            il.Emit(OpCodes.Dup);
                            // <locals[i]> = new ReadOnlySpan<byte>(bytes);
                            il.Emit(OpCodes.Newobj, readOnlySpanOfByteArrayCtor);
                            il.Emit(OpCodes.Stloc, signatureParts[i]);
                            // signatureLength += bytes.Length
                            il.Emit(OpCodes.Ldlen);
                            il.Emit(OpCodes.Ldloc, fullSignatureLength);
                            il.Emit(OpCodes.Add_Ovf);
                            il.Emit(OpCodes.Stloc, fullSignatureLength);
                        }
                        break;
                    default:
                        il.Clear();
                        throw new InvalidOperationException();
                }
            }

            var span = CecilExtensions.FindTypeReference(module, "System", "Span`1", "System.Runtime", true);

            if (span is null)
            {
                throw new InvalidOperationException();
            }

            var spanOfByte = new GenericInstanceType(span)
            {
                GenericArguments = { module.ImportReference(module.TypeSystem.Byte) }
            };

            var spanOfBytePtrCtor = module.ImportReference(new MethodReference(".ctor", module.TypeSystem.Void, spanOfByte)
            {
                Parameters =
                {
                    new ParameterDefinition(new PointerType(module.TypeSystem.Void)),
                    new ParameterDefinition(module.TypeSystem.Int32),
                },
                HasThis = true
            });

            var spanOfByteArrayCtor = module.ImportReference(
                new MethodReference(".ctor", module.TypeSystem.Void, spanOfByte)
                {
                    Parameters = { new ParameterDefinition(new ArrayType(span.Resolve().GenericParameters[0])) },
                    HasThis = true
                },
                spanOfByte);


            var copyToMethod = module.ImportReference(
                new MethodReference("CopyTo", module.TypeSystem.Void, readOnlySpanOfByte)
                {
                    Parameters =
                    {
                        new ParameterDefinition(
                            module.ImportReference(new GenericInstanceType(span) { GenericArguments = { readOnlySpanOfByte.Resolve().GenericParameters[0] } }))
                    },
                    HasThis = true
                });

            // return a Span<T> instead of Span<byte>
            var spanOfSpanElement = new GenericInstanceType(span) { GenericArguments = { span.Resolve().GenericParameters[0] } };

            var spanSliceStartMethod = module.ImportReference(
                new MethodReference("Slice", spanOfSpanElement, spanOfByte)
                {
                    HasThis = true,
                    Parameters = { new ParameterDefinition(module.TypeSystem.Int32) }
                });

            var spanSliceStartLengthMethod = module.ImportReference(
                new MethodReference("Slice", spanOfSpanElement, spanOfByte)
                {
                    HasThis = true,
                    Parameters = { new ParameterDefinition(module.TypeSystem.Int32), new ParameterDefinition(module.TypeSystem.Int32) }
                });

            var readOnlySpanOfByteLength = module.ImportReference(new MethodReference("get_Length", module.TypeSystem.Int32, readOnlySpanOfByte) { HasThis = true });

            var fullSignatureBuffer = new VariableDefinition(spanOfByte);
            staticCtor.Body.Variables.Add(fullSignatureBuffer);

            // fullSignatureBuffer = new Span<byte>(new byte[fullSignatureLength]);
            il.Emit(OpCodes.Ldloc, fullSignatureLength);
            il.Emit(OpCodes.Newarr, module.ImportReference(module.TypeSystem.Byte));
            il.Emit(OpCodes.Newobj, spanOfByteArrayCtor);
            il.Emit(OpCodes.Stloc, fullSignatureBuffer);

            // Write WinRTPinterfaceNamespace bytes to the buffer
            const string WinRTPinterfaceDataBlockName = "<WinRTPinterfaceNamespaceBytes>";

            var staticNamespaceBytesField = new FieldReference(WinRTPinterfaceDataBlockName, CecilExtensions.GetOrCreateDataBlockType(implementationDetailsType, 16), implementationDetailsType);

            if (staticNamespaceBytesField.Resolve() is null)
            {
                staticNamespaceBytesField = new FieldDefinition(WinRTPinterfaceDataBlockName, FieldAttributes.Static | FieldAttributes.Assembly | FieldAttributes.InitOnly | FieldAttributes.HasFieldRVA, CecilExtensions.GetOrCreateDataBlockType(implementationDetailsType, 16))
                {
                    InitialValue = WinRTPinterfaceNamespace.ToByteArray()
                };

                implementationDetailsType.Fields.Add((FieldDefinition)staticNamespaceBytesField);
            }

            il.Emit(OpCodes.Ldsflda, staticNamespaceBytesField);
            il.Emit(OpCodes.Ldc_I4, 16);
            var readOnlySpanTemp = new VariableDefinition(readOnlySpanOfByte);
            staticCtor.Body.Variables.Add(readOnlySpanTemp);
            il.Emit(OpCodes.Newobj, readOnlySpanOfBytePtrCtor);
            il.Emit(OpCodes.Stloc, readOnlySpanTemp);
            il.Emit(OpCodes.Ldloca, readOnlySpanTemp);
            il.Emit(OpCodes.Ldloc, fullSignatureBuffer);
            il.Emit(OpCodes.Call, copyToMethod);

            // int offset = 16;
            var offset = new VariableDefinition(module.TypeSystem.Int32);
            staticCtor.Body.Variables.Add(offset);
            il.Emit(OpCodes.Ldc_I4, 16);
            il.Emit(OpCodes.Stloc, offset);

            for (int i = 0; i < signatureParts.Length; i++)
            {
                // locals[i].CopyTo(fullSignatureBuffer.Slice(offset));
                il.Emit(OpCodes.Ldloca, signatureParts[i]);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldloca, fullSignatureBuffer);
                il.Emit(OpCodes.Ldloc, offset);
                il.Emit(OpCodes.Call, spanSliceStartMethod);
                il.Emit(OpCodes.Call, copyToMethod);
                // offset += locals[i].Length;
                il.Emit(OpCodes.Call, readOnlySpanOfByteLength);
                il.Emit(OpCodes.Ldloc, offset);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, offset);
            }

            var destination = new VariableDefinition(spanOfByte);
            staticCtor.Body.Variables.Add(destination);

            // Span<byte> destination = stackalloc byte[160];
            il.Emit(OpCodes.Ldc_I4, 160);
            il.Emit(OpCodes.Localloc);
            il.Emit(OpCodes.Ldc_I4, 160);
            il.Emit(OpCodes.Newobj, spanOfBytePtrCtor);
            il.Emit(OpCodes.Stloc, destination);

            // SHA1.HashData(fullSignatureBuffer, destination);
            var sha1Type = module.ImportReference(
                new TypeReference("System.Security.Cryptography", "SHA1", module, new AssemblyNameReference("System.Security.Cryptography.Algorithms", default), false).Resolve());
            var hashDataMethod = module.ImportReference(
                new MethodReference("HashData", module.ImportReference(module.TypeSystem.Int32), sha1Type)
                {
                    HasThis = false,
                    Parameters =
                    {
                        new ParameterDefinition(readOnlySpanOfByte),
                        new ParameterDefinition(spanOfByte)
                    }
                });

            // HashData is not defined in .NET Standard
            if (hashDataMethod.Resolve() is null)
            {
                var spanToArrayMethod = module.ImportReference(
                    new MethodReference("ToArray", new ArrayType(span.Resolve().GenericParameters[0]), spanOfByte)
                    {
                        HasThis = true,
                    });

                // byte[] arrayToHash = data.ToArray();
                var arrayToHash = new VariableDefinition(new ArrayType(module.TypeSystem.Byte));
                staticCtor.Body.Variables.Add(arrayToHash);
                il.Emit(OpCodes.Ldloca, fullSignatureBuffer);
                il.Emit(OpCodes.Call, spanToArrayMethod);
                il.Emit(OpCodes.Stloc, arrayToHash);

                // using (SHA1 sha = new SHA1CryptoServiceProvider())
                // destination = sha.ComputeHash(data);
                var sha1CryptoServiceProvider = module.ImportReference(
                    new TypeReference("System.Security.Cryptography", "SHA1CryptoServiceProvider", module, new AssemblyNameReference("netstandard", default), false).Resolve());
                var sha = new VariableDefinition(sha1CryptoServiceProvider);
                staticCtor.Body.Variables.Add(sha);
                var sha1CryptoServiceProviderCtor = module.ImportReference(
                    new MethodReference(".ctor", module.TypeSystem.Void, sha1CryptoServiceProvider)
                    {
                        HasThis = true
                    },
                    sha1CryptoServiceProvider);
                il.Emit(OpCodes.Newobj, sha1CryptoServiceProviderCtor);
                il.Emit(OpCodes.Stloc, sha);

                var computeHashMethod = module.ImportReference(
                    new MethodReference("ComputeHash", new ArrayType(module.TypeSystem.Byte), sha1CryptoServiceProvider)
                    {
                        HasThis = true,
                        Parameters =
                        {
                            new ParameterDefinition(new ArrayType(module.TypeSystem.Byte))
                        }
                    });
                il.Emit(OpCodes.Ldloc, sha);
                il.Emit(OpCodes.Ldloc, arrayToHash);
                il.Emit(OpCodes.Callvirt, computeHashMethod);
                il.Emit(OpCodes.Newobj, spanOfByteArrayCtor);
                il.Emit(OpCodes.Stloc, destination);

                var disposable = module.ImportReference(
                    new TypeReference("System", "IDisposable", module, new AssemblyNameReference("netstandard", default), false).Resolve());
                var disposeMethod = module.ImportReference(
                    new MethodReference("Dispose", module.TypeSystem.Void, disposable)
                    {
                        HasThis = true,
                    });
                il.Emit(OpCodes.Ldloc, sha);
                il.Emit(OpCodes.Callvirt, disposeMethod);
            }
            else
            {
                var spanToReadOnlySpan = module.ImportReference(
                    new MethodReference("op_Implicit",
                        new GenericInstanceType(module.ImportReference(readOnlySpanOfByte.Resolve()))
                        {
                            GenericArguments = { span.Resolve().GenericParameters[0] }
                        },
                        spanOfByte)
                    {
                        HasThis = false,
                        Parameters =
                        {
                        new ParameterDefinition(
                            new GenericInstanceType(span)
                            {
                                GenericArguments = { span.Resolve().GenericParameters[0] }
                            })
                        }
                    });

                il.Emit(OpCodes.Ldloc, fullSignatureBuffer);
                il.Emit(OpCodes.Call, spanToReadOnlySpan);
                il.Emit(OpCodes.Ldloc, destination);
                il.Emit(OpCodes.Call, hashDataMethod);
                il.Emit(OpCodes.Pop);
            }

            // Fix endianness, bytes
            var memoryExtensions = CecilExtensions.FindTypeReference(module, "System", "MemoryExtensions", "System.Memory", false);

            var reverseMethod_Generic = new MethodReference("Reverse", module.TypeSystem.Void, memoryExtensions) { };

            var reverseMethod = new MethodReference("Reverse", module.TypeSystem.Void, memoryExtensions) { };

            var reverseMethodGenericParam = new GenericParameter(reverseMethod);
            reverseMethod.GenericParameters.Add(reverseMethodGenericParam);
            reverseMethod.Parameters.Add(new ParameterDefinition(new GenericInstanceType(span) { GenericArguments = { reverseMethodGenericParam } }));
            reverseMethod = module.ImportReference(
                new GenericInstanceMethod(reverseMethod)
                {
                    GenericArguments = { module.TypeSystem.Byte }
                });

            // Filp endianness to little endian for the int/short components of the guid.
            il.Emit(OpCodes.Ldloca, destination);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldc_I4_4);
            il.Emit(OpCodes.Call, spanSliceStartLengthMethod);
            il.Emit(OpCodes.Call, reverseMethod);
            il.Emit(OpCodes.Ldloca, destination);
            il.Emit(OpCodes.Ldc_I4_4);
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Call, spanSliceStartLengthMethod);
            il.Emit(OpCodes.Call, reverseMethod);
            il.Emit(OpCodes.Ldloca, destination);
            il.Emit(OpCodes.Ldc_I4_6);
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Call, spanSliceStartLengthMethod);
            il.Emit(OpCodes.Call, reverseMethod);
            
            // Encode rfc time/version/clock/reserved fields
            var getItemMethod = module.ImportReference(
                new MethodReference("get_Item", new ByReferenceType(span.Resolve().GenericParameters[0]), spanOfByte) 
                { 
                    Parameters = { new ParameterDefinition(module.TypeSystem.Int32) }, 
                    HasThis = true 
                });
            
            // t[7] = (byte) ((t[7] & 0x0f) | (5 << 4));
            il.Emit(OpCodes.Ldloca, destination);
            il.Emit(OpCodes.Ldc_I4_7);
            il.Emit(OpCodes.Call, getItemMethod);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldind_U1);
            il.Emit(OpCodes.Ldc_I4, 0x0f);
            il.Emit(OpCodes.And);
            il.Emit(OpCodes.Ldc_I4, 5 << 4);
            il.Emit(OpCodes.Or);
            il.Emit(OpCodes.Conv_U1);
            il.Emit(OpCodes.Stind_I1);

            // t[8] = (byte)((t[8] & 0x3f) | 0x80);
            il.Emit(OpCodes.Ldloca, destination);
            il.Emit(OpCodes.Ldc_I4_8);
            il.Emit(OpCodes.Call, getItemMethod);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldind_U1);
            il.Emit(OpCodes.Ldc_I4, 0x3f);
            il.Emit(OpCodes.And);
            il.Emit(OpCodes.Ldc_I4, 0x80);
            il.Emit(OpCodes.Or);
            il.Emit(OpCodes.Conv_U1);
            il.Emit(OpCodes.Stind_I1);

            // cacheField = destination.Slice(0, 16).ToArray()

            var toArrayMethod = module.ImportReference(
                new MethodReference("ToArray", new ArrayType(span.Resolve().GenericParameters[0]), spanOfByte) 
                { 
                    HasThis = true 
                });

            var spanTemp = new VariableDefinition(spanOfByte);
            staticCtor.Body.Variables.Add(spanTemp);

            
            il.Emit(OpCodes.Ldloca, destination);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldc_I4, 16);
            il.Emit(OpCodes.Call, spanSliceStartLengthMethod);
            il.Emit(OpCodes.Stloc, spanTemp);
            il.Emit(OpCodes.Ldloca, spanTemp);
            il.Emit(OpCodes.Call, toArrayMethod);
            il.Emit(OpCodes.Stsfld, new FieldReference(cacheField.Name, cacheField.FieldType, selfInstantiatedCacheType));
            il.Emit(OpCodes.Ret);
        }
    }
}
