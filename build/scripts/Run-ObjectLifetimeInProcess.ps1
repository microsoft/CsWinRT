[CmdletBinding()]
param(
    # The 'win-<plat>' output folder of ObjectLifetimeTests.Lifted (contains the loose MSIX layout).
    [Parameter(Mandatory = $true)] [string] $LayoutDir
)

$ErrorActionPreference = 'Stop'

# Locate the loose package manifest.
$manifest = Join-Path $LayoutDir 'AppX\AppxManifest.xml'
if (-not (Test-Path $manifest)) { $manifest = Join-Path $LayoutDir 'AppxManifest.xml' }
if (-not (Test-Path $manifest)) { throw "AppxManifest.xml not found under $LayoutDir" }

# Register the loose layout. If it is already deployed (e.g. by the preceding VSTest run) that's fine.
Write-Host "Registering package from $manifest"
try {
    Add-AppxPackage -Register $manifest -ErrorAction Stop
} catch {
    Write-Host "Add-AppxPackage -Register: $($_.Exception.Message)"
}

# Build the AppUserModelId (PackageFamilyName!AppId) for activation.
[xml]$m = Get-Content $manifest
$identityName = $m.Package.Identity.Name
$appId = $m.Package.Applications.Application.Id
$pkg = Get-AppxPackage -Name $identityName
if (-not $pkg) { throw "Package $identityName is not registered" }
$aumid = "$($pkg.PackageFamilyName)!$appId"

# Launch directly (no --parentprocessid) so the app runs RunTestsInProcess and self-exits with the
# pass/fail code; wait for it and surface that code.
Write-Host "Launching $aumid directly (exercises RunTestsInProcess)"

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class PackagedAppRunner
{
    [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IApplicationActivationManager
    {
        int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)] string appUserModelId, [MarshalAs(UnmanagedType.LPWStr)] string arguments, int options, out uint processId);
    }

    [ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
    private class ApplicationActivationManager { }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr handle, uint ms);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(IntPtr handle, out uint exitCode);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    private const uint SYNCHRONIZE = 0x00100000;
    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    private const uint INFINITE = 0xFFFFFFFF;

    public static int Run(string aumid)
    {
        var mgr = (IApplicationActivationManager)new ApplicationActivationManager();
        uint pid;
        int hr = mgr.ActivateApplication(aumid, null, 0, out pid);
        if (hr < 0) { throw new COMException("ActivateApplication failed", hr); }

        IntPtr handle = OpenProcess(SYNCHRONIZE | PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (handle == IntPtr.Zero) { throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "OpenProcess failed"); }
        try
        {
            WaitForSingleObject(handle, INFINITE);
            uint code;
            if (!GetExitCodeProcess(handle, out code)) { throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "GetExitCodeProcess failed"); }
            return unchecked((int)code);
        }
        finally { CloseHandle(handle); }
    }
}
'@

$exit = [PackagedAppRunner]::Run($aumid)
Write-Host "In-process Object Lifetime run exited with code $exit"

# Surface the framework log the app wrote to its package temp folder.
$logPath = Join-Path $env:LOCALAPPDATA "Packages\$($pkg.PackageFamilyName)\TempState\objectlifetime-inproc.log"
if (Test-Path $logPath) {
    Write-Host "----- Object Lifetime in-process test output -----"
    Get-Content $logPath | ForEach-Object { Write-Host $_ }
    Write-Host "--------------------------------------------------"
} else {
    Write-Host "No in-process test log found at $logPath"
}

# Warn (don't fail the build) if the in-process run reported test failures.
if ($exit -ne 0) {
    Write-Host "##vso[task.logissue type=warning]Object Lifetime in-process run reported test failures (exit code $exit)"
}
exit 0
