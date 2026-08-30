---
name: update-copilot-instructions
description: Update the Copilot instructions file for the CsWinRT project. Use when the user wants to refresh, sync, or update the copilot instructions, or when they mention that the instructions are outdated or need updating.
---

# Update CsWinRT Copilot instructions

Perform an extensive, in-depth analysis of the CsWinRT 3.0 codebase and update `.github/copilot-instructions.md` to reflect the current state of the code. The instructions file is the authoritative Copilot context document for this repository — it must accurately describe the architecture, projects, conventions, and build pipeline.

<investigate_before_answering>
Read `.github/copilot-instructions.md` thoroughly before making any changes. Understand every section and what it claims about the codebase. Then investigate the actual codebase to find discrepancies.
</investigate_before_answering>

## Workflow

### Step 1: read the current instructions

Read `.github/copilot-instructions.md` in full. Take note of every factual claim it makes: directory structures, file lists, type names, diagnostic IDs, MSBuild properties, tool behaviors, project settings, etc.

### Step 2: analyze each project in depth

Launch parallel explore agents for each of the 12 CsWinRT 3.0 projects listed in the instructions. For each project, verify:

1. **WinRT.Runtime (`src/WinRT.Runtime2/`)**
   - Directory structure matches what's documented
   - Authored composable-class support is documented: `InteropServices/Aggregation/`, `WindowsRuntimeComposition`, `WindowsRuntimeOverridableAttribute`, non-delegating inner objects, and per-aggregate delegating vtables
   - Key types listed still exist and have the described purposes
   - T4 templates (`.tt` files) are accurately listed
   - Project settings (TFM, language version, nullable, unsafe, etc.) are current
   - Namespace organization matches
   - Reference assembly build is documented: the dual implementation/reference build driven by `CsWinRTBuildReferenceAssembly`, the `WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY` / `WINDOWS_RUNTIME_REFERENCE_ASSEMBLY` / `WINDOWS_RUNTIME_IMPLEMENTATION_ONLY_FILE` compilation symbols, the `[WindowsRuntimeImplementationOnlyMember]` marker attribute (`Attributes/WindowsRuntimeImplementationOnlyMemberAttribute.cs`), the `BannedSymbols.txt` + `Microsoft.CodeAnalysis.BannedApiAnalyzers` guard (`RS0030` as error), the reference-assembly-only `WindowsRuntimeObject()` constructor (`CSWINRT3001` obsolete diagnostic), and packaging into `ref\net10.0\` alongside the implementation in `lib\net10.0\` (`src/build.cmd`, `nuget/Microsoft.Windows.CsWinRT.nuspec`)
   - The **two strategies for keeping API out of the supported surface** are documented and current: (1) **strip entirely** (the default — `[WindowsRuntimeImplementationOnlyMember]` or an excluded file/folder, so the type is absent from the reference assembly), and (2) **public but hidden** (the exception — the type stays in the reference assembly but is marked reference-assembly-only `[Obsolete(..., DiagnosticId = "CSWINRT3xxx")]` + `[EditorBrowsable(Never)]`). Strategy 2 is required only when CsWinRT-generated code that compiles against the reference assembly names the type (stripping would then cause `CS0234`/`CS0246`); the generated code suppresses the diagnostic so normal builds are unaffected. Verify the list of current strategy-2 cases is accurate and complete by checking the `CSWINRT3xxx` constants in `Properties/WindowsRuntimeConstants.cs` and the per-diagnostic pages under `docs/diagnostics/`: `CSWINRT3001` (the `WindowsRuntimeObject()` constructor), `CSWINRT3002` (the three type map group types, named by the source generator's `[assembly: TypeMapAssemblyTarget<TGroup>]` output), `CSWINRT3003` (`WindowsRuntimeComponentAssemblyAttribute` / `WindowsRuntimeComponentAssemblyExportsTypeAttribute`, named by the authoring generator's `ManagedExports.g.cs`), and `CSWINRT3004` (`WindowsRuntimeReferenceAssemblyAttribute`, emitted as `[assembly: WindowsRuntimeReferenceAssembly]` by the projection writer's `AssemblyAttributes.cs` base resource and shipped in reference projection assemblies). If a `CSWINRT3xxx` diagnostic is added or removed, update both the reference-assembly section and the runtime row of the "Error ID ranges" table in the instructions

2. **WinRT.SourceGenerator2 (`src/Authoring/WinRT.SourceGenerator2/`)**
   - Source generators listed still exist and generate what's described
   - Generated output that references **public-but-hidden** runtime types still suppresses the corresponding obsolete diagnostics: `TypeMapAssemblyTargetGenerator`'s `[assembly: TypeMapAssemblyTarget<TGroup>]` output names the three type map group types (`CSWINRT3002`), and `AuthoringExportTypesGenerator`'s `ManagedExports.g.cs` names the component authoring attributes (`CSWINRT3003`). If a generator starts or stops emitting references to a strategy-2 type, keep this in sync with the "Two strategies for implementation-only API" coverage in the WinRT.Runtime entry above
   - Diagnostic analyzer list is complete and IDs are correct (check `DiagnosticDescriptors.cs` and `AnalyzerReleases.Shipped.md`)
   - Diagnostic ID range is accurate
   - Project dependencies are current
   - Assembly name is current (it is `WinRT.SourceGenerator`, **not** `WinRT.SourceGenerator2` — the project folder has `2` for repo history, but the produced .dll does not)

3. **Projection writer (`src/WinRT.Projection.Writer/`)**
   - Directory structure and namespaces match (`Attributes/`, `Builders/`, `Errors/`, `Extensions/`, `Factories/`, `Generation/`, `Helpers/`, `Metadata/`, `Models/`, `References/`, `Resolvers/`, `Resources/`, `Writers/`)
   - Public API surface (`ProjectionWriter.Run`, `ProjectionWriterOptions` shape) is accurate
   - Error ID range (5xxx in `Errors/WellKnownProjectionWriterExceptions.cs`) is accurate
   - Resources structure (`Additions/` per-namespace + `Base/` baseline) matches
   - Composable component output is current: composition factories, cached aggregation entries, protected/overridable member projection, and `[UnsafeAccessor]` CCW dispatch

4. **Reference projection generator (`src/WinRT.Projection.Ref.Generator/`)**
   - CLI parameters on `ReferenceProjectionGeneratorArgs` are current
   - Error ID range (`CSWINRTPROJECTIONREFGENxxxx`) in `Errors/WellKnownReferenceProjectionGeneratorExceptions.cs` is accurate
   - Project settings (Native AOT, dependencies) are current
   - MSBuild integration via `nuget/Microsoft.Windows.CsWinRT.targets` (CsWinRTGenerateProjection target → `RunCsWinRTProjectionRefGenerator`) is wired
   - Debug repro support is documented (mentions `--debug-repro-directory`, `ref-projection-debug-repro.zip`, expansion of input tokens to concrete `.winmd` files)

5. **Impl generator (`src/WinRT.Impl.Generator/`)**
   - Type forward routing logic is current
   - Project settings and dependencies are current
   - CLI parameters are current
   - Debug repro support is documented (mentions `--debug-repro-directory`, `impl-debug-repro.zip`, output assembly + reference assemblies bundled)

6. **Projection generator (`src/WinRT.Projection.Generator/`)**
   - Three projection modes are accurately described
   - Namespace filter logic is current
   - Project settings and dependencies (project reference to `WinRT.Projection.Writer`) are current
   - The pipeline is documented as in-process (the projection writer is invoked as a library)
   - `ProjectionGeneratorArgs` no longer contains any leftover `CsWinRTExePath` field
   - Debug repro support is documented (mentions `--debug-repro-directory`, `projection-debug-repro.zip`, expanded Windows metadata bundled into a `windows-metadata/` subfolder)

7. **Interop generator (`src/WinRT.Interop.Generator/`)**
   - Generated content categories are current
   - Directory structure and key types are accurate
   - Project settings and dependencies are current
   - Debug repro support is documented (mentions `--debug-repro-directory`, `interop-debug-repro.zip`, reference + implementation `.dll`-s bundled into separate subfolders)

8. **WinMD generator (`src/WinRT.WinMD.Generator/`)**
   - CLI parameters on `WinMDGeneratorArgs` are current
   - Error ID range (`CSWINRTWINMDGENxxxx`) in `Errors/WellKnownWinMDExceptions.cs` is accurate
   - Composable authoring metadata is current: `[Composable]` factories, `[Protected]`/`[Overridable]` interface implementations, and diagnostics for unsupported aggregation interfaces or constructor parameters
   - Project settings and dependencies are current
   - MSBuild integration via `nuget/Microsoft.Windows.CsWinRT.Authoring.WinMD.targets` is wired (gated on `CsWinRTComponent`)
   - Debug repro support is documented (mentions `--debug-repro-directory`, `winmd-debug-repro.zip`, input component `.dll` + reference assemblies bundled)

9. **Generator core (`src/WinRT.Generator.Core/`)**
   - Project settings are current (`net10.0`, C# 14, `IsAotCompatible`, `DisableRuntimeMarshalling`, root namespace `WindowsRuntime.Generator`, assembly name `WinRT.Generator.Core`, `AsmResolver.DotNet` dependency)
   - `[InternalsVisibleTo]` is declared for all five CLI tool assemblies (`cswinrtimplgen`, `cswinrtinteropgen`, `cswinrtprojectiongen`, `cswinrtprojectionrefgen`, `cswinrtwinmdgen`)
   - Shared infrastructure types are present and accurately described: `GeneratorHost.CreateRunner` + `GeneratorPhaseRunner<TArgs>` + `IGeneratorArgs` (entry-point scaffold), `ResponseFileParser`/`ResponseFileBuilder` + `CommandLineArgumentNameAttribute` (reflection-based `.rsp` handling), `IGeneratorErrorFactory` + `WellKnownGeneratorException`/`UnhandledGeneratorException`/`WellKnownGeneratorMessages`/`GeneratorExceptionExtensions` (shared error contract), `DebugReproPacker` (debug repro), and misc helpers (`MvidGenerator`, `GeneratorJsonSerializerContext`, `{File,Path,RuntimeContext,IncrementalHash}Extensions`, `WellKnownPublicKeys`/`WellKnownPublicKeyTokens`)
   - The library has no error IDs of its own — per-tool `WellKnown*Exceptions` factories own them and are dispatched through `IGeneratorErrorFactory` (the shared unhandled-exception format is `{ErrorPrefix}9999`)

10. **Generator tasks (`src/WinRT.Generator.Tasks/`)**
   - MSBuild task classes are accurately listed (including `RunCsWinRTProjectionRefGenerator` and `RunCsWinRTWinMDGenerator`)
   - Task-to-tool mappings are current
   - No leftover `CsWinRTExePath` parameter on `RunCsWinRTMergedProjectionGenerator`
   - All five tasks expose a `DebugReproDirectory` parameter plumbed from `$(CsWinRTGeneratorDebugReproDirectory)`

11. **SDK projection builds (`src/WinRT.Sdk.Projection/`)**
    - Assembly name logic (base vs XAML) is current
    - Windows SDK package download and WinMD sourcing is accurate
    - Build parameters (`WindowsSdkBuild`, `WindowsSdkXaml`, `SdkPackageVersion`) are current
    - Project settings are current

12. **WinRT.Internal (`src/WinRT.Internal/`)**
    - Hand-authored C# source files mirror the historical IDL interop interfaces (HWND struct, `[ProjectionInternal]` attribute, all 14 `I*Interop` interfaces with their original IIDs)
    - Project TFM uses the CsWinRT 3.0 revision (`net10.0-windows10.0.X.1`) so the `cswinrt3` SDK projection reference assemblies are selected, and `WindowsSdkPackageVersion` is pinned to match
    - CsWinRT integration is disabled on the project itself (`CsWinRTEnabled`, `CsWinRTGenerateProjection`, `CsWinRTGenerateInteropAssembly[2]` all `false`)
    - `GenerateWindowsRuntimeInternalWinMD` target invokes `cswinrtwinmdgen.exe` directly via `<Exec>` (not via the `UsingTask` mechanism) to avoid `MSB3027` file-lock contention in Visual Studio
    - Output `.winmd` lands at `$(TargetDir)$(AssemblyName).winmd` (`WindowsRuntime.Internal.winmd`), and `src/Directory.Build.props` exposes it via `$(CsWinRTInteropMetadata)` for downstream consumers

### Step 3: verify the test projects

Verify the `src/Tests/` directory is accurately represented in the "Other directories" table. Check:

- Test project list is current (unit tests, functional tests, source generator/analyzer tests, object lifetime tests, authoring tests, test component)
- Test framework and project type descriptions match reality
- No significant test projects have been added or removed

### Step 4: verify the build pipeline

Analyze the `nuget/` folder to verify:

- All `.props` and `.targets` files listed in the instructions still exist
- No new significant `.props`/`.targets` files have been added
- Key MSBuild properties table is complete and defaults are accurate
- Build pipeline flow described matches the actual target ordering

### Step 5: verify code style and conventions

Spot-check project files to verify:

- Language version, nullable, unsafe, and other compiler settings
- Warning suppression and code style enforcement settings
- Naming conventions and patterns described are still used

### Step 6: update the instructions

Apply surgical edits to `.github/copilot-instructions.md` to fix any discrepancies found. Typical updates include:

- **Added/removed/renamed files or directories** in any project
- **New or removed source generators or analyzers** with their diagnostic IDs
- **Changed diagnostic ID ranges** or new diagnostics
- **New or changed MSBuild properties** or feature switches
- **Updated project settings** (TFM, dependencies, compiler options)
- **Changed type forward routing** or projection modes
- **New or removed build tools** or targets files
- **Updated architecture** (new components, changed relationships)

<style_rules>
- Use sentence case for all headings (only capitalize proper nouns and the first word)
- Do not capitalize words after `:` unless they are proper nouns (e.g. write `**Target**: net10.0`, not `**Target**: Net10.0`)
- Use `.dll` (lowercase) not `.DLL`
- Write "Windows Runtime" (not "WinRT") when referring to the technology in prose, but "WinRT" is fine in type/project/tool names
- Keep the same structure and tone as the existing document
- Do not add unnecessary capitalization to words in headings or prose
</style_rules>

### Step 7: update this skill if needed

If significant changes to the solution were discovered (e.g. projects added or removed, new components worth validating, changed validation criteria), also update this skill file (`.github/skills/update-copilot-instructions/SKILL.md`) to reflect those changes. In particular:

- The **project list in step 2** must stay in sync with the actual projects in the solution and in the Copilot instructions. If a project is added or removed, add or remove its entry and validation steps accordingly.
- The **validation steps** for each project should reflect what is actually worth checking. If a project gains new aspects worth validating (e.g. a new source generator, a new category of generated output, new CLI parameters), add those to the checklist. If aspects are removed, remove them.
- The **build pipeline checks in step 3** should reflect the current set of MSBuild files and properties.
- Any **new steps or categories of validation** discovered during the update should be added to this workflow.

This ensures the skill remains useful and accurate for future runs.

### Step 8: summarize changes

After editing, provide a clear summary of what was updated and why, so the user can review the changes before committing.

## What NOT to change

- Do not rewrite sections that are already accurate
- Do not change the overall document structure or section ordering without good reason
- Do not remove the architectural motivation sections ("Why reference projections?", "Why the interop generator?") — these explain design decisions that don't change with code updates
- Do not touch the Mermaid diagrams unless the actual architecture has changed
- Do not change prose style or formatting unless fixing a factual error
