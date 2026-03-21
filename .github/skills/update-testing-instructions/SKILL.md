---
name: update-testing-instructions
description: Update the testing skill instructions for CsWinRT. Use when the user wants to refresh, sync, or update the testing instructions, or when they mention that the testing docs are outdated or need updating.
---

# Update CsWinRT testing instructions

Perform an extensive, in-depth analysis of all test projects in the CsWinRT 3.0 repository and update `.github/skills/testing/SKILL.md` to reflect the current state of the test suite. The testing skill is the authoritative Copilot context document for test placement, patterns, and conventions — it must accurately describe every test project, its settings, classes, patterns, and examples.

<investigate_before_answering>
Read `.github/skills/testing/SKILL.md` thoroughly before making any changes. Understand every section and what it claims about the test projects. Then investigate the actual codebase to find discrepancies.
</investigate_before_answering>

## Workflow

### Step 1: read the current testing instructions

Read `.github/skills/testing/SKILL.md` in full. Take note of every factual claim it makes: test project paths, project settings, test framework details, test class names, helper class names, code examples, functional test project lists, build settings, style rules, etc.

### Step 2: verify unit tests (`src/Tests/UnitTest/`)

Launch an explore agent to analyze the unit test project. Verify:

- **Project settings** are current: test framework, TFM property name, output type, self-contained/AOT settings
- **Key dependencies** listed in the `.csproj` are current (MSTest packages, CsWin32, Newtonsoft.Json, etc.)
- **Project references** listed (WinRT.SourceGenerator2, projection projects) are current
- **Test class list** is complete and accurate — check all `.cs` files for `[TestClass]` classes, verify no classes have been added, removed, or renamed
- **Namespace** is still `UnitTest`
- **Shared helpers** (`UnitTestHelper.cs`, `TestModuleInitializer.cs`) still exist
- **Assert methods** listed are representative of what's actually used
- **Conditional compilation** patterns (e.g. `ENABLE_WORKSTATION_TESTS`) are still used

### Step 3: verify functional tests (`src/Tests/FunctionalTests/`)

Launch an explore agent to analyze the functional test projects. Verify:

- **Project structure** description is accurate (standalone console apps with exit codes)
- **Existing test projects table** is complete — list all subdirectories under `FunctionalTests/` and check for added or removed projects. For each project, verify the description matches what it actually tests (check `Program.cs`)
- **Shared configuration** (`Directory.Build.props`) settings are current: `IsTrimmable`, `IsAotCompatible`, `PublishAot`, `TreatWarningsAsErrors`, `ControlFlowGuard`, `AllowUnsafeBlocks`, TFM property name
- **Exit code convention** is still accurate (100 = success)
- **"To add a new functional test" instructions** are still correct (TFM property name, project references, CI pipeline step)

### Step 4: verify source generator and analyzer tests (`src/Tests/SourceGenerator2Test/`)

Launch an explore agent to analyze the source generator test project. Verify:

- **Project settings** are current: test framework, TFM, key dependencies (MSTest, `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`, `Basic.Reference.Assemblies.*`)
- **Project references** are current (WinRT.SourceGenerator2, projection projects, WinRT.Runtime)
- **Test class table** is complete and accurate — check all `Test_*.cs` files, verify no classes have been added, removed, or renamed, and verify each class's description matches its actual content
- **Test helper classes** in `Helpers/` are accurately listed and described
- **Generator test pattern example** matches the actual conventions used in the test files (variable names, method signatures, `VerifySources` call style)
- **Analyzer test pattern example** matches the actual conventions used in the test files (variable names, method signatures, `VerifyAnalyzerAsync` call style, use of `{|DIAGNOSTIC_ID:target|}` syntax)
- **Diagnostic ID ranges** mentioned for each analyzer test class are still correct

### Step 5: verify object lifetime tests (`src/Tests/ObjectLifetimeTests/`)

Launch an explore agent to analyze the object lifetime test project. Verify:

- **Project settings** are current: output type, test framework, dependencies
- **Key patterns** listed (WeakReference tracking, AsyncQueue helper, test naming) are still accurate
- **Test naming convention** (`BasicTestN()`, `CycleTestN()`, `LeakTestN()`) is still used

### Step 6: verify authoring tests (`src/Tests/AuthoringTest/`)

Launch an explore agent to verify:

- **Current status** (enabled/disabled) is accurately described
- **Project settings** are current: output type, TFM, `CsWinRTComponent` setting, AOT mode
- **What it tests** description is accurate

### Step 7: verify TestComponentCSharp (`src/Tests/TestComponentCSharp/`)

Launch an explore agent to verify:

- **Description** is accurate (WinRT test component, IDL file name, C++ implementation)
- **"Referenced from" list** is current
- **"When to update" guidance** is still correct

### Step 8: verify the "deciding where to add tests" table

Check every row in the routing table against the actual test projects. Verify:

- All existing test project categories are represented
- The recommended test classes/projects for each scenario are still valid (e.g. `TestWinRT`, `ComInteropTests` still exist)
- No new test project categories exist that should be added as rows
- The `TestComponentCSharp` guidance is still accurate

### Step 9: verify code examples

For each code example in the testing skill, verify it matches the conventions actually used in the test files:

- **Unit test pattern**: verify the example uses the correct assert methods, attributes, and structure
- **Functional test pattern**: verify the example uses the correct exit codes and top-level statement style
- **Generator test pattern**: verify the `VerifySources` call matches the actual method signature and style. Check an actual test method in `Test_CustomPropertyProviderGenerator` to confirm
- **Analyzer test pattern**: verify the `VerifyAnalyzerAsync` call matches the actual method signature and style. Check an actual test method in the analyzer test files to confirm

### Step 10: update the testing instructions

Apply surgical edits to `.github/skills/testing/SKILL.md` to fix any discrepancies found. Typical updates include:

- **Added/removed/renamed test classes** in any project
- **Added/removed functional test projects** in the project table
- **Changed project settings** (TFM, dependencies, build settings)
- **Changed test patterns or conventions** (method signatures, helper APIs, assert methods)
- **Changed test helper classes** (added, removed, renamed)
- **Changed diagnostic ID ranges** for analyzer tests
- **New test project categories** that need routing table entries
- **Outdated code examples** that no longer match actual conventions

<style_rules>
- Use sentence case for all headings (only capitalize proper nouns and the first word)
- Keep code examples minimal but representative — they should match what the actual tests look like
- Use backtick formatting for all file names, class names, method names, property names, and inline code
- Keep the same structure and tone as the existing document
- Do not add unnecessary commentary or explanation beyond what's needed for test placement guidance
</style_rules>

### Step 11: update this skill if needed

If significant changes to the test suite were discovered (e.g. test projects added or removed, new categories of tests, changed validation criteria), also update this skill file (`.github/skills/update-testing-instructions/SKILL.md`) to reflect those changes. In particular:

- The **per-project verification steps** (steps 2–7) must stay in sync with the actual test projects. If a test project is added or removed, add or remove its verification step accordingly.
- The **verification criteria** for each step should reflect what is actually worth checking. If a project gains new aspects worth validating (e.g. new test helper classes, new build settings), add those to the checklist.
- The **code example verification step** (step 9) should list all code examples present in the testing skill.

This ensures the skill remains useful and accurate for future runs.

### Step 12: summarize changes

After editing, provide a clear summary of what was updated and why, so the user can review the changes before committing.

## What NOT to change

- Do not rewrite sections that are already accurate
- Do not change the overall document structure or section ordering without good reason
- Do not expand code examples beyond what's needed to show the pattern — keep them concise
- Do not change prose style or formatting unless fixing a factual error
- Do not remove the "When to add tests here" guidance for each project — these are essential for test routing
