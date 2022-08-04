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

return result == 16 ? 100 : 101;