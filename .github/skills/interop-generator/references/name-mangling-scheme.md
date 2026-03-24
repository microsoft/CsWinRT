# Interop generator name mangling scheme

This document specifies the name mangling scheme used by the interop generator (`cswinrtinteropgen`) to produce unique type names in `WinRT.Interop.dll`. The scheme is designed to ensure that generated type names are unique, compact, and descriptive. It uses a combination of assembly names, namespaces, and type names to construct the final mangled name. The scheme handles primitive types, user-defined types, and generic types, including nested generics.

The implementation is in `InteropUtf8NameFactory` (`src/WinRT.Interop.Generator/Factories/InteropUtf8NameFactory.cs`).

## Mangled namespace

The **mangled namespace** for a given type is defined as follows:

1. **No namespace**: if the input type has no namespace, the generated namespace defaults to `ABI`.
2. **Existing namespace**: if the input type has a namespace, the generated namespace prepends `ABI.` to the original namespace.
3. **Nested types**: if the input type is nested, the namespace is derived from the outermost declaring type (i.e. the top-level enclosing type), using the same rules above.

## Mangled type name

The **mangled type name** for a given type is defined as follows:

1. **Primitive types**: well-known primitive types (e.g., `int`, `string`) are mapped to their corresponding identifiers.
2. **User-defined types**: the type name is prefixed with the assembly name (or a compact identifier for well-known assemblies) within angle brackets (i.e. `<>`), and suffixed with the type name. The namespace is not included, as it matches the containing namespace for the generated type (without the `ABI[.]` prefix).
3. **Generic types**: their type arguments are enclosed in angle brackets, right after the type name. Nested generics are recursively processed, and type arguments are separated by a pipe (i.e. `|`). Each type argument also has its name prefixed by the containing namespace.
4. **Array types**: single-dimensional arrays (SZ arrays) are represented by wrapping the mangled name of the element type in angle brackets, and appending `Array` (i.e., `<NAME>Array`) as a suffix. The element type uses the same mangling rules as any other type (primitive, user-defined, generic, or nested array).
5. **Nested types**: when a type is nested, the declaring type chain is concatenated using `+` separators (e.g. `Outer+Inner`). The namespace is still omitted (as with top-level user-defined types) at the root depth, and only the chain of nested type names is emitted after the assembly segment. If a declaring (or nested) type is generic, its (unmangled) metadata name, including its arity marker (`` ` ``) is first transformed by replacing the backtick with `'` (apostrophe) like for any other type name, and then used in the chain. For nested types appearing inside generic type arguments (i.e. at non-root depth), their fully-qualified name (namespace plus nested path) is emitted as a single component before character substitutions are applied.

All `.` characters in the final mangled name are replaced with `-` characters. Additionally, all `` ` `` characters (backticks) in the final mangled name are replaced with `'` characters (apostrophes). These substitutions apply to the **entire** mangled name, including assembly identifiers inside angle brackets. For example, an assembly named `System.Numerics.Vectors` becomes `System-Numerics-Vectors` in the final output.

> [!NOTE]
> The `.` character replacement is not strictly required, as the `.` character is a valid character for an identifier as per ECMA-335. However, using that character can be inconvenient when using reflection APIs to inspect such types, as it makes it not possible to easily distinguish the namespace from the actual type name. So to account for this, we just do this substitution, given the final length of the mangled name remains the same anyway when doing so.
> 
> The `` ` `` character replacement is done to avoid issues with tooling, such as ILSpy, which assume that all types with `` ` `` in their name are generic types. The generated types for marshalling generic types are not themselves generic, which causes these tools to incorrectly interpret the type metadata.

## Well-known assemblies

These are the well-known assemblies and their compact identifiers:
- `System.Runtime`: `#corlib`
- `Microsoft.Windows.SDK.NET` or `Microsoft.Windows.UI.Xaml`: `#Windows`
- `WinRT.Runtime`: `#CsWinRT`

Compact identifiers are prefixed with `#` to distinguish them from user-defined assembly names.

For types not belonging to any well-known assembly, the implementation also checks for a `[WindowsRuntimeMetadata]` attribute on the resolved type definition. If the attribute is present, the Windows Runtime metadata name from the attribute is used as the assembly identifier instead of the actual assembly name. This allows types carrying WinRT metadata to be identified by their canonical Windows Runtime name rather than the .NET assembly they happen to live in. If the attribute is not present, the raw assembly name is used as-is.

> [!NOTE]
> Not all BCL types live in `System.Runtime`. For example, the `System.Numerics` types (`Matrix3x2`, `Matrix4x4`, `Plane`, `Quaternion`, `Vector2`, `Vector3`, `Vector4`) are in the `System.Numerics.Vectors` assembly, so their assembly identifier is `System-Numerics-Vectors` (not `#corlib`) after the `.` → `-` substitution. The assembly used is always the one from the type's actual metadata scope, not the namespace.

## Examples

**Primitive type**

- Type: `System.Int32`
- Mangled name: `ABI.System.int`

**User-defined type**

- Type: `MyNamespace.MyType` (from assembly `MyAssembly`)
- Mangled name: `ABI.MyNamespace.<MyAssembly>MyType`

**Nested user-defined type**

- Type: `MyNamespace.Outer+Inner` (from assembly `MyAssembly`)
- Mangled name: `ABI.MyNamespace.<MyAssembly>Outer+Inner`

**Generic type**

- Type: `System.Collections.Generic.IEnumerable<string>`
- Mangled name: `ABI.System.Collections.Generic.<#corlib>IEnumerable'1<string>`

**Nested generic type**

- Type: `System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, MyNamespace.MyType>` (`MyType` is from assembly `MyAssembly`)
- Mangled name: `ABI.System.Collections.Generic.<#corlib>ICollection'1<<#corlib>System-Collections-Generic-KeyValuePair'2<string|<MyAssembly>MyNamespace-MyType>>`

**Generic nested type**

- Type: `MyNamespace.Outer<int>.Inner<string>` (assemblies `MyAssembly` for all types)
- Mangled name: `ABI.MyNamespace.<MyAssembly>Outer'1<int>+Inner'1<string>`

**Array type (primitive element)**

- Type: `System.Int32[]`
- Mangled name: `ABI.System.<int>Array`

**Array type (user-defined element)**

- Type: `MyNamespace.MyType[]` (from assembly `MyAssembly`)
- Mangled name: `ABI.MyNamespace.<<MyAssembly>MyType>Array`

**Array type (generic element)**

- Type: `System.Collections.Generic.List<string>[]`
- Mangled name: `ABI.System.Collections.Generic.<<#corlib>List'1<string>>Array`

**Array type (nested array)**

- Type: `System.Int32[][]`
- Mangled name: `ABI.System.<<int>Array>Array`

## ANTLR4 name mangling rules

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
              | 'nint'
              | 'nuint'
              | 'string'
              | 'object';

// User-defined types
userDefinedType : '<' assemblyName '>' typePath;

// Generic types
genericType : '<' assemblyName '>' typePath '<' typeArgument ( '|' typeArgument )* '>';
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
             | identifier;

// After substitutions, identifiers may contain '-' (from namespaces), '+' (nested type separators,
// as many as needed), and apostrophes (which act as generic arity markers, replacing backticks).
typePath : simpleIdentifier ( '+' simpleIdentifier )*;
simpleIdentifier : [a-zA-Z_][a-zA-Z0-9_'-]*;
```
