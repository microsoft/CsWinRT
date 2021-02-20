## Adding a Unit Test

A unit test for CsWinRT Diagnostics is a string of source code stored in either `NegativeData.cs` or `PositiveData.cs`
In either file, the test should be added like the snippet below -- it makes it easier to read when debugging.

``` csharp
    private const string <TestName> = @"
// using statements, if any

namespace DiagnosticTests
{ 
  ...
}";
```
        
If the scenario the test covers is positive (should generate a WinMD) then prefix the variable name for the string like "Valid_<TestName>" instead of "<TestName>"
It's easy to tell tests apart then, as any test is negative unless it's labeled "Valid".

For negative scenarios, there should be a DiagnosticDescriptor that matches the scenario
The CsWinRT DiagnosticDescriptors live in `WinRTRules.cs`, check there and if there's none add a new one. 
Any additions or deletions of rules should be followed by an update to `WinRT.SourceGenerator\AnalyzerReleases.{Un}Shipped.md`.

Once the test has been created, it will run if added to the section in `UnitTesting.cs` that corresponds to the tests validity.

The string given to `.SetName()` should try to briefly describe the scenario.
The first few words can help to classify the scenario when separated by periods; this makes it easy to group together tests of similar scenarios.
This is because the Visual Studio TestExplorer for NUnit tests uses this property to display the test
   and tries to be helpful by branching sections of tests based on "." in the name.

## Notes 
 * The namespace must be DiagnosticTests, as we have rules about namespaces and the WinMD filename that make this assumption
 * UnitTests require the "IsCsWinRTComponent" check in Generator.cs to be commented out, 
    if all tests are failing except the valid ones, make sure that this check is disabled 
    - this is a workaround until we can pass AnalyzerConfigOptions in `Helpers.cs` file