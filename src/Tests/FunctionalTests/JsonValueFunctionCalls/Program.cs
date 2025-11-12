using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using WindowsRuntime.InteropServices;

#pragma warning disable CSWINRT3001 // Type or member is obsolete
// TODO: This shouldn't be needed if transitive references are detected correctly.
[assembly: WindowsRuntime.WindowsRuntimeReferenceAssembly]

[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Runtime2")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("Test")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Interop")]
#pragma warning restore CSWINRT3001 // Type or member is obsolete


// Static function calls and create RCW for existing object.
IStringable[] a = new IStringable[] {
                Windows.Data.Json.JsonValue.CreateNumberValue(3),
                Windows.Data.Json.JsonValue.CreateNumberValue(4),
                Windows.Data.Json.JsonValue.CreateNumberValue(5.0)
            };

int result = 0;

// Interface function call
foreach (var str in a)
{
    result += int.Parse(str.ToString());
}

// Class function call
result += (int)(a[1] as Windows.Data.Json.JsonValue).GetNumber();

CheckBoxedEnum();

return result == 17 ? 100 : 101;

void CheckBoxedEnum()
{
    var enumVal = TestComponentCSharp.Class.BoxedEnum;
    if (enumVal is TestComponentCSharp.EnumValue val && val == TestComponentCSharp.EnumValue.Two)
    {
        result += 1;
    }
}