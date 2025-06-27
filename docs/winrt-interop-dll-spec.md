# `WinRT.Interop.dll` public API specification

## Overview

The `WinRT.Interop.dll` is a dynamically generated assembly providing additional marshalling code for applications and libraries that require interop with WinRT APIs. This assembly is generated on the fly after build, by an MSBuild task bundled with CsWinRT. This allows the interop .dll to leverage from global-program-view info, as it can see all types in the entire application domain. This enables several performance optimizations (eg. all vtables can be pre-initialized), security features (all vtables will be in readonly data segments in the PE file), and usability improvements (no need to mark types as being marshalled, things will "just work").

This document provides the specification for the public API of this interop .dll, allowing other projects to rely on these generated members being present and having the detailed format and signature. All generated code not documented here is considered an implementation detail, which can (and will) change at any time, without following semantic versioning.

> [!NOTE]
> The interop .dll cannot be referenced by any other assembly (as it is produced at the very end of the build process), so all upstream assemblies that need to invoke APIs from it must do so by using [`[UnsafeAccessor]`](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.unsafeaccessorattribute) and `[UnsafeAccessorType]`. All generated projections and code within `WinRT.Runtime.dll` will use this technique to access all of these interop APIs. User code should never try to or need to do this manually: all of this code exists solely to support the WinRT marshalling infrastructure behind the scenes.

## Build infrastructure

The `WinRT.Interop.dll` assembly is produced by `cswinrtgen`, which is the interop assembly generator bundled with CsWinRT and the Windows SDK projections. It is a native binary (compiled with Native AOT) that is invoked during build and produces this interop assembly, specialized for the library or application being compiled. The tool is invoked by the [`RunCsWinRTGenerator`](https://github.com/dotnet/sdk/blob/2ab975ef4c560f9383e897d9af4e9784798b7576/src/Tasks/Microsoft.NET.Build.Tasks/RunCsWinRTGenerator.cs) task, which is in the .NET SDK. This task is invoked by the [`_RunCsWinRTGenerator`](https://github.com/dotnet/sdk/blob/2ab975ef4c560f9383e897d9af4e9784798b7576/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.Windows.targets#L275) target, which is defined in `Microsoft.NET.Windows.targets`, also in the .NET SDK.

Note that `cswinrtgen` must be versioned with the `WinRT.Runtime.dll` assembly it was compiled for. Not doing so will result in undefined behavior (eg. failures to run the tool, or runtime crashes). To ensure the assembly versions of these two assemblies always match, the .NET SDK has logic that will select the right version of `cswinrtgen` after all reference assemblies are resolved, which allows dealing with multiple transitive versions with different assembly versions. That is, if a project doesn't have a reference to CsWinRT, then `cswinrtgen` will be loaded from the Windows SDK projections targeting pack. If CsWinRT is referenced (directly or transitively), then `cswinrtgen` will be loaded from that package, but only if the `WinRT.Runtime.dll` binary from that package has a higher version than the one in the Windows SDK projections package being referenced. This correctly handles cases where a dependent project might have a reference to an outdated CsWinRT package.

The .NET SDK takes care of all of this work. As a result, `cswinrtgen` can always rely on its version matching the version of `WinRT.Runtime.dll`. This is critical to allow using "implementation details only" APIs in `WinRT.Runtime.dll`: APIs which are public, hidden, and marked as `[Obsolete]`, which are exclusively meant to be consumed by generated code produced by `cswinrtgen`. These APIs might change at any time without following semantic versioning for CsWinRT. For instance, these are crucial to support marshalling generic Windows Runtime collection interfaces, and the code in `WinRT.Interop.dll` makes heavy use of them in this scenario.

## Name mangling scheme

The name mangling scheme for interop types is designed to ensure that generated type names are unique, compact, and descriptive. It uses a combination of assembly names, namespaces, and type names to construct the final mangled name. The scheme handles primitive types, user-defined types, and generic types, including nested generics.

The **mangled namespace** for a given type is defined as follows:

1. **No namespace**: if the input type has no namespace, the generated namespace defaults to `ABI`.
2. **Existing namespace**: if the input type has a namespace, the generated namespace prepends `ABI.` to the original namespace.

The **mangled type name** for a given type is defined as follows:

1. **Primitive types**: well-known primitive types (e.g., `int`, `string`) are mapped to their corresponding identifiers.
2. **User-defined types**: the type name is prefixed with the assembly name (or a compact identifier for well-known assemblies) within angle brackets (i.e. `<>`), and suffixed with the type name. The namespace is not included, as it matches the containing namespace for the generated type (without the `ABI[.]` prefix).
3. **Generic types**: their type arguments are enclosed in angle brackets, right after the type name. Nested generics are recursively processed, and type arguments are separated by a pipe (i.e. `|`). Each type argument also has its name prefixed by the containing namespace.
4. **Array types**: single-dimensional arrays (SZ arrays) are represented by wrapping the mangled name of the element type in angle brackets, and appending `Array` (i.e., `<NAME>Array`) as a suffix. The element type uses the same mangling rules as any other type (primitive, user-defined, generic, or nested array).

All `.` characters in the final mangled name are replaced with `-` characters.

> [!NOTE]
> This is not strictly required, as the `.` character is a valid character for an identifier as per ECMA-335. However, using that character can be inconvenient when using reflection APIs to inspect such types, as it makes it not possible to easily distinguish the namespace from the actual type name. So to account for this, we just do this substitution, given the final length of the mangled name remains the same anyway when doing so.

These are the well-known assemblies and their compact identifiers:
- `System.Runtime`: `#corlib`
- `Microsoft.Windows.SDK.NET` or `Microsoft.Windows.UI.Xaml`: `#Windows`
- `WinRT.Runtime`: `#CsWinRT`
- `Microsoft.UI.Xaml.Projection`: `#WinUI2`
- `Microsoft.Graphics.Canvas.Interop`: `#Win2D`

Compact identifiers are prefixed with `#` to distinguish them from user-defined assembly names.

### Examples

**Primitive type**

- Type: `System.Int32`
- Mangled name: `ABI.System.int`

**User-defined type**

- Type: `MyNamespace.MyType` (from assembly `MyAssembly`)
- Mangled name: `ABI.MyNamespace.<MyAssembly>MyType`

**Generic type**

- Type: `System.Collections.Generic.IEnumerable<string>`
- Mangled name: ``ABI.System.Collections.Generic.<#corlib>IEnumerable`1<string>"``

**Nested generic type**

- Type: `System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, MyNamespace.MyType>` (`MyType` is from assembly `MyAssembly`)
- Mangled name: ``ABI.System.Collections.Generic.<#corlib>ICollection`1<<#corlib>System-Collections-Generic-KeyValuePair`2<string|<MyAssembly>MyNamespace-MyType>>``

**Array type (primitive element)**

- Type: `System.Int32[]`
- Mangled name: `ABI.System.<int>Array`

**Array type (user-defined element)**

- Type: `MyNamespace.MyType[]` (from assembly `MyAssembly`)
- Mangled name: `ABI.MyNamespace.<<MyAssembly>MyType>Array`

**Array type (generic element)**

- Type: `System.Collections.Generic.List<string>[]`
- Mangled name: ``ABI.System.Collections.Generic.<<#corlib>List`1<string>>Array``

**Array type (nested array)**

- Type: `System.Int32[][]`
- Mangled name: `ABI.System.<<int>Array>Array`

### ANTLR4 name mangling rules

Below is the full specification of the name mangling scheme using ANTLR4 syntax:

```antlr
grammar NameMangling;

// Entry point for a mangled name
mangledName : namespace '.' mangledTypeName EOF;

// Namespace rules
namespace : 'ABI' ('.' identifier)*;

// Mangled type name rules
mangledTypeName : primitiveType
                | userDefinedType
                | genericType
                | arrayType;

// Primitive types
primitiveType : 'bool'
              | 'char'
              | 'sbyte'
              | 'byte'
              | 'short'
              | 'ushort'
              | 'int'
              | 'uint'
              | 'long'
              | 'ulong'
              | 'float'
              | 'double'
              | 'string'
              | 'object';

// User-defined types
userDefinedType : '<' assemblyName '>' identifier;

// Generic types
genericType : '<' assemblyName '>' identifier '<' typeArgument ( '|' typeArgument )* '>';
typeArgument : primitiveType
             | userDefinedType
             | genericType
             | arrayType;

// Array types
arrayType : '<' mangledTypeName '>' 'Array';

// Assembly name rules
assemblyName : '#corlib'
             | '#Windows'
             | '#CsWinRT'
             | '#WinUI2'
             | '#Win2D'
             | identifier;

// Identifier rules
identifier : [a-zA-Z_][a-zA-Z0-9_]*;
```