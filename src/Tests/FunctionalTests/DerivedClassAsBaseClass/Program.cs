using test_component_base;
using test_component_derived.Nested;

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