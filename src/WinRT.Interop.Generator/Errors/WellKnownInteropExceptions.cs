// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using WindowsRuntime.InteropGenerator.Fixups;

namespace WindowsRuntime.InteropGenerator.Errors;

/// <summary>
/// Well known exceptions for the interop generator.
/// </summary>
internal static class WellKnownInteropExceptions
{
    /// <summary>
    /// The prefix for all errors produced by this tool.
    /// </summary>
    public const string ErrorPrefix = "CSWINRTGEN";

    /// <summary>
    /// A runtime class name is too long.
    /// </summary>
    public static WellKnownInteropException RuntimeClassNameTooLong(string name)
    {
        return Exception(1, $"The runtime class name '{name}' is too long. The maximum length is {ushort.MaxValue} characters.");
    }

    /// <summary>
    /// There are too many runtime class names to build the lookup.
    /// </summary>
    public static WellKnownInteropException RuntimeClassNameLookupSizeLimitExceeded()
    {
        return Exception(2, "The runtime class name lookup size limit was exceeded.");
    }

    /// <summary>
    /// The assembly module was not found.
    /// </summary>
    public static WellKnownInteropException AssemblyModuleNotFound()
    {
        return Exception(3, "The assembly module was not found (this might mean that its path was not valid, or that it failed to load).");
    }

    /// <summary>
    /// The WinRT runtime module was not found.
    /// </summary>
    public static WellKnownInteropException WinRTRuntimeModuleNotFound()
    {
        return Exception(4, "The WinRT runtime module (i.e. 'WinRT.Runtime.dll') was not found (this might mean that its path was not valid, or that it failed to load).");
    }

    /// <summary>
    /// Exception when emitting the 'Values' RVA field.
    /// </summary>
    public static WellKnownInteropException TypeHierarchyValuesRvaError(Exception exception)
    {
        return Exception(5, "Failed to generate data for the 'Values' RVA field for the type hierarchy lookup.", exception);
    }

    /// <summary>
    /// Exception when emitting the 'Keys' RVA field.
    /// </summary>
    public static WellKnownInteropException TypeHierarchyKeysRvaError(Exception exception)
    {
        return Exception(6, "Failed to generate data for the 'Keys' RVA field for the type hierarchy lookup.", exception);
    }

    /// <summary>
    /// Exception when emitting the 'Buckets' RVA field.
    /// </summary>
    public static WellKnownInteropException TypeHierarchyBucketsRvaError(Exception exception)
    {
        return Exception(7, "Failed to generate data for the 'Buckets' RVA field for the type hierarchy lookup.", exception);
    }

    /// <summary>
    /// Exception when emitting the type hierarchy implementation.
    /// </summary>
    public static WellKnownInteropException TypeHierarchyImplementationError(Exception exception)
    {
        return Exception(8, "Failed to generate the method implementations for the type hierarchy lookup.", exception);
    }

    /// <summary>
    /// Exception when emitting the interop .dll to disk.
    /// </summary>
    public static WellKnownInteropException EmitDllError(Exception exception)
    {
        return Exception(9, "Failed to emit the interop .dll to disk.", exception);
    }

    /// <summary>
    /// The discovery state was changed after making it readonly.
    /// </summary>
    public static WellKnownInteropException DiscoveryStateChangeAfterMakeReadOnlyError()
    {
        return Exception(10, "An attempt was made to mutate the generator discovery state after it was made readonly (in the emit phase).");
    }

    /// <summary>
    /// Failed to generate marshalling code for a delegate type.
    /// </summary>
    public static WellKnownInteropException DelegateTypeCodeGenerationError(string? delegateType, Exception exception)
    {
        return Exception(11, $"Failed to generate marshalling code for delegate type '{delegateType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for a <see cref="KeyValuePair{TKey, TValue}"/> type.
    /// </summary>
    public static WellKnownInteropException KeyValuePairTypeCodeGenerationError(string? keyValuePairType, Exception exception)
    {
        return Exception(12, $"Failed to generate marshalling code for 'KeyValuePair<,>' type '{keyValuePairType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for a default implementation detail type.
    /// </summary>
    public static WellKnownInteropException DefaultImplementationDetailTypeCodeGenerationError(Exception exception)
    {
        return Exception(13, $"Failed to generate marshalling code for some default implementation detail type.", exception);
    }

    /// <summary>
    /// Failed to discover type hierarchy types.
    /// </summary>
    public static WellKnownInteropException DiscoverTypeHierarchyTypesError(string? name, Exception exception)
    {
        return Exception(14, $"Failed to discover type hierarchy types for module '{name}'.", exception);
    }

    /// <summary>
    /// Failed to discover generic type instantiations.
    /// </summary>
    public static WellKnownInteropException DiscoverGenericTypeInstantiationsError(string? name, Exception exception)
    {
        return Exception(15, $"Failed to discover generic type instantiations for module '{name}'.", exception);
    }

    /// <summary>
    /// Failed to discover generic type instantiations.
    /// </summary>
    public static WellKnownInteropException LoadAndDiscoverModulesLoopDidNotComplete()
    {
        return Exception(16, "Failed to complete processing all input modules.");
    }

    /// <summary>
    /// Failed to discover generic type instantiations.
    /// </summary>
    public static WellKnownInteropException LoadAndDiscoverModulesLoopError(Exception exception)
    {
        return Exception(17, "Failed to load and process all input modules.", exception);
    }

    /// <summary>
    /// Failed to define the interop assembly.
    /// </summary>
    public static WellKnownInteropException DefineInteropAssemblyError(Exception exception)
    {
        return Exception(18, "Failed to define the interop module and assembly.", exception);
    }

    /// <summary>
    /// Failed to define the <c>[IgnoresAccessChecksTo]</c> attributes.
    /// </summary>
    public static WellKnownInteropException DefineIgnoresAccessChecksToAttributesError(Exception exception)
    {
        return Exception(19, "Failed to generate the '[IgnoresAccessChecksTo]' attribute definition and annotations.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <see cref="IEnumerator{T}"/> type.
    /// </summary>
    public static WellKnownInteropException IEnumerator1TypeCodeGenerationError(TypeSignature enumeratorType, Exception exception)
    {
        return Exception(20, $"Failed to generate marshalling code for 'IEnumerator<T>' type '{enumeratorType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <see cref="IEnumerable{T}"/> type.
    /// </summary>
    public static WellKnownInteropException IEnumerable1TypeCodeGenerationError(TypeSignature enumerableType, Exception exception)
    {
        return Exception(21, $"Failed to generate marshalling code for 'IEnumerable<T>' type '{enumerableType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <see cref="IReadOnlyList{T}"/> type.
    /// </summary>
    public static WellKnownInteropException IReadOnlyList1TypeCodeGenerationError(TypeSignature readOnlyListType, Exception exception)
    {
        return Exception(22, $"Failed to generate marshalling code for 'IReadOnlyList<T>' type '{readOnlyListType}'.", exception);
    }

    /// <summary>
    /// Registering a tracked type definition multiple times for the same signature.
    /// </summary>
    /// <param name="typeSignature">The type signature.</param>
    /// <param name="key">The key.</param>
    public static WellKnownInteropException AddingDuplicateTrackedTypeDefinition(TypeSignature typeSignature, string key)
    {
        return Exception(23, $"Duplicate tracked type definition for signature '{typeSignature}' and key '{key}'.");
    }

    /// <summary>
    /// Failed to find a tracked type definition.
    /// </summary>
    /// <param name="typeSignature">The type signature.</param>
    /// <param name="key">The key.</param>
    public static WellKnownInteropException TrackedTypeDefinitionLookupError(TypeSignature typeSignature, string key)
    {
        return Exception(24, $"Failed to find a tracked type definition for signature '{typeSignature}' and key '{key}'.");
    }

    /// <summary>
    /// Failed to generate marshalling code for an <see cref="IList{T}"/> type.
    /// </summary>
    public static WellKnownInteropException IList1TypeCodeGenerationError(TypeSignature listType, Exception exception)
    {
        return Exception(25, $"Failed to generate marshalling code for 'IList<T>' type '{listType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <see cref="IReadOnlyDictionary{TKey, TValue}"/> type.
    /// </summary>
    public static WellKnownInteropException IReadOnlyDictionary2TypeCodeGenerationError(TypeSignature readOnlyDictionaryType, Exception exception)
    {
        return Exception(26, $"Failed to generate marshalling code for 'IReadOnlyDictionary<TKey, TValue>' type '{readOnlyDictionaryType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <see cref="IDictionary{TKey, TValue}"/> type.
    /// </summary>
    public static WellKnownInteropException IDictionary2TypeCodeGenerationError(TypeSignature dictionaryType, Exception exception)
    {
        return Exception(27, $"Failed to generate marshalling code for 'IDictionary<TKey, TValue>' type '{dictionaryType}'.", exception);
    }

    /// <summary>
    /// Some exception was thrown when trying to read the response file.
    /// </summary>
    public static WellKnownInteropException ResponseFileReadError(Exception exception)
    {
        return Exception(28, "Failed to read the response file to run 'cswinrtgen'.", exception);
    }

    /// <summary>
    /// Failed to parse an argument from the response file.
    /// </summary>
    public static WellKnownInteropException ResponseFileArgumentParsingError(string argumentName, Exception? exception = null)
    {
        return Exception(29, $"Failed to parse argument '{argumentName}' from response file.", exception);
    }

    /// <summary>
    /// The input response file is malformed.
    /// </summary>
    public static WellKnownInteropException MalformedResponseFile()
    {
        return Exception(30, "The response file is malformed and contains invalid content.");
    }

    /// <summary>
    /// The debug repro directory does not exist.
    /// </summary>
    public static WellKnownInteropException DebugReproDirectoryDoesNotExist(string path)
    {
        return Exception(31, $"The debug repro directory '{path}' does not exist.");
    }

    /// <summary>
    /// The debug repro directory does not exist.
    /// </summary>
    public static WellKnownInteropException WinRTRuntimeAssemblyVersionMismatch(
        Version? winRTRuntimeAssemblyVersion,
        Version? cswinrtgenAssemblyVersion)
    {
        return Exception(32,
            $"The assembly version of 'WinRT.Runtime.dll' ('{winRTRuntimeAssemblyVersion}') does not match the assembly version of the 'cswinrtgen' " +
            $"tool being used ('{cswinrtgenAssemblyVersion}'). The two binaries must be versioned together to ensure things work correctly.");
    }

    /// <summary>
    /// Failed to discover SZ array types.
    /// </summary>
    public static WellKnownInteropException DiscoverSzArrayTypesError(string? name, Exception exception)
    {
        return Exception(33, $"Failed to discover SZ array type for module '{name}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for a SZ array type.
    /// </summary>
    public static WellKnownInteropException SzArrayTypeCodeGenerationError(string? arrayType, Exception exception)
    {
        return Exception(34, $"Failed to generate marshalling code for SZ array type '{arrayType}'.", exception);
    }

    /// <summary>
    /// Failed to discover SZ array types.
    /// </summary>
    public static WellKnownInteropException WinRTRuntimeDllVersion2References(IEnumerable<string> names)
    {
        string combinedNames = string.Join(", ", names.Select(static name => $"'{name}'"));

        return Exception(35,
            $"One or more referenced assemblies were compiled against CsWinRT 2.x (i.e. referencing 'WinRT.Runtime.dll' version '2.x'), which is not compatible with CsWinRT 3.0 (i.e. referencing 'WinRT.Runtime.dll' version '3.0'). " +
            $"CsWinRT 3.0 is not backward-compatible with CsWinRT 2.x, so referencing any assemblies targeting an older version of CsWinRT is not supported. Those assemblies will need to be recompiled using CsWinRT 3.0 (i.e. using" +
            $"a TFM such as 'net10.0-windows10.0.26100.1', where the '.1' revision number at the end of the target platform version indicates CsWinRT 3.0). The following assemblies are compiled with the older version: {combinedNames}.");
    }

    /// <summary>
    /// Exception when no type hierarchy key-value pairs were discovered.
    /// </summary>
    public static WellKnownInteropException TypeHierarchyNoDiscoveredKeyValuePairs()
    {
        return Exception(36,
            "No type hierarchy key-value pairs were discovered across referenced assemblies. This should never happen, and it indicates that either " +
            "the referenced assemblies were not valid (e.g. incorrectly mixing CsWinRT 2.x and 3.0 projection assemblies), or that some other " +
            "unexpected error occurred during the discovery phase, which wasn't correctly identified earlier in the generation process.");
    }

    /// <summary>
    /// Failed to discover exposed user-defined types.
    /// </summary>
    public static WellKnownInteropException DiscoverExposedUserDefinedTypesError(string? name, Exception exception)
    {
        return Exception(37, $"Failed to discover (non-generic) exposed user-defined types for module '{name}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for a user-defined type.
    /// </summary>
    public static WellKnownInteropException UserDefinedTypeCodeGenerationError(string? userDefinedType, Exception exception)
    {
        return Exception(38, $"Failed to generate marshalling code for user-defined type '{userDefinedType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for a user-defined vtable type.
    /// </summary>
    public static WellKnownInteropException UserDefinedVtableTypeCodeGenerationError(string? userDefinedType, Exception exception)
    {
        return Exception(39, $"Failed to generate marshalling code for user-defined vtable type '{userDefinedType}'.", exception);
    }

    /// <summary>
    /// The Windows SDK projection module was not found.
    /// </summary>
    public static WellKnownInteropException WindowsSdkProjectionModuleNotFound()
    {
        return Exception(40, "The Windows SDK projection module (i.e. 'Microsoft.Windows.SDK.NET.dll') was not found (this might mean that its path was not valid, or that it failed to load).");
    }

    /// <summary>
    /// Failed to generate marshalling code for an <c>Windows.Foundation.Collections.IMapChangedEventArgs&lt;K&gt;</c> type.
    /// </summary>
    public static WellKnownInteropException IMapChangedEventArgs1TypeCodeGenerationError(TypeSignature argsType, Exception exception)
    {
        return Exception(41, $"Failed to generate marshalling code for 'IMapChangedEventArgs<K>' type '{argsType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <c>Windows.Foundation.Collections.IObservableVector&lt;T&gt;</c> type.
    /// </summary>
    public static WellKnownInteropException IObservableVectorTypeCodeGenerationError(TypeSignature elementType, Exception exception)
    {
        return Exception(41, $"Failed to generate marshalling code for 'IObservableVector<T>' type '{elementType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <c>Windows.Foundation.Collections.IObservableMap&lt;K, V&gt;</c> type.
    /// </summary>
    public static WellKnownInteropException IObservableMapTypeCodeGenerationError(TypeSignature elementType, Exception exception)
    {
        return Exception(42, $"Failed to generate marshalling code for 'IObservableMap<K, V>' type '{elementType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for a dynamic implementation detail type.
    /// </summary>
    public static WellKnownInteropException DynamicImplementationDetailTypeCodeGenerationError(Exception exception)
    {
        return Exception(43, $"Failed to generate marshalling code for some dynamic implementation detail type.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;</c> type.
    /// </summary>
    public static WellKnownInteropException IAsyncActionWithProgressTypeCodeGenerationError(TypeSignature actionType, Exception exception)
    {
        return Exception(44, $"Failed to generate marshalling code for 'IAsyncActionWithProgress<TResult>' type '{actionType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <c>Windows.Foundation.IAsyncOperation&lt;TResult&gt;</c> type.
    /// </summary>
    public static WellKnownInteropException IAsyncOperationTypeCodeGenerationError(TypeSignature operationType, Exception exception)
    {
        return Exception(45, $"Failed to generate marshalling code for 'IAsyncOperation<TResult>' type '{operationType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <c>Windows.Foundation.IAsyncOperationWithProgress&lt;TResult, TProgress&gt;</c> type.
    /// </summary>
    public static WellKnownInteropException IAsyncOperationWithProgressTypeCodeGenerationError(TypeSignature operationType, Exception exception)
    {
        return Exception(46, $"Failed to generate marshalling code for 'IAsyncOperationWithProgress<TResult, TProgress>' type '{operationType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for a dynamic implementation detail type.
    /// </summary>
    public static WellKnownInteropException DynamicDynamicCustomMappedTypeMapEntriesCodeGenerationError(Exception exception)
    {
        return Exception(47, $"Failed to generate type map entries for some dynamic custom-mapped types.", exception);
    }

    /// <summary>
    /// Failed to resolve the associated <c>ComWrappersMarshallerAttribute</c> type for a custom-mapped type.
    /// </summary>
    public static WellKnownInteropException CustomMappedTypeComWrappersMarshallerAttributeTypeResolveError(TypeSignature type)
    {
        return Exception(48, $"Failed to resolve the associated 'ComWrappersMarshallerAttribute' type for the custom-mapped type '{type}'.");
    }

    /// <summary>
    /// Failed to resolve the type of an implemented interface.
    /// </summary>
    public static WellKnownInteropWarning InterfaceImplementationTypeNotResolvedWarning(TypeSignature interfaceType, TypeSignature type)
    {
        return Warning(49, $"Failed to resolve interface type '{interfaceType}' while processing type '{type}': the interface will not be included in the set of available COM interface entries.");
    }

    /// <summary>
    /// Failed to resolve the generated 'System.Runtime.InteropServices.Marshalling.IIUnknownInterfaceType' implementation for a given interface type.
    /// </summary>
    public static WellKnownInteropWarning GeneratedComInterfaceImplementationTypeNotFoundWarning(TypeDefinition interfaceType, TypeDefinition type)
    {
        return Warning(50, $"Failed to resolve the generated 'System.Runtime.InteropServices.Marshalling.IIUnknownInterfaceType' implementation for the '[GeneratedComInterface]' type '{interfaceType}' while processing type '{type}': the interface will not be included in the set of available COM interface entries.");
    }

    /// <summary>
    /// Invalid custom-mapped type used to get an IID.
    /// </summary>
    public static WellKnownInteropException InvalidCustomMappedTypeForWellKnownInterfaceIIDs(ITypeDescriptor interfaceType)
    {
        return Exception(51, $"Type '{interfaceType}' is not a valid well-known custom-mapped interface type: its IID could not be retrieved.");
    }

    /// <summary>
    /// Failed to generate marshalling code for an <see cref="IReadOnlyCollection{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> type.
    /// </summary>
    public static WellKnownInteropException IReadOnlyCollectionKeyValuePairTypeCodeGenerationError(TypeSignature operationType, Exception exception)
    {
        return Exception(52, $"Failed to generate marshalling code for 'IReadOnlyCollection<KeyValuePair<TResult, TProgress>>' type '{operationType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <see cref="ICollection{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> type.
    /// </summary>
    public static WellKnownInteropException ICollectionKeyValuePairTypeCodeGenerationError(TypeSignature operationType, Exception exception)
    {
        return Exception(53, $"Failed to generate marshalling code for 'ICollection<KeyValuePair<TKey, TValue>>' type '{operationType}'.", exception);
    }

    /// <summary>
    /// Failed to generate the signature of some Windows Runtime type.
    /// </summary>
    public static Exception TypeSignatureGenerationError(TypeSignature type)
    {
        return Exception(54, $"Failed to generate the type signature for type '{type}'.");
    }

    /// <summary>
    /// The emit state was changed after making it readonly.
    /// </summary>
    public static WellKnownInteropException EmitStateChangeAfterMakeReadOnlyError()
    {
        return Exception(55, "An attempt was made to mutate the generator emit state after it was made readonly.");
    }

    /// <summary>
    /// Failed to perform two-pass rewrite of a marshalling method.
    /// </summary>
    public static WellKnownInteropException MethodRewriteError(TypeSignature type, MethodDefinition method, Exception exception)
    {
        return Exception(56, $"Failed to perform two-pass rewrite of method '{method}' to marshal type '{type}'.", exception);
    }

    /// <summary>
    /// A generated interop method is missing an IL method body.
    /// </summary>
    public static WellKnownInteropException MethodRewriteMissingBodyError(MethodDefinition method)
    {
        return Exception(57, $"Generated interop method '{method}' is missing an IL method body, two-pass rewrite cannot be performed.");
    }

    /// <summary>
    /// A marker instruction was not found in a generated interop method.
    /// </summary>
    public static WellKnownInteropException MethodRewriteMarkerInstructionNotFoundError(CilInstruction marker, MethodDefinition method)
    {
        return Exception(58, $"Marker instruction '{marker.OpCode.Code}' not found in generated interop method '{method}'.");
    }

    /// <summary>
    /// A source local variable was not found in a generated interop method.
    /// </summary>
    public static WellKnownInteropException MethodRewriteSourceLocalNotFoundError(CilLocalVariable source, MethodDefinition method)
    {
        return Exception(59, $"Local variable of type '{source.VariableType}' not found in generated interop method '{method}'.");
    }

    /// <summary>
    /// Failed to generate marshalling code for a <see cref="KeyValuePair{TKey, TValue}"/> type.
    /// </summary>
    public static WellKnownInteropException KeyValuePairTypeSharedCodeGenerationError(Exception exception)
    {
        return Exception(60, $"Failed to generate shared marshalling code for 'KeyValuePair<,>' types.", exception);
    }

    /// <summary>
    /// Failed to resolve the '[Guid]' attribute for a given interface type.
    /// </summary>
    public static WellKnownInteropWarning GeneratedComInterfaceGuidAttributeNotFoundWarning(TypeDefinition interfaceType, TypeDefinition type)
    {
        return Warning(61, $"Failed to resolve the '[Guid]' attribute for the '[GeneratedComInterface]' type '{interfaceType}' while processing type '{type}': the interface will not be included in the set of available COM interface entries.");
    }

    /// <summary>
    /// Failed to resolve a base type for a user-defined type.
    /// </summary>
    public static WellKnownInteropWarning UserDefinedTypeNotFullyResolvedWarning(ITypeDefOrRef baseType, TypeDefinition type)
    {
        return Warning(62, $"Failed to resolve the base type '{baseType}' in the type hierarchy for user-defined type '{type}': marshalling code for it will not be generated.");
    }

    /// <summary>
    /// A '[GeneratedComInterface]' type is attempting to implement a reserved interface (e.g. 'IUnknown').
    /// </summary>
    public static WellKnownInteropException GeneratedComInterfaceReservedGuidError(TypeDefinition interfaceType, TypeDefinition type, Guid iid, string interfaceName)
    {
        return Exception(63, $"The '[GeneratedComInterface]' type '{interfaceType}' found while processing type '{type}' is using IID '{iid.ToString().ToUpperInvariant()}' (which maps to the '{interfaceName}' interface): this interface is implicitly implemented and cannot be overridden.");
    }

    /// <summary>
    /// Failed to resolve a generic type signature.
    /// </summary>
    public static WellKnownInteropWarning GenericTypeSignatureNotResolvedError(TypeSignature typeSignature, ModuleDefinition module)
    {
        return Warning(64, $"Failed to resolve the generic type signature '{typeSignature}' while processing module '{module}': marshalling code for it will not be generated.");
    }

    /// <summary>
    /// Failed to resolve an SZ array type signature.
    /// </summary>
    public static WellKnownInteropWarning SzArrayTypeSignatureNotResolvedError(TypeSignature typeSignature, ModuleDefinition module)
    {
        return Warning(65, $"Failed to resolve the SZ array type signature '{typeSignature}' while processing module '{module}': marshalling code for it will not be generated.");
    }

    /// <summary>
    /// Failed to resolve a base type for a Windows Runtime class type.
    /// </summary>
    public static WellKnownInteropWarning WindowsRuntimeClassTypeNotResolvedWarning(ITypeDefOrRef baseType, TypeDefinition classType)
    {
        return Warning(66, $"Failed to resolve the base type '{baseType}' for Windows Runtime class type '{classType}': runtime casts to the base type will not work if the type is trimmed.");
    }

    /// <summary>
    /// A parameter index was not valid in a generated interop method.
    /// </summary>
    public static WellKnownInteropException MethodRewriteSourceLocalTypeMismatchError(TypeSignature localType, TypeSignature returnType, MethodDefinition method)
    {
        return Exception(67, $"Local variable of type '{localType}' cannot be used to marshal a value of type '{returnType}' in generated interop method '{method}'.");
    }

    /// <summary>
    /// The type of a local was not valid in a generated interop method.
    /// </summary>
    public static WellKnownInteropException MethodRewriteParameterIndexNotValidError(int parameterIndex, MethodDefinition method)
    {
        return Exception(68, $"Parameter index '{parameterIndex}' was not valid for generated interop method '{method}'.");
    }

    /// <summary>
    /// The type of a parameter was not valid in a generated interop method.
    /// </summary>
    public static WellKnownInteropException MethodRewriteSourceParameterTypeMismatchError(TypeSignature parameterType, TypeSignature returnType, MethodDefinition method)
    {
        return Exception(69, $"Parameter variable of type '{parameterType}' cannot be used to marshal a value of type '{returnType}' in generated interop method '{method}'.");
    }

    /// <summary>
    /// A type doesn't have the <c>Dispose</c> method available.
    /// </summary>
    public static WellKnownInteropException MethodRewriteDisposeNotAvailableError(TypeSignature parameterType, MethodDefinition method)
    {
        return Exception(70, $"Value of type '{parameterType}' in generated interop method '{method}' cannot be disposed, as it is an unmanaged (or blittable) value type.");
    }

    /// <summary>
    /// Failed to resolve the primary Windows Runtime interface for a user-defined type.
    /// </summary>
    public static WellKnownInteropException PrimaryWindowsRuntimeInterfaceNotFoundError(TypeSignature userDefinedType)
    {
        return Exception(71, $"Failed to resolve primary Windows Runtime interface for user-defined type '{userDefinedType}'.");
    }

    /// <summary>
    /// Registering a tracked method definition multiple times for the same signature.
    /// </summary>
    /// <param name="typeSignature">The type signature.</param>
    /// <param name="key">The key.</param>
    public static WellKnownInteropException AddingDuplicateTrackedMethodDefinition(TypeSignature typeSignature, string key)
    {
        return Exception(72, $"Duplicate tracked method definition for signature '{typeSignature}' and key '{key}'.");
    }

    /// <summary>
    /// Failed to find a tracked method definition.
    /// </summary>
    /// <param name="typeSignature">The type signature.</param>
    /// <param name="key">The key.</param>
    public static WellKnownInteropException TrackedMethodDefinitionLookupError(TypeSignature typeSignature, string key)
    {
        return Exception(73, $"Failed to find a tracked method definition for signature '{typeSignature}' and key '{key}'.");
    }

    /// <summary>
    /// Failed to define metadata assembly attributes.
    /// </summary>
    public static WellKnownInteropException EmitMetadataAssemblyAttributesError(Exception exception)
    {
        return Exception(74, "Failed to emit the metadata assembly attributes for the interop assembly.", exception);
    }

    /// <summary>
    /// A generated interop method is missing an IL method body.
    /// </summary>
    public static WellKnownInteropException MethodFixupMissingBodyError(MethodDefinition method)
    {
        return Exception(75, $"Generated interop method '{method}' is missing an IL method body, fixups cannot be applied.");
    }

    /// <summary>
    /// A generated interop method has invalid exception handler labels.
    /// </summary>
    public static WellKnownInteropException MethodFixupInvalidExceptionHandlerLabels(MethodDefinition method)
    {
        return Exception(76, $"Generated interop method '{method}' has invalid exception handler labels, fixups cannot be applied.");
    }

    /// <summary>
    /// A generated interop method has invalid branch instruction labels.
    /// </summary>
    public static WellKnownInteropException MethodFixupInvalidBranchInstructionLabels(MethodDefinition method)
    {
        return Exception(77, $"Generated interop method '{method}' has invalid branch instruction labels, fixups cannot be applied.");
    }

    /// <summary>
    /// Failed to apply a fixup to a marshalling method.
    /// </summary>
    public static WellKnownInteropException MethodFixupError(InteropMethodFixup fixup, MethodDefinition method, Exception exception)
    {
        return Exception(78, $"Failed to apply fixup '{fixup.GetType()}' to method '{method}'.", exception);
    }

    /// <summary>
    /// Failed to generate the runtime class name of some Windows Runtime type.
    /// </summary>
    public static Exception RuntimeClassNameGenerationError(TypeSignature type)
    {
        return Exception(79, $"Failed to generate the runtime class name for type '{type}'.");
    }

    /// <summary>
    /// Failed to resolve the target <c>ComWrappersMarshallerAttribute</c> type for a non-projected Windows Runtime type.
    /// </summary>
    public static WellKnownInteropException NonProjectedTypeComWrappersMarshallerAttributeTypeResolveError(TypeReference attributeType, string nativeType)
    {
        return Exception(80, $"Failed to resolve the 'ComWrappersMarshallerAttribute' type '{attributeType}' for a non-projected Windows Runtime type '{nativeType}'.");
    }

    /// <summary>
    /// Failed to resolve the associated "Methods" type for a custom-mapped type.
    /// </summary>
    public static WellKnownInteropException CustomMappedTypeMethodsTypeResolveError(TypeSignature type)
    {
        return Exception(81, $"Failed to resolve the associated 'Methods' type for the custom-mapped type '{type}'.");
    }

    /// <summary>
    /// Failed to resolve the element type for an array type.
    /// </summary>
    public static WellKnownInteropWarning ArrayTypeElementTypeNotFullyResolvedWarning(ITypeDefOrRef baseType, TypeDefinition type)
    {
        return Warning(82, $"Failed to resolve the base type '{baseType}' in the type hierarchy for element type '{type}': marshalling code for corresponding SZ arrays will not be generated.");
    }

    /// <summary>
    /// Multiple '[GeneratedComInterface]' types are using the same IID.
    /// </summary>
    public static WellKnownInteropWarning GeneratedComInterfaceDuplicateIidWarning(TypeDefinition interfaceType, TypeDefinition type, Guid iid)
    {
        return Warning(83, $"Failed to validate the '[GeneratedComInterface]' type '{interfaceType}' on type '{type}', because the type already implements another interface with IID '{iid.ToString().ToUpperInvariant()}': the interface will not be included in the set of available COM interface entries.");
    }

    /// <summary>
    /// An exposed type exceeded the maximum limit of interfaces.
    /// </summary>
    public static WellKnownInteropWarning ExceededNumberOfExposedWindowsRuntimeInterfaceTypesWarning(TypeSignature type)
    {
        return Warning(84, $"Exposed type '{type}' exceeded the maximum limit of 128 projected Windows Runtime interfaces implemented: all exceeding interfaces will not be included in the set of available COM interface entries.");
    }

    /// <summary>
    /// Creates a new exception with the specified id and message.
    /// </summary>
    /// <param name="id">The exception id.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <returns>The resulting exception.</returns>
    private static WellKnownInteropException Exception(int id, string message, Exception? innerException = null)
    {
        return new($"{ErrorPrefix}{id:0000}", message, innerException);
    }

    /// <summary>
    /// Creates a new warning with the specified id and message.
    /// </summary>
    /// <param name="id">The warning id.</param>
    /// <param name="message">The warning message.</param>
    /// <returns>The resulting warning.</returns>
    private static WellKnownInteropWarning Warning(int id, string message)
    {
        return new($"{ErrorPrefix}{id:0000}", message);
    }
}