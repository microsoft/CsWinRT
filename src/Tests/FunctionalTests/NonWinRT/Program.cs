using System;
using System.Collections.Generic;

// Test calling .NET functions which are WinRT custom mappings
// to ensure we can build them without a reference to the Windows SDK projection.
// This is to ensure we do our best to only generate for WinRT scenarios.

char[] charValues = new[] {'a', 'b', 'c'};
_= string.Join(',', charValues);

_ = bool.Parse("true");

string[] strValues = new[] { "a", "b", "c" };
_= string.Concat(strValues);

List<string> strList = new List<string>(){ "a", "b", "c" };
_= string.Concat(strList);

// Test a scenario with debug builds where we can't tell for sure
// whether it is not a WinRT scenario so we will generate. But if
// unsafe is disabled, we will not actually generate allowing the
// project to still build.
// This is scoped to debug just so we can test both behaviors.
#if DEBUG
CustomConcat(strList);

static void CustomConcat(IEnumerable<string> strList)
{
    _ = string.Concat(strList);
}
#endif