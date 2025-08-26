// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet.Signatures;

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
    public static Exception RuntimeClassNameTooLong(string name)
    {
        return Exception(1, $"The runtime class name '{name}' is too long. The maximum length is {ushort.MaxValue} characters.");
    }

    /// <summary>
    /// There are too many runtime class names to build the lookup.
    /// </summary>
    public static Exception RuntimeClassNameLookupSizeLimitExceeded()
    {
        return Exception(2, "The runtime class name lookup size limit was exceeded.");
    }

    /// <summary>
    /// The assembly module was not found.
    /// </summary>
    public static Exception AssemblyModuleNotFound()
    {
        return Exception(3, "The assembly module was not found (this might mean that its path was not valid, or that it failed to load).");
    }

    /// <summary>
    /// The WinRT runtime module was not found.
    /// </summary>
    public static Exception WinRTModuleNotFound()
    {
        return Exception(4, "The WinRT runtime module was not found (this might mean that its path was not valid, or that it failed to load).");
    }

    /// <summary>
    /// Exception when emitting the 'Values' RVA field.
    /// </summary>
    public static Exception TypeHierarchyValuesRvaError(Exception exception)
    {
        return Exception(5, "Failed to generate data for the 'Values' RVA field for the type hierarchy lookup.", exception);
    }

    /// <summary>
    /// Exception when emitting the 'Keys' RVA field.
    /// </summary>
    public static Exception TypeHierarchyKeysRvaError(Exception exception)
    {
        return Exception(6, "Failed to generate data for the 'Keys' RVA field for the type hierarchy lookup.", exception);
    }

    /// <summary>
    /// Exception when emitting the 'Buckets' RVA field.
    /// </summary>
    public static Exception TypeHierarchyBucketsRvaError(Exception exception)
    {
        return Exception(7, "Failed to generate data for the 'Buckets' RVA field for the type hierarchy lookup.", exception);
    }

    /// <summary>
    /// Exception when emitting the type hierarchy implementation.
    /// </summary>
    public static Exception TypeHierarchyImplementationError(Exception exception)
    {
        return Exception(8, "Failed to generate the method implementations for the type hierarchy lookup.", exception);
    }

    /// <summary>
    /// Exception when emitting the interop .dll to disk.
    /// </summary>
    public static Exception EmitDllError(Exception exception)
    {
        return Exception(9, "Failed to emit the interop .dll to disk.", exception);
    }

    /// <summary>
    /// The state was changed after making it readonly.
    /// </summary>
    public static Exception StateChangeAfterMakeReadOnly()
    {
        return Exception(10, "An attempt was made to mutate the generator state after it was made readonly (in the emit phase).");
    }

    /// <summary>
    /// Failed to generate marshalling code for a delegate type.
    /// </summary>
    public static Exception DelegateTypeCodeGenerationError(string? delegateType, Exception exception)
    {
        return Exception(11, $"Failed to generate marshalling code for delegate type '{delegateType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
    /// </summary>
    public static Exception KeyValuePairTypeCodeGenerationError(string? keyValuePairType, Exception exception)
    {
        return Exception(12, $"Failed to generate marshalling code for 'KeyValuePair<,>' type '{keyValuePairType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an implementation detail type.
    /// </summary>
    public static Exception ImplementationDetailTypeCodeGenerationError(Exception exception)
    {
        return Exception(13, $"Failed to generate marshalling code for some implementation detail type.", exception);
    }

    /// <summary>
    /// Failed to discover type hierarchy types.
    /// </summary>
    public static Exception DiscoverTypeHierarchyTypesError(string? name, Exception exception)
    {
        return Exception(14, $"Failed to discover type hierarchy types for module '{name}'.", exception);
    }

    /// <summary>
    /// Failed to discover generic type instantiations.
    /// </summary>
    public static Exception DiscoverGenericTypeInstantiationsError(string? name, Exception exception)
    {
        return Exception(15, $"Failed to discover generic type instantiations for module '{name}'.", exception);
    }

    /// <summary>
    /// Failed to discover generic type instantiations.
    /// </summary>
    public static Exception LoadAndDiscoverModulesLoopDidNotComplete()
    {
        return Exception(16, "Failed to complete processing all input modules.");
    }

    /// <summary>
    /// Failed to discover generic type instantiations.
    /// </summary>
    public static Exception LoadAndDiscoverModulesLoopError(Exception exception)
    {
        return Exception(17, "Failed to load and process all input modules.", exception);
    }

    /// <summary>
    /// Failed to define the interop assembly.
    /// </summary>
    public static Exception DefineInteropAssemblyError(Exception exception)
    {
        return Exception(18, "Failed to define the interop module and assembly.", exception);
    }

    /// <summary>
    /// Failed to define the <c>[IgnoreAccessChecksTo]</c> attributes
    /// </summary>
    public static Exception DefineIgnoreAccessChecksToAttributesError(Exception exception)
    {
        return Exception(19, "Failed to generate the '[IgnoreAccessChecksTo]' attribute definition and annotations.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <see cref="System.Collections.Generic.IEnumerator{T}"/> type.
    /// </summary>
    public static Exception IEnumerator1TypeCodeGenerationError(TypeSignature enumeratorType, Exception exception)
    {
        return Exception(20, $"Failed to generate marshalling code for 'IEnumerator<T>' type '{enumeratorType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <see cref="System.Collections.Generic.IEnumerable{T}"/> type.
    /// </summary>
    public static Exception IEnumerable1TypeCodeGenerationError(TypeSignature enumerableType, Exception exception)
    {
        return Exception(21, $"Failed to generate marshalling code for 'IEnumerable<T>' type '{enumerableType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <see cref="System.Collections.Generic.IReadOnlyList{T}"/> type.
    /// </summary>
    public static Exception IReadOnlyList1TypeCodeGenerationError(TypeSignature readOnlyListType, Exception exception)
    {
        return Exception(22, $"Failed to generate marshalling code for 'IReadOnlyList<T>' type '{readOnlyListType}'.", exception);
    }

    /// <summary>
    /// Registering a tracked type definition multiple times for the same signature.
    /// </summary>
    /// <param name="typeSignature">The type signature.</param>
    /// <param name="key">The key.</param>
    public static Exception AddingDuplicateTrackedTypeDefinition(TypeSignature typeSignature, string key)
    {
        return Exception(23, $"Duplicate tracked type definition for signature '{typeSignature}' and key '{key}'.");
    }

    /// <summary>
    /// Failed to find a tracked type definition.
    /// </summary>
    /// <param name="typeSignature">The type signature.</param>
    /// <param name="key">The key.</param>
    public static Exception TrackedTypeDefinitionLookupError(TypeSignature typeSignature, string key)
    {
        return Exception(24, $"Failed to find a tracked type definition for signature '{typeSignature}' and key '{key}'.");
    }

    /// <summary>
    /// Failed to generate marshalling code for an <see cref="System.Collections.Generic.IList{T}"/> type.
    /// </summary>
    public static Exception IList1TypeCodeGenerationError(TypeSignature listType, Exception exception)
    {
        return Exception(25, $"Failed to generate marshalling code for 'IList<T>' type '{listType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> type.
    /// </summary>
    public static Exception IReadOnlyDictionary2TypeCodeGenerationError(TypeSignature readOnlyDictionaryType, Exception exception)
    {
        return Exception(26, $"Failed to generate marshalling code for 'IReadOnlyDictionary<TKey, TValue>' type '{readOnlyDictionaryType}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for an <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> type.
    /// </summary>
    public static Exception IDictionary2TypeCodeGenerationError(TypeSignature dictionaryType, Exception exception)
    {
        return Exception(27, $"Failed to generate marshalling code for 'IDictionary<TKey, TValue>' type '{dictionaryType}'.", exception);
    }

    /// <summary>
    /// Some exception was thrown when trying to read the response file.
    /// </summary>
    public static Exception ResponseFileReadError(Exception exception)
    {
        return Exception(28, "Failed to read the response file to run 'cswinrtgen'.", exception);
    }

    /// <summary>
    /// Failed to parse an argument from the response file.
    /// </summary>
    public static Exception ResponseFileArgumentParsingError(string argumentName, Exception? exception = null)
    {
        return Exception(29, $"Failed to parse argument '{argumentName}' from response file.", exception);
    }

    /// <summary>
    /// The input response file is malformed.
    /// </summary>
    public static Exception MalformedResponseFile()
    {
        return Exception(30, "The response file is malformed and contains invalid content.");
    }

    /// <summary>
    /// The debug repro directory does not exist.
    /// </summary>
    public static Exception DebugReproDirectoryDoesNotExist(string path)
    {
        return Exception(31, $"The debug repro directory '{path}' does not exist.");
    }

    /// <summary>
    /// The debug repro directory does not exist.
    /// </summary>
    public static Exception WinRTRuntimeAssemblyVersionMismatch(
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
    public static Exception DiscoverSzArrayTypesError(string? name, Exception exception)
    {
        return Exception(33, $"Failed to discover SZ array type for module '{name}'.", exception);
    }

    /// <summary>
    /// Failed to generate marshalling code for a SZ array type.
    /// </summary>
    public static Exception SzArrayTypeCodeGenerationError(string? arrayType, Exception exception)
    {
        return Exception(34, $"Failed to generate marshalling code for SZ array type '{arrayType}'.", exception);
    }

    /// <summary>
    /// Failed to discover SZ array types.
    /// </summary>
    public static Exception WinRTRuntimeDllVersion2References(IEnumerable<string> names)
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
    public static Exception TypeHierarchyNoDiscoveredKeyValuePairs()
    {
        return Exception(36,
            "No type hierarchy key-value pairs were discovered across referenced assemblies. This should never happen, and it indicates that either " +
            "the referenced assemblies were not valid (e.g. incorrectly mixing CsWinRT 2.x and 3.0 projection assemblies), or that some other " +
            "unexpected error occurred during the discovery phase, which wasn't correctly identified earlier in the generation process.");
    }

    /// <summary>
    /// Creates a new exception with the specified id and message.
    /// </summary>
    /// <param name="id">The exception id.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <returns>The resulting exception.</returns>
    private static Exception Exception(int id, string message, Exception? innerException = null)
    {
        return new WellKnownInteropException($"{ErrorPrefix}{id:0000}", message, innerException);
    }
}

