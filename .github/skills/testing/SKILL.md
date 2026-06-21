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

CsWinRT 3.0 has 6 primary test project areas, each serving a different purpose. Additional specialized test projects also exist under `src/Tests/`:

### 1. Unit tests (`src/Tests/UnitTest/`)

**What it tests:** Core Windows Runtime interop functionality — parameter marshalling, collections, events, delegates, COM interop, exception handling, API compatibility, XAML template parts.

**When to add tests here:** For testing Windows Runtime projection behavior, marshalling correctness, COM interop scenarios, or runtime infrastructure from `WinRT.Runtime`.

**Project settings:**
- **Test framework:** MSTest (`[TestClass]`, `[TestMethod]`, `Assert.*`)
- **TFM:** Variable via `$(AppBuildTFMs)`, multi-platform (x86/x64)
- **Output type:** Exe, self-contained, AOT-enabled
- **Key dependencies:** MSTest.TestFramework, MSTest.Engine, MSTest.SourceGeneration, Microsoft.Windows.CsWin32, Newtonsoft.Json, Microsoft.VCRTForwarders.140
- **References:** WinRT.SourceGenerator2 (as analyzer), Test/TestSubset/Windows/WinAppSDK projections

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
| `Test_GeneratedCustomPropertyProviderTargetTypeAnalyzer` | CSWINRT2000–2001 diagnostics |
| `Test_GeneratedCustomPropertyProviderExistingMemberImplementationAnalyzer` | CSWINRT2003 diagnostic |
| `Test_GeneratedCustomPropertyProviderAttributeArgumentAnalyzer` | CSWINRT2004–2008 diagnostics |
| `Test_ComImportInterfaceAnalyzer` | CSWINRT2009 diagnostic (casts to `[ComImport]` interfaces) |
| `Test_ValidApiContractEnumTypeAnalyzer` | CSWINRT2010 diagnostic |
| `Test_ValidContractVersionAttributeAnalyzer` | CSWINRT2011–2013 diagnostics |
| `Test_ApiContractTypeRequiresContractVersionAnalyzer` | CSWINRT2014 diagnostic |
| `Test_PublicTypeRequiresVersioningAnalyzer` | CSWINRT2015 diagnostic |
| `Test_PublicTypeRequiresContractVersionAnalyzer` | CSWINRT2016 diagnostic |
| `Test_PublicTypeMixedVersioningAttributesAnalyzer` | CSWINRT2017 diagnostic |

**Test helpers (in `Helpers/`):**
- `CSharpGeneratorTest<TGenerator>` — runs a generator on source code and compares output
- `CSharpAnalyzerTest<TAnalyzer>` — runs an analyzer and verifies diagnostics

**Pattern for generator tests:**
```csharp
[TestMethod]
public void ValidClass_SomeScenario()
{
    const string source = """
        using WindowsRuntime.Xaml;

        namespace MyNamespace;

        [GeneratedCustomPropertyProvider]
        public partial class MyType
        {
            public string Name { get; set; }
        }
        """;

    const string result = """
        // expected generated code...
        """;

    CSharpGeneratorTest<CustomPropertyProviderGenerator>.VerifySources(source, ("MyNamespace.MyType.g.cs", result));
}
```

**Pattern for analyzer tests:**
```csharp
[TestMethod]
public async Task InvalidType_Warns()
{
    string source = """
        using WindowsRuntime.Xaml;

        [GeneratedCustomPropertyProvider]
        public static class {|CSWINRT2000:MyType|} { }
        """;

    await CSharpAnalyzerTest<GeneratedCustomPropertyProviderTargetTypeAnalyzer>.VerifyAnalyzerAsync(source);
}
```

- Use `{|DIAGNOSTIC_ID:target|}` inline syntax to mark expected diagnostics
- Or use explicit `expectedDiagnostics` array with `DiagnosticResult` for complex cases
- Pass `isCsWinRTComponent: true` to `VerifyAnalyzerAsync` for analyzers that only apply to authored components (e.g. the contract-versioning analyzers)
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

### 5. Authoring tests (`src/Tests/AuthoringTest/`)

**What it tests:** Authoring a WinRT component in C# — validates that diverse type patterns (enums, structs, classes, interfaces, delegates, collections, XAML controls, async operations, data binding types, and contract-versioning attributes) can be successfully projected as a WinRT component. The component itself builds (build-time validation); the C++ consumption tests that exercise it (`AuthoringConsumptionTest*`) are not yet enabled in the solution.

**When to add tests here:** For testing new WinRT component authoring scenarios — new type shapes, attributes, or versioning patterns.

**Project settings:**
- **Type:** `CsWinRTComponent=true` class library; Release x64 publishes as a Native AOT shared library (`OutputType=Exe`, `PublishAot=true`, `SelfContained=true`, `NativeLib=Shared`)
- **TFM:** `net10.0-windows10.0.26100.1`
- Build-time validation (compilation succeeds = test passes)

### 6. Smoke tests (`src/Tests/SmokeTests/`)

**What it tests:** End-to-end consumption of the **real** `Microsoft.Windows.CsWinRT` NuGet package — a consuming app, an authoring component, and a third-party projection — fully isolated from the repository build infrastructure. Validates that the packaged `ref`/`lib` assemblies, the build targets, and all post-build generators work correctly for an external customer.

**When to add tests here:** For verifying that the produced NuGet package works in a real, isolated environment (correct `ref`/`lib` assemblies referenced, generators running). Keep these minimal — they are smoke tests, not feature coverage. Use `UnitTest/` or `FunctionalTests/` for marshalling/feature coverage instead.

**Project structure:** Three standalone projects, intentionally kept out of `cswinrt.slnx` (the package they consume only exists after the build packs it). They are isolated from the repo build infrastructure via blank `Directory.Build.props`/`.targets` and a local `Directory.Packages.props` (central package management disabled). All shared configuration lives in `Directory.Build.props`, so each `.csproj` only carries what makes it different.

**Existing tests:**
| Project | Tests |
|---------|-------|
| `Consumption/` | An `Exe` that calls `JsonObject.Parse(...)` then `Stringify()` from `Windows.Data.Json`, exercising the Windows SDK projection, the interop generator, and the `WinRT.Runtime` ref/impl assemblies |
| `Authoring/` | A `CsWinRTComponent` library exposing a minimal `Greeter` class, exercising WinMD generation, the reference projection, and the forwarder assembly |
| `Projection/` | A `CsWinRTGenerateReferenceProjection` library that generates a reference projection for the `Authoring` component's `.winmd` (reused via a build-ordering `ProjectReference`), exercising `cswinrtprojectionrefgen` and `cswinrtimplgen`, exactly as a NuGet projection author would |

**Shared configuration (`Directory.Build.props`):**
- **TFM:** `net10.0-windows10.0.26100.1` (the `.1` CsWinRT 3.0 revision), with a pinned `WindowsSdkPackageVersion` so the build uses the real .NET SDK targeting pack (mirrors `src/WinRT.Internal`)
- `RestoreSources` overrides all inherited NuGet sources: the local CsWinRT build output (`CsWinRTPackageSource`) plus public NuGet (`PublicNuGetSource`)
- `CsWinRTPackageVersion`/`CsWinRTPackageSource` default to the local `build.cmd x64 Release` output and are overridden by the build/CI that produced the package

**How they run:** `run-smoke-tests.ps1` (parameterized by `-Test` and `-Runtime`) builds and runs the consumption app (asserting a clean exit code), builds the authoring component and verifies the generated `Authoring.winmd` defines `Authoring.Greeter`, and builds the projection library verifying it produces both a forwarder and a `ref` reference assembly. The consumption and authoring tests run on both CoreCLR and Native AOT (`-Runtime`); the projection test is build-only and runs on CoreCLR only. It is invoked after the `nuget pack` step in `src/build.cmd` (x64 only; skippable via `cswinrt_run_smoke_tests=false`) and as individual steps in `build/AzurePipelineTemplates/CsWinRT-PublishToNuGet-Steps.yml`.

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
| WinRT component authoring patterns | `AuthoringTest/` |
| The produced NuGet package works end-to-end (real `ref`/`lib` assemblies, generators) | `SmokeTests/` (`Consumption/` or `Authoring/`) |
| Generated projection code patterns or cross-ABI control flow | Update `TestComponentCSharp/` and add tests in `UnitTest/` or `FunctionalTests/` |

## Test component: TestComponentCSharp (`src/Tests/TestComponentCSharp/`)

A **WinRT test component** (defined in `TestComponentCSharp.idl`, implemented in C++) that complements the general `TestComponent` from the [TestWinRT](https://github.com/microsoft/TestWinRT/) submodule. It tests scenarios specific to the C#/WinRT language projection.

**When to update this project:** When you need to validate generated projection code patterns or cross-ABI control flow — e.g. a C# type calling a method on a projected object with specific parameters, and the native implementation validating the result. New types and members can be added to `TestComponentCSharp.idl` as needed.

**Referenced from:** unit tests (`UnitTest/`), functional tests (`FunctionalTests/`), and projection test projects (`Projections/Test/`).

## Style rules

- Use MSTest attributes (`[TestClass]`, `[TestMethod]`) for all test projects except functional tests
- Functional tests use top-level statements and exit codes (no test framework)
- Test naming: `DescriptiveCondition_ExpectedBehavior` for analyzer/generator tests
- Use raw string literals (`"""..."""`) for inline source code in generator/analyzer tests
- Use `[DataRow]` for parameterized MSTest tests
- Keep test methods focused — one scenario per test
