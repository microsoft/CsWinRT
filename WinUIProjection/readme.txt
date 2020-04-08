This project simply generates and compiles a complete projection of Windows SDK and WinUI metadata.

With a release cswinrt.exe, it takes < 1s to generate all of the projection sources.  
With debug, it takes a minute.  So only release builds create the projection by default.  
This can be overridden by explicitly setting the BuildTestProjection property.

This project assumes a private nuget source has been created, pointing to:
https://microsoft.pkgs.visualstudio.com/_packaging/WinUI-Xaml-CI@IXP/nuget/v3/index.json

For usability (until the cswinrt nuget has msbuild support), this and the UnitTest projects
both make use of Directory.Build.* files to create the projection, stage binaries, etc.

WinUITest uses a response file to generate the projection, which can be supplied as a
debugging parameter to the cswinrt project.