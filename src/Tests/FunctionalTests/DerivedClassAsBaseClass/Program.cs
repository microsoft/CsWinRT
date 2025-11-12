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

var hierarchyDAsHierarchyC = HierarchyC.CreateDerivedHierarchyD();
if (hierarchyDAsHierarchyC.HierarchyB_Method() != "HierarchyC.HierarchyB_Method" ||
       hierarchyDAsHierarchyC.HierarchyA_Method() != "HierarchyB.HierarchyA_Method")
{
    return 101;
}

var hierarchyDAsHierarchyCCast = (HierarchyC) HierarchyC.CreateDerivedHierarchyDAsObject();
if (hierarchyDAsHierarchyCCast.HierarchyB_Method() != "HierarchyC.HierarchyB_Method" ||
       hierarchyDAsHierarchyCCast.HierarchyA_Method() != "HierarchyB.HierarchyA_Method")
{
    return 101;
}

return 100;