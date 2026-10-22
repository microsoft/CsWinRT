# CSWINRTINTEROPGEN0112: Generated IID getter name is not unique

Multiple interface IIDs produced the same getter name in the generated `ABI.InterfaceIIDs` type. The interop generator rejects the collision rather than emitting ambiguous metadata or selecting another interface's IID, which could cause runtime interop failures.

Generated names combine assembly identifiers, type names and generic arguments, and replace certain characters for compatibility with tooling. Distinct metadata names can therefore collide; for example, a `.` and an existing `-` both become `-` in the generated name.

The error identifies the conflicting generated getter name, and the enclosing diagnostic provides type-generation context. Rename the conflicting input types or assemblies, or report the collision to the component author if those names are outside your control. This is an error even when warnings are not treated as errors.
