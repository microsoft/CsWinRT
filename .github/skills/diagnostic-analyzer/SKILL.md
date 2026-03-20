---
name: diagnostic-analyzer
description: Create a new Roslyn diagnostic analyzer for the CsWinRT source generator project. Use when the user wants to add a new analyzer, add a new diagnostic warning or error, or validate usage of an attribute or API pattern at compile time.
---

# Create a diagnostic analyzer

Add a new Roslyn diagnostic analyzer to the CsWinRT source generator project (`src/Authoring/WinRT.SourceGenerator2/`). Follow the established patterns and group changes into separate commits.

<investigate_before_answering>
Before creating the analyzer, read the existing analyzers and diagnostic descriptors to understand:
- The current diagnostic ID range and the next available ID
- The code style, structure, and patterns used
- The test infrastructure and test patterns

Key files to read:
- `src/Authoring/WinRT.SourceGenerator2/Diagnostics/DiagnosticDescriptors.cs` — all diagnostic descriptors
- `src/Authoring/WinRT.SourceGenerator2/AnalyzerReleases.Shipped.md` — shipped diagnostics table
- All analyzers in `src/Authoring/WinRT.SourceGenerator2/Diagnostics/Analyzers/` — code patterns
- `src/Tests/SourceGenerator2Test/Helpers/CSharpAnalyzerTest{TAnalyzer}.cs` — test helper

Also check these external repositories for additional analyzer patterns to reference:
- https://github.com/Sergio0694/ComputeSharp/tree/main/src/ComputeSharp.D2D1.SourceGenerators/Diagnostics/Analyzers/
- https://github.com/Sergio0694/ComputeSharp/tree/main/src/ComputeSharp.SourceGenerators/Diagnostics/Analyzers/
</investigate_before_answering>

## Step 1: Add the diagnostic descriptor

**File:** `src/Authoring/WinRT.SourceGenerator2/Diagnostics/DiagnosticDescriptors.cs`

Add a new `DiagnosticDescriptor` field following the existing pattern:

```csharp
/// <summary>
/// Gets a <see cref="DiagnosticDescriptor"/> for [describe what it detects].
/// </summary>
public static readonly DiagnosticDescriptor MyNewDiagnostic = new(
    id: "CSWINRT2XXX",
    title: "Short title describing the issue",
    messageFormat: """The type '{0}' has some problem because...""",
    category: "WindowsRuntime.SourceGenerator",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Longer description explaining the constraint and why it exists.",
    helpLinkUri: "https://github.com/microsoft/CsWinRT");
```

**Conventions:**
- IDs use the `CSWINRT2XXX` pattern (current range: `CSWINRT2000`–`CSWINRT2008`)
- Use the next sequential ID after the highest existing one
- Category is always `"WindowsRuntime.SourceGenerator"`
- Help link URI is always `"https://github.com/microsoft/CsWinRT"`
- Default severity is typically `DiagnosticSeverity.Error` (use `Warning` only if explicitly requested)
- Use `{0}`, `{1}`, etc. in `messageFormat` for the type name and any other contextual info
- The XML doc comment should say: `Gets a <see cref="DiagnosticDescriptor"/> for [what it detects].`

**Commit this change alone** with a message like: `Add CSWINRT2XXX diagnostic descriptor for [description]`

## Step 2: Register the diagnostic in AnalyzerReleases.Shipped.md

**File:** `src/Authoring/WinRT.SourceGenerator2/AnalyzerReleases.Shipped.md`

Add a new row to the table under the `## Release 3.0.0` / `### New Rules` section:

```markdown
CSWINRT2XXX | WindowsRuntime.SourceGenerator | Error | Short title describing the issue
```

The columns are: `Rule ID | Category | Severity | Notes` (where Notes = the title from the descriptor).

**Important:** Always edit `AnalyzerReleases.Shipped.md`, NOT `AnalyzerReleases.Unshipped.md`.

**Include this in the same commit as step 1.**

## Step 3: Create the analyzer class

**File:** `src/Authoring/WinRT.SourceGenerator2/Diagnostics/Analyzers/MyNewAnalyzer.cs`

Follow the established code pattern:

```csharp
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that [describes what it validates].
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MyNewAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [
        DiagnosticDescriptors.MyNewDiagnostic];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            // Resolve any required symbols from the compilation
            if (context.Compilation.GetTypeByMetadataName("Some.Required.Type") is not { } requiredType)
            {
                return;
            }

            context.RegisterSymbolAction(context =>
            {
                // Filter to relevant symbols
                if (context.Symbol is not INamedTypeSymbol typeSymbol)
                {
                    return;
                }

                // Check conditions and report diagnostics
                if (/* violation detected */)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.MyNewDiagnostic,
                        typeSymbol.Locations.FirstOrDefault(),
                        typeSymbol));
                }
            }, SymbolKind.NamedType);
        });
    }
}
```

**Code conventions:**
- File-scoped namespace: `namespace WindowsRuntime.SourceGenerator.Diagnostics;`
- Class is `public sealed` and extends `DiagnosticAnalyzer`
- Has `[DiagnosticAnalyzer(LanguageNames.CSharp)]` attribute
- `SupportedDiagnostics` uses collection expression syntax: `[Descriptor1, Descriptor2]`
- Always call `context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`
- Always call `context.EnableConcurrentExecution()`
- Use `RegisterCompilationStartAction` wrapping `RegisterSymbolAction` (or `RegisterSyntaxNodeAction`)
- Use `static` lambdas where possible
- Resolve required type symbols from `context.Compilation.GetTypeByMetadataName()` and bail early if not found
- Use the `HasAttributeWithType()` extension method (from `ISymbolExtensions`) to check for attributes
- Report diagnostics with `Diagnostic.Create(descriptor, location, messageArgs...)`
- Use `typeSymbol.Locations.FirstOrDefault()` for the diagnostic location

**Commit this change alone** with a message like: `Add [analyzer name] analyzer for CSWINRT2XXX`

## Step 4: Add tests (if requested)

**File:** `src/Tests/SourceGenerator2Test/Test_MyNewAnalyzer.cs`

Create a new test class following the existing pattern:

```csharp
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

using VerifyCS = CSharpAnalyzerTest<MyNewAnalyzer>;

/// <summary>
/// Tests for <see cref="MyNewAnalyzer"/>.
/// </summary>
[TestClass]
public sealed class Test_MyNewAnalyzer
{
    // --- Tests where the analyzer should NOT warn ---

    [TestMethod]
    public async Task ValidScenario_DoesNotWarn()
    {
        const string source = """
            // valid code that should not trigger the diagnostic
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    // --- Tests where the analyzer SHOULD warn ---

    [TestMethod]
    public async Task InvalidScenario_Warns()
    {
        const string source = """
            // code with {|CSWINRT2XXX:target|} inline diagnostic marker
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
```

**Test conventions:**
- File name: `Test_MyNewAnalyzer.cs`
- Class name: `Test_MyNewAnalyzer` (matches analyzer name with `Test_` prefix)
- Add `using VerifyCS = CSharpAnalyzerTest<MyNewAnalyzer>;` alias
- Test naming: `Condition_ExpectedBehavior` (e.g. `ValidType_DoesNotWarn`, `StaticType_Warns`)
- Use `[DataRow]` for parameterized tests when testing multiple type modifiers or variations
- Use `{|CSWINRT2XXX:target|}` inline syntax to mark expected diagnostic locations in source
- For more complex cases, use explicit `expectedDiagnostics` parameter with `DiagnosticResult` objects
- Always include both positive tests (no warning expected) and negative tests (warning expected)
- Use raw string literals (`"""..."""`) for inline C# source code
- Test methods are `async Task` (the verify helper is async)

**Commit this change alone** with a message like: `Add tests for [analyzer name] analyzer`

## Extension method reference

The source generator project provides these extension methods for analyzing symbols (in `Extensions/ISymbolExtensions.cs`):

- `HasAttributeWithType(ISymbol, INamedTypeSymbol)` — checks if a symbol has a specific attribute
- Other `ITypeSymbolExtensions` for type analysis

Check the `Extensions/` folder for all available helpers before writing new utility code.
