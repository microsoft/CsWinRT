using System;
using Windows.Foundation;


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

[WinRT.DynamicWindowsRuntimeCast(typeof(TestComponentCSharp.EnumValue))]
void CheckBoxedEnum()
{
    var enumVal = TestComponentCSharp.Class.BoxedEnum;
    if (enumVal is TestComponentCSharp.EnumValue val && val == TestComponentCSharp.EnumValue.Two)
    {
        result += 1;
    }
}