#if EXCLUSIVE_TO_PUBLIC_INTERFACES
using Windows.Data.Json;
using Windows.UI.Xaml;

internal sealed class ManagedExclusives : IJsonObjectWithDefaultValues, IFrameworkElementProtected7
{
    private readonly JsonValue value = JsonValue.CreateNumberValue(42);

    public int ViewportInvalidations { get; private set; }

    public void InvalidateViewport() => ViewportInvalidations++;

    public JsonValueType ValueType => value.ValueType;

    public JsonArray GetArray() => value.GetArray();

    public bool GetBoolean() => value.GetBoolean();

    public double GetNumber() => value.GetNumber();

    public JsonObject GetObject() => value.GetObject();

    public string GetString() => value.GetString();

    public string Stringify() => value.Stringify();

    public JsonValue GetNamedValue(string name, JsonValue defaultValue) => defaultValue;

    public JsonObject GetNamedObject(string name, JsonObject defaultValue) => defaultValue;

    public JsonArray GetNamedArray(string name, JsonArray defaultValue) => defaultValue;

    public string GetNamedString(string name, string defaultValue) => defaultValue;

    public double GetNamedNumber(string name, double defaultValue) => defaultValue;

    public bool GetNamedBoolean(string name, bool defaultValue) => defaultValue;
}
#endif
