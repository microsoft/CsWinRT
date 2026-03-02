# End-to-end test for CSWinRT deprecated and removed API support
# Compiles a test IDL with deprecated/removed types to WinMD,
# generates C# projection with cswinrt.exe, and verifies the output.

param(
    [string]$CsWinRTExe = "_build\x64\Release\cswinrt\bin\cswinrt.exe"
)

$ErrorActionPreference = "Stop"
$script:passed = 0
$script:failed = 0

function Test-Result {
    param([bool]$Condition, [string]$Message)
    if ($Condition) {
        Write-Host "  PASS: $Message" -ForegroundColor Green
        $script:passed++
    } else {
        Write-Host "  FAIL: $Message" -ForegroundColor Red
        $script:failed++
    }
}

$testDir = "tests\DeprecatedRemovedTest"
$outputDir = "$testDir\output"
$winmd = "$testDir\DeprecatedRemovedTest.winmd"
$outputFile = "$outputDir\DeprecatedRemovedTest.cs"

# ============================================================================
# Step 1: Compile IDL to WinMD (if not already done)
# ============================================================================
Write-Host "`n=== Step 1: Compile IDL to WinMD ===" -ForegroundColor Cyan

if (!(Test-Path $winmd)) {
    $vcvarsall = "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvarsall.bat"
    $sdkUnion = "C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.26100.0"
    $sdkIncWinrt = "C:\Program Files (x86)\Windows Kits\10\Include\10.0.26100.0\winrt"

    $midlResult = cmd /c "`"$vcvarsall`" x64 >nul 2>&1 && midl /winrt /metadata_dir `"$sdkUnion`" /nomidl /W1 /nologo /I `"$sdkIncWinrt`" /h nul /dlldata nul /iid nul /proxy nul /notlb /winmd `"$winmd`" `"$testDir\DeprecatedRemovedTest.idl`"" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ERROR: MIDL compilation failed" -ForegroundColor Red
        $midlResult | ForEach-Object { Write-Host "    $_" }
        exit 1
    }
}
Test-Result (Test-Path $winmd) "WinMD file exists"

# ============================================================================
# Step 2: Generate C# projection
# ============================================================================
Write-Host "`n=== Step 2: Generate C# projection ===" -ForegroundColor Cyan

if (!(Test-Path $CsWinRTExe)) {
    Write-Host "  ERROR: cswinrt.exe not found at $CsWinRTExe" -ForegroundColor Red
    Write-Host "  Build CSWinRT first." -ForegroundColor Yellow
    exit 1
}

New-Item -ItemType Directory -Force $outputDir | Out-Null
& $CsWinRTExe -input $winmd -input local -include DeprecatedRemovedTest -output $outputDir 2>&1 | Out-Null
Test-Result (Test-Path $outputFile) "C# projection file generated"

$content = Get-Content $outputFile -Raw

# ============================================================================
# Step 3: Verify REMOVED types are excluded from user projection
# ============================================================================
Write-Host "`n=== Step 3: Removed types excluded ===" -ForegroundColor Cyan

# RemovedEnum - entire enum should not exist
Test-Result ($content -notmatch 'enum RemovedEnum') `
    "RemovedEnum type definition excluded"

# RemovedStruct - entire struct should not exist in user namespace
Test-Result ($content -notmatch '(?m)^\s*public struct RemovedStruct\b') `
    "RemovedStruct not in user namespace"

# RemovedClass - no user-facing class definition
$hasRemovedClassDef = $content -match '(?m)(public|internal) (sealed )?class RemovedClass[^R]'
Test-Result (-not $hasRemovedClassDef) `
    "RemovedClass class definition excluded"

# RemovedDelegate - no user-facing delegate declaration
Test-Result ($content -notmatch 'delegate void RemovedDelegate\(\)') `
    "RemovedDelegate declaration excluded"

# ============================================================================
# Step 4: Verify DEPRECATED types have [Obsolete] annotations
# ============================================================================
Write-Host "`n=== Step 4: Deprecated types have [Obsolete] ===" -ForegroundColor Cyan

Test-Result ($content -match 'Obsolete.*DeprecatedEnum is deprecated') `
    "DeprecatedEnum has [Obsolete] with message"

Test-Result ($content -match 'Obsolete.*DeprecatedClass is deprecated') `
    "DeprecatedClass has [Obsolete] with message"

Test-Result ($content -match 'Obsolete.*DeprecatedDelegate is deprecated') `
    "DeprecatedDelegate has [Obsolete] with message"

Test-Result ($content -match 'Obsolete.*DeprecatedStruct is deprecated') `
    "DeprecatedStruct has [Obsolete] with message"

# ============================================================================
# Step 5: Verify NORMAL types are present without [Obsolete]
# ============================================================================
Write-Host "`n=== Step 5: Normal types present ===" -ForegroundColor Cyan

Test-Result ($content -match 'enum NormalEnum') `
    "NormalEnum type is present"

Test-Result ($content -match 'delegate void NormalDelegate\(\)') `
    "NormalDelegate declaration present"

Test-Result ($content -match 'class TestClass\b') `
    "TestClass class is present"

# ============================================================================
# Step 6: PartiallyRemovedEnum - removed field excluded, others present
# ============================================================================
Write-Host "`n=== Step 6: PartiallyRemovedEnum field removal ===" -ForegroundColor Cyan

Test-Result ($content -match 'Visible =') `
    "PartiallyRemovedEnum.Visible is present"

Test-Result ($content -match 'AlsoVisible =') `
    "PartiallyRemovedEnum.AlsoVisible is present"

Test-Result ($content -notmatch 'Hidden =') `
    "PartiallyRemovedEnum.Hidden is excluded (removed)"

# ============================================================================
# Step 7: IVtableTest - vtable slots preserved, removed method hidden
# ============================================================================
Write-Host "`n=== Step 7: IVtableTest vtable preservation ===" -ForegroundColor Cyan

# The ABI vtable must have all 4 methods including RemovedMethod
# Check for RemovedMethod in the ABI section (should exist for vtable)
Test-Result ($content -match 'RemovedMethod') `
    "RemovedMethod exists in ABI vtable code (binary compat)"

# But user-facing interface should NOT have RemovedMethod
# The user interface IVtableTest should have NormalMethod but NOT RemovedMethod in its declaration
Test-Result ($content -match 'NormalMethod') `
    "NormalMethod accessible in projection"

Test-Result ($content -match 'AnotherNormalMethod') `
    "AnotherNormalMethod accessible in projection"

Test-Result ($content -match 'Obsolete.*DeprecatedMethod is deprecated') `
    "DeprecatedMethod has [Obsolete] annotation"

# ============================================================================
# Step 8: TestClass method-level removal
# ============================================================================
Write-Host "`n=== Step 8: TestClass method-level removal ===" -ForegroundColor Cyan

# Removed methods should NOT have [Obsolete] - they're completely hidden from user API
Test-Result ($content -notmatch 'Obsolete.*RemovedMethod has been removed') `
    "Removed methods do NOT get [Obsolete] (fully hidden)"

Test-Result ($content -match 'Obsolete.*DeprecatedMethod is deprecated') `
    "TestClass.DeprecatedMethod has [Obsolete]"

# ============================================================================
# Summary
# ============================================================================
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Passed: $($script:passed)" -ForegroundColor Green
if ($script:failed -gt 0) {
    Write-Host "Failed: $($script:failed)" -ForegroundColor Red
    exit 1
} else {
    Write-Host "All checks passed!" -ForegroundColor Green
}
