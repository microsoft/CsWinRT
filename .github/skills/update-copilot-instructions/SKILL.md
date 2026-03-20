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

Launch parallel explore agents for each of the 7 CsWinRT 3.0 projects listed in the instructions. For each project, verify:

1. **WinRT.Runtime (`src/WinRT.Runtime2/`)**
   - Directory structure matches what's documented
   - Key types listed still exist and have the described purposes
   - T4 templates (`.tt` files) are accurately listed
   - Project settings (TFM, language version, nullable, unsafe, etc.) are current
   - Namespace organization matches

2. **WinRT.SourceGenerator2 (`src/Authoring/WinRT.SourceGenerator2/`)**
   - Source generators listed still exist and generate what's described
   - Diagnostic analyzer list is complete and IDs are correct (check `DiagnosticDescriptors.cs`)
   - Diagnostic ID range is accurate
   - Project dependencies are current

3. **cswinrt.exe (`src/cswinrt/`)**
   - Key files listed still exist
   - Command-line options are current (check `settings.h`)
   - Namespace additions in `strings/additions/` are up to date
   - Generated code patterns are accurately described

4. **Impl generator (`src/WinRT.Impl.Generator/`)**
   - Type forward routing logic is current
   - Project settings and dependencies are current
   - CLI parameters are current

5. **Projection generator (`src/WinRT.Projection.Generator/`)**
   - Three projection modes are accurately described
   - Namespace filter logic is current
   - Project settings and dependencies are current

6. **Interop generator (`src/WinRT.Interop.Generator/`)**
   - Generated content categories are current
   - Directory structure and key types are accurate
   - Project settings and dependencies are current

7. **Generator tasks (`src/WinRT.Generator.Tasks/`)**
   - MSBuild task classes are accurately listed
   - Task-to-tool mappings are current

8. **SDK projection builds (`src/WinRT.Sdk.Projection/`)**
   - Assembly name logic (base vs XAML) is current
   - Windows SDK package download and WinMD sourcing is accurate
   - Build parameters (`WindowsSdkBuild`, `WindowsSdkXaml`) are current
   - Project settings are current

### Step 3: verify the build pipeline

Analyze the `nuget/` folder to verify:

- All `.props` and `.targets` files listed in the instructions still exist
- No new significant `.props`/`.targets` files have been added
- Key MSBuild properties table is complete and defaults are accurate
- Build pipeline flow described matches the actual target ordering

### Step 4: verify code style and conventions

Spot-check project files to verify:

- Language version, nullable, unsafe, and other compiler settings
- Warning suppression and code style enforcement settings
- Naming conventions and patterns described are still used

### Step 5: update the instructions

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
- Use `.dll` (lowercase) not `.DLL`
- Write "Windows Runtime" (not "WinRT") when referring to the technology in prose, but "WinRT" is fine in type/project/tool names
- Keep the same structure and tone as the existing document
- Do not add unnecessary capitalization to words in headings or prose
</style_rules>

### Step 6: update this skill if needed

If significant changes to the solution were discovered (e.g. projects added or removed, new components worth validating, changed validation criteria), also update this skill file (`.github/skills/update-copilot-instructions/SKILL.md`) to reflect those changes. In particular:

- The **project list in step 2** must stay in sync with the actual projects in the solution and in the Copilot instructions. If a project is added or removed, add or remove its entry and validation steps accordingly.
- The **validation steps** for each project should reflect what is actually worth checking. If a project gains new aspects worth validating (e.g. a new source generator, a new category of generated output, new CLI parameters), add those to the checklist. If aspects are removed, remove them.
- The **build pipeline checks in step 3** should reflect the current set of MSBuild files and properties.
- Any **new steps or categories of validation** discovered during the update should be added to this workflow.

This ensures the skill remains useful and accurate for future runs.

### Step 7: summarize changes

After editing, provide a clear summary of what was updated and why, so the user can review the changes before committing.

## What NOT to change

- Do not rewrite sections that are already accurate
- Do not change the overall document structure or section ordering without good reason
- Do not remove the architectural motivation sections ("Why reference projections?", "Why the interop generator?") — these explain design decisions that don't change with code updates
- Do not touch the Mermaid diagrams unless the actual architecture has changed
- Do not change prose style or formatting unless fixing a factual error
