using System;
using Windows.Data.Json;

JsonObject json = JsonObject.Parse("""{ "a": 42 }""");

string stringified = json.Stringify();

return stringified.Contains("42") ? 0 : 1;
