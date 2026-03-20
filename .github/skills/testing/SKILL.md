---
name: testing
description: Add or update unit tests for the CsWinRT project. Use when the user asks to add tests, write tests, update tests, check test coverage, or find existing tests for a specific feature. Helps determine the right test project and patterns to use.
---

# CsWinRT testing skill

Help add, update, or find tests in the CsWinRT 3.0 test suite. This skill understands the different test projects, their purposes, patterns, and conventions, and can guide test placement and implementation.

<investigate_before_answering>
Before adding tests, always check whether tests for the same functionality already exist in the appropriate test project. Search for relevant test class names, method names, or type names under test.
</investigate_before_answering>

## Test project overview

CsWinRT has 5 test project areas, each serving a different purpose:

### 1. Unit tests (`src/Tests/UnitTest/`)

**What it tests:** Core Windows Runtime interop functionality — parameter marshalling, collections, events, delegates, COM interop, exception handling, API compatibility, XAML template parts.

**When to add tests here:** For testing Windows Runtime projection behavior, marshalling correctness, COM interop scenarios, or runtime infrastructure from `WinRT.Runtime`.

**Project settings:**
- **Test framework:** MSTest (`[TestClass]`, `[TestMethod]`, `Assert.*`)
- **TFM:** Variable via `$(AppBuildTFMs)`, multi-platform (x86/x64)
- **Output type:** Exe, self-contained, AOT-enabled
- **Key dependencies:** MSTest.TestFramework, MSTest.Engine, MSTest.SourceGeneration, Microsoft.Windows.CsWin32, Newtonsoft.Json
- **References:** WinRT.SourceGenerator2 (as analyzer), Test/Windows/WinAppSDK projections

**Test organization:**
- Single namespace: `UnitTest`
- Test classes: `TestAPIs`, `ComGenerationTests`, `ComInteropTests`, `ExceptionTests`, `TestGuids`, `TestWinRT`, `UnitTestCSharp`, `TestWinUI`
- Shared helpers in `UnitTestHelper.cs` and `TestModuleInitializer.cs`

**Patterns:**
```csharp
[TestClass]
public class TestWinRT
{
    [TestMethod]
    public void TestSomeFeature()
    {
        // Arrange
        var instance = new SomeWinRTClass();

        // Act
        var result = instance.SomeMethod(args);

        // Assert
        Assert.AreEqual(expected, result);
    }
}
```

- Use `Assert.AreEqual()`, `Assert.IsTrue()`, `Assert.ThrowsExactly<>()`, `CollectionAssert.AreEqual()`
- Use `[Ignore]` for tests that hang or raise unrecoverable exceptions
- Conditional compilation with `#if ENABLE_WORKSTATION_TESTS` for tests needing local resources

### 2. Functional tests (`src/Tests/FunctionalTests/`)

**What it tests:** End-to-end integration scenarios under real publishing conditions (trimmed CoreCLR with ReadyToRun, or NativeAOT). Validates that projections survive trimming and AOT compilation.

**When to add tests here:** For testing a specific interop scenario that must work correctly after trimming/AOT — e.g. a new collection type, a new async pattern, a CCW scenario, or a dynamic casting scenario.

**Project structure:** Each test is a **separate standalone console application** (its own `.csproj` + `Program.cs`). There is no test framework — tests use exit codes.

**Existing test projects:**
| Project | Tests |
|---------|-------|
| `Async/` | IAsyncOperation, IAsyncOperationWithProgress, async/await, progress, cancellation |
| `CCW/` | COM Callable Wrapper marshalling, QueryInterface, IMarshal |
| `ClassActivation/` | Class instantiation, composition, mixed COM classes |
| `Collections/` | Generic collections, arrays (blittable/non-blittable), multi-module vtable lookup |
| `DerivedClassActivation/` | Derived class construction, method resolution |
| `DerivedClassAsBaseClass/` | Base class polymorphism, casting |
| `DynamicInterfaceCasting/` | Dynamic casting, interface queries, CCW interface casting |
| `Events/` | Event handlers, property changed notifications |
| `JsonValueFunctionCalls/` | Windows.Data.Json API calls, static methods, boxed enums |
| `NonWinRT/` | Compile-time validation for non-WinRT scenarios |
| `OptInMode/` | Opt-in external type marshalling |
| `Structs/` | Blittable and non-blittable struct marshalling |
| `TypeMarshaling/` | System.* type name resolution and marshalling |
| `TestImplementExclusiveTo/` | `[ImplementExclusiveTo]` attribute (library, not exe) |
| `TestLibrary/` | Shared library for multi-module vtable lookup tests |

**Shared configuration (Directory.Build.props):**
- `IsTrimmable=true`, `IsAotCompatible=true`
- `PublishAot=true` (x64, net8+)
- `TreatWarningsAsErrors=true`
- `ControlFlowGuard=Guard`
- `AllowUnsafeBlocks=true`

**Exit code convention:**
- `100` = success
- `101`–`299` = specific failure codes

**Pattern for a new functional test:**
```csharp
// Program.cs (top-level statements)
var instance = new SomeWinRTClass();
instance.Property = 42;

var result = instance.GetPropertyAsync().GetAwaiter().GetResult();
if (result != 42)
    return 101;

// ... more checks ...

return 100;
```

**To add a new functional test:**
1. Create a new folder under `FunctionalTests/` (e.g. `MyScenario/`)
2. Create `MyScenario.csproj` following the pattern of existing projects (use `$(FunctionalTestsBuildTFMs)`, reference Test/Windows projections, reference `WinRT.SourceGenerator2` as analyzer)
3. Create `Program.cs` with top-level statements, return `100` on success
4. Add the test to the CI matrix in the relevant pipeline YAML

### 3. Source generator and analyzer tests (`src/Tests/SourceGenerator2Test/`)

**What it tests:** All source generators and diagnostic analyzers in `WinRT.SourceGenerator2`.

**When to add tests here:** For testing a new or modified source generator, a new analyzer diagnostic, or changes to generated code output.

**Project settings:**
- **Test framework:** MSTest
- **TFM:** `net10.0`
- **Key dependencies:** MSTest, `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`, `Basic.Reference.Assemblies.Net100`
- **References:** WinRT.SourceGenerator2 (project reference), Windows/WinAppSDK projections, WinRT.Runtime

**Test classes:**
| Test class | What it tests |
|------------|---------------|
| `Test_CustomPropertyProviderGenerator` | `CustomPropertyProviderGenerator` source generator output |
| `Test_GeneratedCustomPropertyProviderAttributeArgumentAnalyzer` | CSWINRT2004–2008 diagnostics |
| `Test_GeneratedCustomPropertyProviderTargetTypeAnalyzer` | CSWINRT2000–2001 diagnostics |
| `Test_GeneratedCustomPropertyProviderExistingMemberImplementationAnalyzer` | CSWINRT2003 diagnostic |

**Test helpers (in `Helpers/`):**
- `CSharpGeneratorTest<TGenerator>` — runs a generator on source code and compares output
- `CSharpAnalyzerTest<TAnalyzer>` — runs an analyzer and verifies diagnostics

**Pattern for generator tests:**
```csharp
[TestMethod]
public void ValidClass_SomeScenario()
{
    const string source = """
        using WindowsRuntime.Xaml.Attributes;

        namespace MyNamespace;

        [GeneratedCustomPropertyProvider]
        public partial class MyType
        {
            public string Name { get; set; }
        }
        """;

    CSharpGeneratorTest<CustomPropertyProviderGenerator>.VerifySources(
        source,
        result: ("MyNamespace.MyType.g.cs", """
            // expected generated code...
            """));
}
```

**Pattern for analyzer tests:**
```csharp
[TestMethod]
public async Task InvalidType_Warns()
{
    const string source = """
        using WindowsRuntime.Xaml.Attributes;

        namespace MyNamespace;

        [GeneratedCustomPropertyProvider]
        public static class {|CSWINRT2000:MyType|} { }
        """;

    await CSharpAnalyzerTest<GeneratedCustomPropertyProviderTargetTypeAnalyzer>
        .VerifyAnalyzerAsync(source);
}
```

- Use `{|DIAGNOSTIC_ID:target|}` inline syntax to mark expected diagnostics
- Or use explicit `expectedDiagnostics` array with `DiagnosticResult` for complex cases
- Test naming convention: `Condition_ExpectedBehavior` (e.g. `NullPropertyName_Warns`, `ValidClass_DoesNotWarn`)

### 4. Object lifetime tests (`src/Tests/ObjectLifetimeTests/`)

**What it tests:** Reference tracking, garbage collection behavior, and XAML binding lifecycle scenarios. Tests that native objects are properly released when managed references are dropped.

**When to add tests here:** For testing GC behavior, reference cycle detection, weak reference tracking, or XAML element lifetime in the visual tree.

**Project settings:**
- **Type:** WinExe (desktop XAML application)
- **Test framework:** MSTest (runs inside a WinUI application)
- **Dependencies:** Microsoft.WindowsAppSDK.WinUI, Windows/WinAppSDK projections

**Key patterns:**
- `WeakReference` tracking to verify objects are collected
- `AsyncQueue` helper for scheduling actions on UI thread and forcing GC
- Tests named `BasicTestN()`, `CycleTestN()`, `LeakTestN()`

### 5. Authoring tests (`src/Tests/AuthoringTest/`) — currently disabled, WIP

**What it tests:** Authoring a WinRT component in C# — validates that diverse type patterns (enums, structs, classes, interfaces, delegates, collections, XAML controls, async operations, data binding types) can be successfully projected as a WinRT component.

**When to add tests here:** Currently disabled. When enabled, for testing new WinRT component authoring scenarios.

**Project settings:**
- **Type:** Class library with `CsWinRTComponent=true`
- **TFM:** `net10.0`
- **Release x64:** NativeAOT self-contained mode
- Build-time validation (compilation succeeds = test passes)

## Deciding where to add tests

| You want to test... | Add test to... |
|---------------------|----------------|
| Marshalling a type across the WinRT boundary | `UnitTest/` (add to `TestWinRT` or `UnitTestCSharp`) |
| A COM interop scenario | `UnitTest/` (add to `ComInteropTests` or `ComGenerationTests`) |
| Exception/HRESULT conversion | `UnitTest/` (add to `ExceptionTests`) |
| A scenario must survive trimming/AOT | `FunctionalTests/` (new or existing project) |
| A new collection/async/CCW pattern under AOT | `FunctionalTests/` (new or existing project) |
| A source generator's output | `SourceGenerator2Test/` (new `Test_*` class or add to existing) |
| An analyzer diagnostic | `SourceGenerator2Test/` (new `Test_*Analyzer` class or add to existing) |
| GC/reference tracking behavior | `ObjectLifetimeTests/` |
| XAML visual tree element lifetime | `ObjectLifetimeTests/` |
| WinRT component authoring patterns | `AuthoringTest/` (when enabled) |
| Generated projection code patterns or cross-ABI control flow | Update `TestComponentCSharp/` and add tests in `UnitTest/` or `FunctionalTests/` |

## Test component: TestComponentCSharp (`src/Tests/TestComponentCSharp/`)

A **WinRT test component** (defined in `class.idl`, implemented in C++) that complements the general `TestComponent` from the [TestWinRT](https://github.com/microsoft/TestWinRT/) submodule. It tests scenarios specific to the C#/WinRT language projection.

**When to update this project:** When you need to validate generated projection code patterns or cross-ABI control flow — e.g. a C# type calling a method on a projected object with specific parameters, and the native implementation validating the result. New types and members can be added to `class.idl` as needed.

**Referenced from:** unit tests (`UnitTest/`), functional tests (`FunctionalTests/`), and projection test projects (`Projections/Test/`).

## Style rules

- Use MSTest attributes (`[TestClass]`, `[TestMethod]`) for all test projects except functional tests
- Functional tests use top-level statements and exit codes (no test framework)
- Test naming: `DescriptiveCondition_ExpectedBehavior` for analyzer/generator tests
- Use raw string literals (`"""..."""`) for inline source code in generator/analyzer tests
- Use `[DataRow]` for parameterized MSTest tests
- Keep test methods focused — one scenario per test
