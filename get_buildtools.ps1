function LaunchSetupAndWait([string]$exePath, [string[]]$ArgumentList)
{
    Write-Host -NoNewline "Launching $exePath... "
    $c = Start-Process $exePath -Wait -PassThru -ArgumentList $ArgumentList
    $exitcode = $c.ExitCode
    if ($exitCode -eq 0)
    {
        Write-Host -ForegroundColor Green Done.
    }
    else
    {
        Write-Host -ForegroundColor Red "Error $exitcode"
    }
    return ($exitCode -eq 0)
}

function Download([uri]$Uri, [string]$OutFile)
{
    Write-Host -NoNewline "Downloading $OutFile... "
    Invoke-WebRequest -Uri $Uri -OutFile $OutFile
    Write-Host -ForegroundColor Green Done.
}

function Install-MSBuild
{
    # Install Visual Studio Build Tools 16.8
    $buildtools_dir = join-path $pwd ".buildtools"
    New-Item -Path $buildtools_dir -Force -ItemType 'Directory' | Out-Null
    $vs_buildtools = join-path $pwd "vs_buildtools.exe"
    Download -Uri https://download.visualstudio.microsoft.com/download/pr/ecb3860e-5c66-4a3f-8acf-ef190d5f9a96/18162a4d36635d0958bf56654d4a03b211dcd8474b3a4036c8a0a0fb6a0eb053/vs_BuildTools.exe -OutFile $vs_buildtools
    $installed = LaunchSetupAndWait $vs_buildtools -ArgumentList "--quiet --wait --norestart --force --installPath $buildtools_dir --add Microsoft.VisualStudio.Workload.VCTools --add Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools --add Microsoft.VisualStudio.Workload.UniversalBuildTools --add Microsoft.VisualStudio.ComponentGroup.UWP.VC.BuildTools"
    if (!$installed)
    {
        # try to figure out what went wrong
        $latestClientLog = (Get-ChildItem $env:temp\dd_client_* | Sort-Object -Property LastWriteTime -Descending)[0]
        $log = (Get-Content $latestClientLog)
        $errorLines = ($log -like '*Error :*')
        if ($errorLines)
        {
            Write-Host $errorLines
        }
        Write-Host "For more information see %temp%\dd_*.txt"
    }
}

Install-MSBuild
