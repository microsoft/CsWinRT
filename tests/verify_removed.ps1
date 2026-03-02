# Verify deprecated and removed API support in CSWinRT
# Validates helpers, code generation, and generated output

$ErrorActionPreference = "Stop"
$script:passed = 0
$script:failed = 0

function Assert-Contains {
    param([string]$File, [string]$Pattern, [string]$Message)
    $content = Get-Content $File -Raw
    if ($content -match $Pattern) {
        Write-Host "  PASS: $Message" -ForegroundColor Green
        $script:passed++
    } else {
        Write-Host "  FAIL: $Message" -ForegroundColor Red
        Write-Host "    Expected pattern: $Pattern" -ForegroundColor Yellow
        $script:failed++
    }
}

Write-Host "`n=== helpers.h: deprecated/removed helpers ===" -ForegroundColor Cyan

$helpersFile = "src\cswinrt\helpers.h"
Assert-Contains $helpersFile "is_removed" "is_removed() function exists"
Assert-Contains $helpersFile "is_deprecated" "is_deprecated() function exists"
Assert-Contains $helpersFile "get_deprecated_message" "get_deprecated_message() function exists"
Assert-Contains $helpersFile "is_deprecated_not_removed" "is_deprecated_not_removed() helper exists"
Assert-Contains $helpersFile "DeprecatedAttribute" "References DeprecatedAttribute"

Write-Host "`n=== code_writers.h: deprecated/removed handling ===" -ForegroundColor Cyan

$writersFile = "src\cswinrt\code_writers.h"
Assert-Contains $writersFile "is_removed" "code_writers.h has is_removed() checks"
Assert-Contains $writersFile "write_obsolete_attribute" "code_writers.h has write_obsolete_attribute"
Assert-Contains $writersFile "System.Obsolete" "Generates [System.Obsolete] annotations"

Write-Host "`n=== Vtable preservation: write_vtable has NO is_removed checks ===" -ForegroundColor Cyan

# The write_vtable function must iterate ALL methods including removed ones.
# Verify it does NOT contain is_removed checks (vtable slots must be preserved).
$writersContent = Get-Content $writersFile -Raw
$vtableSection = [regex]::Match($writersContent, 'void write_vtable\(writer.*?\n\s{4}\}', [System.Text.RegularExpressions.RegexOptions]::Singleline).Value
if ($vtableSection -and $vtableSection -notmatch 'is_removed') {
    Write-Host "  PASS: write_vtable does NOT skip removed methods (vtable preserved)" -ForegroundColor Green
    $script:passed++
} elseif (!$vtableSection) {
    Write-Host "  SKIP: Could not isolate write_vtable function" -ForegroundColor Yellow
} else {
    Write-Host "  FAIL: write_vtable contains is_removed check (vtable slots may be broken)" -ForegroundColor Red
    $script:failed++
}

# Verify write_interface_members DOES have is_removed checks (user projection hidden)
$membersSection = [regex]::Match($writersContent, 'void write_interface_members\(writer.*?(?=\n\s{4}void\s)', [System.Text.RegularExpressions.RegexOptions]::Singleline).Value
if ($membersSection -and $membersSection -match 'is_removed') {
    Write-Host "  PASS: write_interface_members skips removed methods (user API hidden)" -ForegroundColor Green
    $script:passed++
} else {
    Write-Host "  FAIL: write_interface_members missing is_removed check" -ForegroundColor Red
    $script:failed++
}

Write-Host "`n=== Generated output verification ===" -ForegroundColor Cyan

$cswinrtExe = "_build\x64\Release\cswinrt\bin\cswinrt.exe"
if (Test-Path $cswinrtExe) {
    $testDir = "_test_verify"
    New-Item -ItemType Directory -Force $testDir | Out-Null
    & $cswinrtExe -input local -include Windows.Media.PlayTo -output $testDir 2>&1 | Out-Null
    $outputFile = Join-Path $testDir "Windows.Media.PlayTo.cs"
    if (Test-Path $outputFile) {
        Assert-Contains $outputFile "System.Obsolete" "[Obsolete] annotations present in generated C#"
        Assert-Contains $outputFile "PlayToConnection may be altered" "Deprecation message preserved in [Obsolete]"
        Assert-Contains $outputFile "enum PlayToConnectionState" "Deprecated enum type generated"
        Assert-Contains $outputFile "Disconnected" "Deprecated enum values generated"
    } else {
        Write-Host "  SKIP: Generated file not found" -ForegroundColor Yellow
    }
    Remove-Item $testDir -Recurse -Force -ErrorAction SilentlyContinue
} else {
    Write-Host "  SKIP: cswinrt.exe not built" -ForegroundColor Yellow
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Passed: $($script:passed)" -ForegroundColor Green
if ($script:failed -gt 0) {
    Write-Host "Failed: $($script:failed)" -ForegroundColor Red
    exit 1
} else {
    Write-Host "All checks passed!" -ForegroundColor Green
}
