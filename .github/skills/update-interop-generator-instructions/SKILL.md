---
name: update-interop-generator-instructions
description: Update the interop generator skill instructions for CsWinRT. Use when the user wants to refresh, sync, or update the interop generator skill, or when they mention that the interop generator docs are outdated or need updating.
---

# Update CsWinRT interop generator instructions

Perform an extensive, in-depth analysis of the interop generator project (`src/WinRT.Interop.Generator/`) and update `.github/skills/interop-generator/SKILL.md` to reflect the current state of the code. The interop generator skill is the authoritative Copilot context document for understanding, modifying, and debugging the interop sidecar generator — it must accurately describe the architecture, pipeline, file organization, and conventions.

<investigate_before_answering>
Read `.github/skills/interop-generator/SKILL.md` thoroughly before making any changes. Understand every section and what it claims about the codebase. Then investigate the actual codebase to find discrepancies.
</investigate_before_answering>

## Workflow

### Step 1: read the current instructions

Read `.github/skills/interop-generator/SKILL.md` in full. Take note of every factual claim it makes: file lists, directory structures, type names, method names, CLI parameters, error code ranges, pipeline steps, builder/factory/resolver descriptions, etc.

### Step 2: verify project structure

Launch an explore agent to verify the directory tree listed in the skill matches the actual project structure at `src/WinRT.Interop.Generator/`. Check:

- Every file and subdirectory listed still exists
- No new files or subdirectories have been added that are not documented
- No files have been renamed or removed
- File descriptions still match their actual purpose

### Step 3: verify the pipeline architecture

Launch an explore agent to verify the pipeline flow described in the skill. Check:

- **Entry point** (`Program.cs`) still delegates to `InteropGenerator.Run`
- **Discovery phase** steps are current: module loading, type discovery methods, parallel processing
- **Emit phase** steps are current: all `Define*Types()` methods, rewrite/fixup steps, output writing
- **Phase ordering** is still correct

### Step 4: verify .rsp file handling

Launch an explore agent to verify:

- **Parameter list** is complete — check `InteropGeneratorArgs.cs` for all properties with `[CommandLineArgumentName]`
- **Parsing/serialization** logic in `.Parsing.cs` and `.Formatting.cs` is accurately described
- No parameters have been added, removed, or renamed

### Step 5: verify debug repro handling

Launch an explore agent to verify:

- **Debug repro structure** (zip contents) is accurately described
- **DLL naming scheme** (SHAKE128 hash) is still used
- **Save/unpack methods** are current
- No changes to the debug repro workflow

### Step 6: verify the discovery phase

Launch an explore agent to verify:

- **Discovery state** (`InteropGeneratorDiscoveryState.cs`) collections are complete
- **Type discovery methods** in `Discovery/InteropTypeDiscovery.cs` and `.Generics.cs` are current
- **Generic cascade table** (which interface cascades to which) is accurate
- **Visitor classes** in `Visitors/` are complete and correctly described
- **Type exclusions** are current
- **Interface limit** is still 128

### Step 7: verify the emit phase

Launch an explore agent to verify:

- **Emit state** (`InteropGeneratorEmitState.cs`) collections are complete
- **Define*Types() method list** matches the actual methods in `InteropGenerator.Emit.cs`
- **Builder partial files** in `Builders/` are complete (check for added or removed partials)
- **What gets generated** per type category is still accurate
- **Factory classes** in `Factories/` are current
- **Dynamic custom-mapped types** are current (check builder partial files)

### Step 8: verify two-pass IL and fixups

Launch an explore agent to verify:

- **MethodRewriteInfo variants** in `Models/MethodRewriteInfo/` are complete
- **Rewriter classes** in `Rewriters/` are complete
- **Fixup classes** in `Fixups/` are complete
- **Descriptions** of each variant/rewriter/fixup are still accurate

### Step 9: verify resolvers and references

Launch an explore agent to verify:

- **Resolver classes** in `Resolvers/` are complete and accurately described
- **Reference classes** in `References/` are complete
- **Well-known interface IIDs** are current (native interface entry order, reserved IIDs)
- **Marshaller type resolution** logic is current

### Step 10: verify helpers

Launch an explore agent to verify:

- **SignatureGenerator** primitives and projection logic are current
- **GuidGenerator** algorithm is accurately described
- **TypeMapping** entries are current (check for added/removed mappings)
- **TypeExclusions** are current
- **MvidGenerator** algorithm is current

### Step 11: verify diagnostics

Launch an explore agent to verify:

- **Error code categories** table is complete — check `WellKnownInteropExceptions.cs` for all factory methods
- **Error code range** is accurate
- **Error types** (WellKnownInteropException, WellKnownInteropWarning, UnhandledInteropException) are current

### Step 12: update the skill

Apply surgical edits to `.github/skills/interop-generator/SKILL.md` to fix any discrepancies found. Typical updates include:

- **Added/removed/renamed files or directories**
- **New or removed CLI parameters**
- **Changed pipeline steps or ordering**
- **New or removed builder/factory/resolver/rewriter/fixup classes**
- **Changed error codes or error code ranges**
- **New generic interface types or cascading relationships**
- **Changed type exclusions or filtering logic**
- **New or changed type mapping entries**
- **Updated control flow between generated code and WinRT.Runtime**
- **Changed naming conventions or patterns**
- **New or removed MethodRewriteInfo variants**

<style_rules>
- Use sentence case for all headings (only capitalize proper nouns and the first word)
- Use `.dll` (lowercase) not `.DLL`
- Write "Windows Runtime" (not "WinRT") when referring to the technology in prose, but "WinRT" is fine in type/project/tool names
- Keep the same structure and tone as the existing document
- Do not add unnecessary capitalization to words in headings or prose
- Use backtick formatting for all file names, class names, method names, and inline code
</style_rules>

### Step 13: update related documentation if applicable

If the changes to the interop generator are significant enough to affect the design documents, also check and update the documentation files in `docs/cswinrtgen/`:

- `docs/cswinrtgen/marshalling-generic-interfaces.md` — Generic interface marshalling design
- `docs/cswinrtgen/marshalling-arrays.md` — Array marshalling design
- `docs/cswinrtgen/name-mangling-scheme.md` — Name mangling scheme for generated interop types

These docs describe the *design* of the generated code patterns. If the actual generated code has diverged from what these docs describe (e.g., new types generated, changed API patterns, renamed infrastructure types), update the docs to match.

### Step 14: update this skill if needed

If significant changes to the interop generator were discovered (e.g., new pipeline phases, new categories of generated code, restructured directories, new resolver/builder/factory classes), also update this skill file (`.github/skills/update-interop-generator-instructions/SKILL.md`) to reflect those changes. In particular:

- The **per-area verification steps** (steps 2–11) must stay in sync with the actual project structure. If a new area is added (e.g., a new subdirectory with its own logic), add a verification step for it.
- The **verification criteria** for each step should reflect what is actually worth checking. If a section gains new aspects worth validating (e.g., new builder partials, new resolver types), add those to the checklist.
- The **documentation step** (step 13) should list all docs in `docs/cswinrtgen/` that may need updating.

This ensures the skill remains useful and accurate for future runs.

### Step 15: summarize changes

After editing, provide a clear summary of what was updated and why, so the user can review the changes before committing.

## What NOT to change

- Do not rewrite sections that are already accurate
- Do not change the overall document structure or section ordering without good reason
- Do not remove architectural motivation sections — these explain design decisions that don't change with code updates
- Do not change prose style or formatting unless fixing a factual error
- Do not remove the control flow section explaining how generated code interacts with WinRT.Runtime — this is essential context
