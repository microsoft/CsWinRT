# validate-writer-output.ps1

A regression harness that catches accidental output drift in
`WinRT.Projection.Writer`. It runs the writer against a configured set of
scenarios (each described by an `.rsp` response file, the same format
`WinRT.Projection.Writer.TestRunner` accepts), captures a SHA256 manifest of
every emitted `.cs` file, and compares the result against a previously
captured baseline.

## Usage

```powershell
# First run: capture the baseline manifests for every .rsp scenario
.\validate-writer-output.ps1 -Mode capture

# Subsequent runs: validate that the writer still produces byte-identical output
.\validate-writer-output.ps1 -Mode validate

# Convenience: capture if no baseline exists yet, otherwise overwrite on drift
.\validate-writer-output.ps1 -Mode capture-and-validate
```

## Parameters

| Parameter | Default | Purpose |
|---|---|---|
| `-Mode` | (required) | One of `capture`, `validate`, `capture-and-validate`. |
| `-RepoRoot` | the repo root, derived from the script's location | Override if running the script from outside the standard repo layout. |
| `-RspRoot` | `$RepoRoot\eng\rsp` | The directory containing the `.rsp` files. Each `.rsp` file becomes one scenario, named by its file stem. |
| `-Scenarios` | every `*.rsp` under `-RspRoot` | Restrict the scenario set when validating only a subset. |
| `-Configuration` | `Release` | The build configuration used to locate the TestRunner exe. |

## Per-scenario manifest layout

For every scenario, the script writes a `.sha256` file under
`$PSScriptRoot\baselines\<scenario>.sha256` containing one line per emitted
`.cs` file:

```
<sha256 hash>  <relative-cs-file-name>
```

Drift is reported with file-by-file diffs (added / removed / changed).

## Notes

- The `.rsp` files are not committed alongside the script because they encode
  paths into local `.winmd` metadata sources that vary between machines. Each
  contributor sets up their own `.rsp` files for the scenarios they care
  about, then captures a baseline against the writer state they consider
  correct, and validates from there.
- Pass 16 (output-format cleanup) intentionally broke byte identity. If you
  need to validate parse-equivalence rather than byte identity, use the
  Roslyn-based parse validator alongside this harness.
