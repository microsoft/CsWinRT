using System;
using Windows.Data.Json;

// Parse a JSON object from a string literal (a static projected method that also creates
// an RCW), then round-trip it back to a string with Stringify (an instance projected
// method). This exercises the generated Windows SDK projection and interop assemblies, and
// the WinRT.Runtime ref/impl assemblies, end-to-end against the real NuGet package.
JsonObject json = JsonObject.Parse("""{ "a": 42 }""");

string stringified = json.Stringify();

Console.WriteLine($"Round-tripped JSON: {stringified}");

// A successful round-trip must preserve the original value. Returning a non-zero exit code
// (or throwing) signals failure to the smoke test runner.
return stringified.Contains("42") ? 0 : 1;
