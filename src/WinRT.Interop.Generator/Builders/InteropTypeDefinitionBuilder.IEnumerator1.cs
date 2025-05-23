// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <inheritdoc cref="InteropTypeDefinitionBuilder"/>
internal partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Helpers for <see cref="System.Collections.Generic.IEnumerator{T}"/> types.
    /// </summary>
    public static class IEnumerator1
    {
        /// <summary>
        /// Creates a new type definition for the methods for an <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="iteratorMethodsType">The resulting methods type.</param>
        public static void IIteratorMethods(
            GenericInstanceTypeSignature enumeratorType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition iteratorMethodsType)
        {
            // We're declaring an 'internal abstract class' type
            iteratorMethodsType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType, "IIteratorMethods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

            module.TopLevelTypes.Add(iteratorMethodsType);

            // Define the 'Current' method as follows:
            //
            // public static <TYPE_ARGUMENT> Current(WindowsRuntimeObjectReference thisReference)
            MethodDefinition currentMethod = new(
                name: "Current"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: enumeratorType.TypeArguments[0].Import(module),
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false)]))
            {
                NoInlining = true
            };

            iteratorMethodsType.Methods.Add(currentMethod);

            // Declare the local variables:
            //   [0]: 'WindowsRuntimeObjectReferenceValue' (for 'thisValue')
            //   [1]: 'void*' (for 'thisPtr')
            //   [2]: 'void*' (the native value that was retrieved)
            CilLocalVariable loc_0_thisValue = new(interopReferences.WindowsRuntimeObjectReferenceValue.ToTypeSignature(isValueType: true).Import(module));
            CilLocalVariable loc_1_thisPtr = new(module.CorLibTypeFactory.Void.MakePointerType());
            CilLocalVariable loc_2_currentNative = new(module.CorLibTypeFactory.Void.MakePointerType());

            // Jump labels
            CilInstruction ldloca_s_0_tryStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction ldloca_s_0_finallyStart = new(Ldloca_S, loc_0_thisValue);
            CilInstruction nop_finallyEnd = new(Nop);

            // Create a method body for the 'Current' method
            currentMethod.CilMethodBody = new CilMethodBody(currentMethod)
            {
                LocalVariables = { loc_0_thisValue, loc_1_thisPtr, loc_2_currentNative },
                Instructions =
                {
                    // Initialize 'thisValue'
                    { Ldarg_0 },
                    { Callvirt, interopReferences.WindowsRuntimeObjectReferenceAsValue.Import(module) },
                    { Stloc_0 },

                    // '.try' code
                    { ldloca_s_0_tryStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueGetThisPtrUnsafe.Import(module) },
                    { Stloc_1 },
                    { Ldloc_1 },
                    { Ldloca_S, loc_2_currentNative },
                    { Ldloc_1 },
                    { Ldind_I },
                    { Ldfld, interopDefinitions.IEnumerator1Vftbl.GetField("get_Current"u8) },
                    { Calli, WellKnownTypeSignatureFactory.IEnumerator1CurrentImpl(interopReferences).Import(module).MakeStandAloneSignature() },
                    { Call, interopReferences.RestrictedErrorInfoThrowExceptionForHR.Import(module) },
                    { Leave_S, nop_finallyEnd.CreateLabel() },

                    // '.finally' code
                    { ldloca_s_0_finallyStart },
                    { Call, interopReferences.WindowsRuntimeObjectReferenceValueDispose.Import(module) },
                    { Endfinally },
                    { nop_finallyEnd }
                },
                ExceptionHandlers =
                {
                    new CilExceptionHandler
                    {
                        HandlerType = CilExceptionHandlerType.Finally,
                        TryStart = ldloca_s_0_tryStart.CreateLabel(),
                        TryEnd = ldloca_s_0_finallyStart.CreateLabel(),
                        HandlerStart = ldloca_s_0_finallyStart.CreateLabel(),
                        HandlerEnd = nop_finallyEnd.CreateLabel()
                    }
                }
            };

            // If the value is blittable, return it directly
            if (enumeratorType.TypeArguments[0].IsValueType) // TODO
            {
                _ = currentMethod.CilMethodBody.Instructions.Add(Ldloc_2);
                _ = currentMethod.CilMethodBody.Instructions.Add(Ret);
            }
            else
            {
                CilInstructionCollection instructions = currentMethod.CilMethodBody.Instructions;

                // Declare an additional variable:
                //   [3]: '<TYPE_ARGUMENT>' (for the marshalled value)
                CilLocalVariable loc_3_current = new(enumeratorType.TypeArguments[0].Import(module));

                currentMethod.CilMethodBody.LocalVariables.Add(loc_3_current);

                // Jump labels for the 'try/finally' blocks
                CilInstruction ldloc_2_tryStart = new(Ldloc_2);
                CilInstruction ldloc_2_finallyStart = new(Ldloc_2);
                CilInstruction ldloc_3_finallyEnd = new(Ldloc_3);

                // We need to marshal the native value to a managed object
                instructions.Add(ldloc_2_tryStart);
                _ = instructions.Add(Call, interopReferences.HStringMarshallerConvertToManaged.Import(module));
                _ = instructions.Add(Stloc_3);
                _ = instructions.Add(Leave_S, ldloc_3_finallyEnd.CreateLabel());
                instructions.Add(ldloc_2_finallyStart);
                _ = instructions.Add(Call, interopReferences.HStringMarshallerFree.Import(module));
                _ = instructions.Add(Endfinally);
                instructions.Add(ldloc_3_finallyEnd);
                _ = instructions.Add(Ret);

                // Register the 'try/finally'
                currentMethod.CilMethodBody.ExceptionHandlers.Add(new CilExceptionHandler
                {
                    HandlerType = CilExceptionHandlerType.Finally,
                    TryStart = ldloc_2_tryStart.CreateLabel(),
                    TryEnd = ldloc_2_finallyStart.CreateLabel(),
                    HandlerStart = ldloc_2_finallyStart.CreateLabel(),
                    HandlerEnd = ldloc_3_finallyEnd.CreateLabel()
                });
            }

            // Define the 'HasCurrent' method as follows:
            //
            // public static bool HasCurrent(WindowsRuntimeObjectReference thisReference)
            MethodDefinition hasCurrentMethod = new(
                name: "HasCurrent"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false)]));

            iteratorMethodsType.Methods.Add(hasCurrentMethod);

            // Create a method body for the 'HasCurrent' method
            hasCurrentMethod.CilMethodBody = new CilMethodBody(hasCurrentMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IIteratorMethodsHasCurrent.Import(module) },
                    { Ret }
                }
            };

            // Define the 'MoveNext' method as follows:
            //
            // public static bool HasCurrent(WindowsRuntimeObjectReference thisReference)
            MethodDefinition moveNextMethod = new(
                name: "MoveNext"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false)]));

            iteratorMethodsType.Methods.Add(moveNextMethod);

            // Create a method body for the 'HasCurrent' method
            moveNextMethod.CilMethodBody = new CilMethodBody(moveNextMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IIteratorMethodsMoveNext.Import(module) },
                    { Ret }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the native object for an <c>IIterator&lt;T&gt;</c> interface.
        /// </summary>
        /// <param name="enumeratorType">The <see cref="TypeSignature"/> for the <see cref="System.Collections.Generic.IEnumerator{T}"/> type type.</param>
        /// <param name="iteratorMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="IIteratorMethods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature enumeratorType,
            TypeDefinition iteratorMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            TypeSignature elementType = enumeratorType.TypeArguments[0];
            TypeSignature windowsRuntimeEnumerator1Type = interopReferences.WindowsRuntimeEnumerator1.MakeGenericInstanceType(elementType);

            // We're declaring an 'internal sealed class' type
            nativeObjectType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(enumeratorType),
                name: InteropUtf8NameFactory.TypeName(enumeratorType, "NativeObject"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed,
                baseType: windowsRuntimeEnumerator1Type.Import(module).ToTypeDefOrRef());

            module.TopLevelTypes.Add(nativeObjectType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateConstructor(module, interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false));

            nativeObjectType.Methods.Add(ctor);

            _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
            _ = ctor.CilMethodBody!.Instructions.Insert(1, Ldarg_1);
            _ = ctor.CilMethodBody!.Instructions.Insert(2, Call, interopReferences.WindowsRuntimeEnumerator1_ctor(windowsRuntimeEnumerator1Type).Import(module));

            // Define the 'CurrentNative' method as follows:
            //
            // public static <ELEMENT_TYPE> CurrentNative()
            MethodDefinition currentNativeMethod = new(
                name: "CurrentNative"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(elementType.Import(module)));

            nativeObjectType.Methods.Add(currentNativeMethod);

            // Mark the 'CurrentNative' method as overriding the base method
            nativeObjectType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.WindowsRuntimeEnumerator1CurrentNative.Import(module),
                body: currentNativeMethod));

            // Create a method body for the 'CurrentNative' method
            currentNativeMethod.CilMethodBody = new CilMethodBody(currentNativeMethod)
            {
                Instructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.WindowsRuntimeObjectget_NativeObjectReference.Import(module) },
                    { Call, iteratorMethodsType.GetMethod("Current"u8) },
                    { Ret }
                }
            };
        }
    }
}
