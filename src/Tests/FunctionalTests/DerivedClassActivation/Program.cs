using System.Runtime.InteropServices;
using test_component_base;
using test_component_derived.Nested;
using WindowsRuntime.InteropServices;

#pragma warning disable CSWINRT3001 // Type or member is obsolete
// TODO: This shouldn't be needed if transitive references are detected correctly.
[assembly: WindowsRuntime.WindowsRuntimeReferenceAssembly]

[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Runtime2")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("Test")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Interop")]
#pragma warning restore CSWINRT3001 // Type or member is obsolete

HierarchyB hierarchyCAsHierarchyB = new HierarchyC();
return hierarchyCAsHierarchyB.HierarchyB_Method() == "HierarchyC.HierarchyB_Method" && 
       hierarchyCAsHierarchyB.HierarchyA_Method() == "HierarchyB.HierarchyA_Method" ? 100 : 101;
