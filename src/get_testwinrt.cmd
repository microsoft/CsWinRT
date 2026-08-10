rem clone TestWinRT and sync to specific commit
@echo off
if /i "%cswinrt_echo%" == "on" @echo on
set this_dir=%~dp0
setlocal EnableDelayedExpansion
if not exist %this_dir%TestWinRT\. (
	echo Cloning TestWinRT
	git clone https://github.com/microsoft/TestWinRT %this_dir%TestWinRT
	if ErrorLevel 1 popd & exit /b !ErrorLevel!
)
pushd %this_dir%TestWinRT
echo Syncing TestWinRT
git checkout -f master
if ErrorLevel 1 popd & exit /b !ErrorLevel!
git fetch -f
if ErrorLevel 1 popd & exit /b !ErrorLevel!
git reset -q --hard d15a32b2f7c22b022e01e09d893a19fe0b3043f8
if ErrorLevel 1 popd & exit /b !ErrorLevel!
if /i "%~1" == "-skiprestore" goto :done
echo Restoring Nuget
msbuild -t:restore -p:RestorePackagesConfig=true Test.sln
if ErrorLevel 1 popd & exit /b !ErrorLevel!
:done
popd
exit /b 0
