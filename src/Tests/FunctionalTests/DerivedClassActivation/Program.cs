using test_component_base;
using test_component_derived.Nested;

HierarchyB hierarchyCAsHierarchyB = new HierarchyC();
return hierarchyCAsHierarchyB.HierarchyB_Method() == "HierarchyC.HierarchyB_Method" && 
       hierarchyCAsHierarchyB.HierarchyA_Method() == "HierarchyB.HierarchyA_Method" ? 100 : 101;
