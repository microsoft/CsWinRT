using System;
using Authoring;
using Windows.Data.Json;

JsonObject json = JsonObject.Parse("""{ "a": 42 }""");

string stringified = json.Stringify();

new BackgroundTask(null).Run(null);

return stringified.Contains("42") && new Greeter().Greet("World") == "Hello, World!" ? 0 : 1;
