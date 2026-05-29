# End-to-end test for CSWinRT deprecated and removed API support
# Compiles a test IDL with deprecated/removed types to WinMD,
# generates C# projection with cswinrt.exe, and verifies the output.
#
# Regression coverage for gaps found during functional testing:
#   - Commit 4c180d3f: Member-level deprecation on [default_interface] runtimeclass
#   - Commit ed2ebda2: Type-level ABI code referencing removed projected types (CS0234)
#   - Commit b4ba8896: MIDL attribute on getter/add methods, not Property/Event rows
#
# Usage: pwsh -File run_test.ps1 [-CsWinRTExe path\to\cswinrt.exe]

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
# Step 3: Verify REMOVED types are excluded from projection
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

# IRemovedInterface - projected interface should NOT exist
# (Regression: commit ed2ebda2 — previously generated empty interface with broken ABI reference)
Test-Result ($content -notmatch 'interface IRemovedInterface') `
    "IRemovedInterface projected interface excluded"

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

Test-Result ($content -match 'Obsolete.*IDeprecatedInterface is deprecated') `
    "IDeprecatedInterface has [Obsolete] with message"

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

Test-Result ($content -match 'NormalMethod') `
    "NormalMethod accessible in projection"

Test-Result ($content -match 'AnotherNormalMethod') `
    "AnotherNormalMethod accessible in projection"

Test-Result ($content -match 'Obsolete.*DeprecatedMethod is deprecated') `
    "DeprecatedMethod has [Obsolete] annotation"

# ============================================================================
# Step 8: TestClass method-level removal and deprecation
# ============================================================================
Write-Host "`n=== Step 8: TestClass method-level removal ===" -ForegroundColor Cyan

# Removed methods should NOT have [Obsolete] - they're completely hidden from user API
Test-Result ($content -notmatch 'Obsolete.*RemovedMethod has been removed') `
    "Removed methods do NOT get [Obsolete] (fully hidden)"

Test-Result ($content -match 'Obsolete.*DeprecatedMethod is deprecated') `
    "TestClass.DeprecatedMethod has [Obsolete]"

# ============================================================================
# Step 9: TestClass PROPERTY deprecation/removal
# (Regression: commit b4ba8896 — MIDL puts DeprecatedAttr on getter, not Property row)
# ============================================================================
Write-Host "`n=== Step 9: TestClass property deprecation/removal ===" -ForegroundColor Cyan

# NormalProp should be present on the projected class
Test-Result ($content -match '(?m)public string NormalProp\b') `
    "NormalProp is present on projected TestClass"

# DeprecatedProp should have [Obsolete] on the projected class
Test-Result ($content -match 'Obsolete.*DeprecatedProp is deprecated') `
    "DeprecatedProp has [Obsolete] on projected TestClass"

# RemovedProp should NOT be on the projected class
# (Regression: previously not removed because is_removed() checked Property row, not getter)
$hasRemovedPropOnClass = $content -match '(?m)public string RemovedProp\b'
Test-Result (-not $hasRemovedPropOnClass) `
    "RemovedProp excluded from projected TestClass"

# ============================================================================
# Step 10: TestClass EVENT deprecation/removal
# (Regression: commit b4ba8896 — MIDL puts DeprecatedAttr on add method, not Event row)
# ============================================================================
Write-Host "`n=== Step 10: TestClass event deprecation/removal ===" -ForegroundColor Cyan

# NormalEvent should be present on the projected class
Test-Result ($content -match '(?m)public event.*NormalEvent\b') `
    "NormalEvent is present on projected TestClass"

# DeprecatedEvent should have [Obsolete] on the projected class
Test-Result ($content -match 'Obsolete.*DeprecatedEvent is deprecated') `
    "DeprecatedEvent has [Obsolete] on projected TestClass"

# RemovedEvent should NOT be on the projected class
$hasRemovedEventOnClass = $content -match '(?m)public event.*RemovedEvent\b'
Test-Result (-not $hasRemovedEventOnClass) `
    "RemovedEvent excluded from projected TestClass"

# ============================================================================
# Step 11: Interface member signatures (ITestClass)
# (Regression: commit 4c180d3f — write_interface_member_signatures not filtering)
# ============================================================================
Write-Host "`n=== Step 11: Interface member signatures ===" -ForegroundColor Cyan

# Extract internal interface ITestClass body using brace-counting (avoid early match on { get; })
$lines = $content -split "`n"
$inIface = $false; $depth = 0; $ifaceLines = @()
foreach ($line in $lines) {
    if ($line -match 'internal interface ITestClass\b') { $inIface = $true; $depth = 0 }
    if ($inIface) {
        $depth += ([regex]::Matches($line, '\{')).Count
        $depth -= ([regex]::Matches($line, '\}')).Count
        $ifaceLines += $line
        if ($depth -le 0 -and $ifaceLines.Count -gt 1) { break }
    }
}
$ifaceBody = $ifaceLines -join "`n"
if ($ifaceBody) {

    Test-Result ($ifaceBody -match 'void ActiveMethod\(\)') `
        "ITestClass interface has ActiveMethod"

    Test-Result ($ifaceBody -notmatch 'void RemovedMethod\(\)') `
        "ITestClass interface excludes RemovedMethod"

    Test-Result ($ifaceBody -notmatch 'RemovedProp') `
        "ITestClass interface excludes RemovedProp"

    Test-Result ($ifaceBody -notmatch 'RemovedEvent') `
        "ITestClass interface excludes RemovedEvent"

    Test-Result ($ifaceBody -match 'Obsolete.*DeprecatedMethod') `
        "ITestClass interface has [Obsolete] on DeprecatedMethod"

    Test-Result ($ifaceBody -match 'Obsolete.*DeprecatedProp') `
        "ITestClass interface has [Obsolete] on DeprecatedProp"

    Test-Result ($ifaceBody -match 'Obsolete.*DeprecatedEvent') `
        "ITestClass interface has [Obsolete] on DeprecatedEvent"
} else {
    Write-Host "  FAIL: Could not find internal interface ITestClass" -ForegroundColor Red
    $script:failed += 7
}

# ============================================================================
# Step 12: ABI code integrity for removed types
# (Regression: commit ed2ebda2 — ABI code referenced removed projected types)
# ============================================================================
Write-Host "`n=== Step 12: ABI code integrity for removed types ===" -ForegroundColor Cyan

# ABI namespace should NOT reference IRemovedClass or IRemovedInterface as helper types
# These would cause CS0234 if generated because the projected types don't exist
Test-Result ($content -notmatch 'WindowsRuntimeHelperType.*IRemovedClass') `
    "No WindowsRuntimeHelperType reference to IRemovedClass"

Test-Result ($content -notmatch 'WindowsRuntimeHelperType.*IRemovedInterface') `
    "No WindowsRuntimeHelperType reference to IRemovedInterface"

# No RcwFactoryAttribute for removed class
Test-Result ($content -notmatch 'RemovedClassRcwFactoryAttribute') `
    "No RcwFactoryAttribute for RemovedClass"

# ============================================================================
# Step 13: Read-write property deprecation/removal
# ============================================================================
Write-Host "`n=== Step 13: Read-write property deprecation/removal ===" -ForegroundColor Cyan

Test-Result ($content -match '(?m)public string WritableProp\b') `
    "WritableProp is present on projected TestClass"

Test-Result ($content -match 'Obsolete.*WritableDeprecatedProp is deprecated') `
    "WritableDeprecatedProp has [Obsolete] on projected TestClass"

$hasWritableRemovedProp = $content -match '(?m)public string WritableRemovedProp\b'
Test-Result (-not $hasWritableRemovedProp) `
    "WritableRemovedProp excluded from projected TestClass"

# Check setter exists for writable prop
Test-Result ($content -match 'set_WritableProp') `
    "WritableProp setter path exists in ABI"

# ============================================================================
# Step 14: Static property deprecation/removal
# ============================================================================
Write-Host "`n=== Step 14: Static property deprecation/removal ===" -ForegroundColor Cyan

Test-Result ($content -match '(?m)(public )?static string StaticProp\b') `
    "StaticProp is present on projected TestClass"

Test-Result ($content -match 'Obsolete.*StaticDeprecatedProp is deprecated') `
    "StaticDeprecatedProp has [Obsolete] on projected TestClass"

$hasStaticRemovedProp = $content -match '(?m)(public )?static string StaticRemovedProp\b'
Test-Result (-not $hasStaticRemovedProp) `
    "StaticRemovedProp excluded from projected TestClass"

# ============================================================================
# Step 15: Constructor deprecation/removal
# ============================================================================
Write-Host "`n=== Step 15: Constructor deprecation/removal ===" -ForegroundColor Cyan

# Default constructor should be present
Test-Result ($content -match 'TestClass\(\)') `
    "Default constructor TestClass() is present"

# Deprecated constructor should have [Obsolete]
Test-Result ($content -match 'Obsolete.*Constructor with name is deprecated') `
    "Deprecated constructor has [Obsolete] annotation"

# Removed constructor should NOT be present in user-facing code
$hasRemovedCtor = $content -match 'Obsolete.*Constructor with name and config has been removed'
$hasRemovedCtorDef = $content -match 'TestClass\(string\s+\w+,\s*int\s+\w+\)'
Test-Result (-not $hasRemovedCtor -and -not $hasRemovedCtorDef) `
    "Removed constructor excluded from projected TestClass"

# ============================================================================
# Step 16: Interface checks for new constructs
# ============================================================================
Write-Host "`n=== Step 16: Interface checks for new constructs ===" -ForegroundColor Cyan

if ($ifaceBody) {
    Test-Result ($ifaceBody -notmatch 'WritableRemovedProp') `
        "ITestClass interface excludes WritableRemovedProp"

    Test-Result ($ifaceBody -match 'Obsolete.*WritableDeprecatedProp') `
        "ITestClass interface has [Obsolete] on WritableDeprecatedProp"

    Test-Result ($ifaceBody -match 'WritableProp') `
        "ITestClass interface has WritableProp"
} else {
    Write-Host "  SKIP: ITestClass not found for new construct checks" -ForegroundColor Yellow
}

# ============================================================================
# Step 17: Compile generated C# code
# (Ultimate regression test — catches any ABI reference to missing types)
# ============================================================================
Write-Host "`n=== Step 17: Compile generated C# code ===" -ForegroundColor Cyan

$compileDir = "$testDir\compile_test"
if (Test-Path $compileDir) { Remove-Item $compileDir -Recurse -Force }
New-Item -ItemType Directory -Force $compileDir | Out-Null

# Create a minimal .csproj that compiles the generated code
$csproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Library</OutputType>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <CsWinRTEnabled>false</CsWinRTEnabled>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <NoWarn>CS0618;CS0612</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="..\output\*.cs" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include="WinRT.Runtime">
      <HintPath>..\..\..\src\WinRT.Runtime\bin\Release\net8.0\WinRT.Runtime.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
"@
$csproj | Set-Content "$compileDir\CompileTest.csproj"

$buildResult = dotnet build "$compileDir\CompileTest.csproj" --nologo 2>&1
$buildOutput = $buildResult | Out-String
$buildSuccess = $buildOutput -match 'Build succeeded'
$hasErrors = $buildOutput -match 'error CS'

Test-Result $buildSuccess `
    "Generated C# code compiles without errors"

if (-not $buildSuccess -and $hasErrors) {
    # Show the specific errors
    $buildResult | Select-String "error CS" | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow }
}

# Cleanup compile test
Remove-Item $compileDir -Recurse -Force -ErrorAction SilentlyContinue

# ============================================================================
# Step 18: Component generation excludes removed classes
# ============================================================================
Write-Host "`n=== Step 18: Component activation factory excludes removed classes ===" -ForegroundColor Cyan

$componentDir = "$testDir\component_output"
New-Item -ItemType Directory -Force $componentDir | Out-Null
& $CsWinRTExe -input $winmd -input local -include DeprecatedRemovedTest -output $componentDir -component 2>&1 | Out-Null

$moduleFile = "$componentDir\WinRT_Module.cs"
Test-Result (Test-Path $moduleFile) `
    "WinRT_Module.cs generated in component mode"

if (Test-Path $moduleFile) {
    $moduleContent = Get-Content $moduleFile -Raw

    # RemovedClass must NOT appear in the activation factory
    Test-Result ($moduleContent -notmatch 'RemovedClass') `
        "RemovedClass excluded from component activation factory"

    # Normal and deprecated classes MUST still appear
    Test-Result ($moduleContent -match 'TestClass') `
        "TestClass present in component activation factory"
    Test-Result ($moduleContent -match 'DeprecatedClass') `
        "DeprecatedClass present in component activation factory"
}

# Cleanup component output
Remove-Item $componentDir -Recurse -Force -ErrorAction SilentlyContinue

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
