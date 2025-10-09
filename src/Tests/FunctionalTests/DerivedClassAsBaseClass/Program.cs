using System;
using test_component_base;
using test_component_derived.Nested;
using WinRT;

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

// Test RCW being constructed from weak reference via rehydration
var hierarchyEAsHierarchyB = HierarchyC.CreateNonProjectedDerivedHierarchyEAsHierarchyB();
WeakReference<HierarchyB> weakRefHierarchyB = new WeakReference<HierarchyB>(hierarchyEAsHierarchyB);

// Hold native object alive while letting go of RCW
var ptr = MarshalInspectable<object>.FromManaged(hierarchyEAsHierarchyB);
hierarchyEAsHierarchyB = null;

for (int i = 0; i < 4; i++)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
}

if (weakRefHierarchyB.TryGetTarget(out var hierarchyE))
{
    if (hierarchyE.HierarchyB_Method() != "HierarchyC.HierarchyB_Method" ||
           hierarchyE.HierarchyA_Method() != "HierarchyB.HierarchyA_Method")
    {
        return 101;
    }
}
else
{
    return 102;
}

// Test RCW being constructed from weak reference again but with a more derived static type
var hierarchyEAsHierarchyC = HierarchyC.CreateNonProjectedDerivedHierarchyEAsHierarchyC();
WeakReference<HierarchyC> weakRefHierarchyC = new WeakReference<HierarchyC>(hierarchyEAsHierarchyC);

// Hold native object alive while letting go of RCW
ptr = MarshalInspectable<object>.FromManaged(hierarchyEAsHierarchyC);
hierarchyEAsHierarchyC = null;

for (int i = 0; i < 4; i++)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
}

if (weakRefHierarchyC.TryGetTarget(out var hierarchyE2))
{
    if (hierarchyE2.HierarchyB_Method() != "HierarchyC.HierarchyB_Method" ||
           hierarchyE2.HierarchyA_Method() != "HierarchyB.HierarchyA_Method")
    {
        return 101;
    }
}
else
{
    return 102;
}

// Test inital scenario again
var hierarchyEAsHierarchyB2 = HierarchyC.CreateNonProjectedDerivedHierarchyEAsHierarchyB();
WeakReference<HierarchyB> weakRefHierarchyB2 = new WeakReference<HierarchyB>(hierarchyEAsHierarchyB2);

// Hold native object alive while letting go of RCW
ptr = MarshalInspectable<object>.FromManaged(hierarchyEAsHierarchyB2);
hierarchyEAsHierarchyB2 = null;

for (int i = 0; i < 4; i++)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
}

if (weakRefHierarchyB2.TryGetTarget(out var hierarchyE3))
{
    if (hierarchyE3.HierarchyB_Method() != "HierarchyC.HierarchyB_Method" ||
           hierarchyE3.HierarchyA_Method() != "HierarchyB.HierarchyA_Method")
    {
        return 101;
    }
}
else
{
    return 102;
}

return 100;