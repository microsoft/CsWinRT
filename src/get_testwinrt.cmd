rem clone TestWinRT and sync to specific commit
@echo off
if /i "%cswinrt_echo%" == "on" @echo on
set this_dir=%~dp0
setlocal EnableDelayedExpansion
if not exist %this_dir%TestWinRT\. (
	echo Cloning TestWinRT
	git clone https://github.com/j0shuams/TestWinRT %this_dir%TestWinRT
	if ErrorLevel 1 popd & exit /b !ErrorLevel!
)
pushd %this_dir%TestWinRT
echo Syncing TestWinRT
git checkout -f toolset-142
if ErrorLevel 1 popd & exit /b !ErrorLevel!
git fetch -f
if ErrorLevel 1 popd & exit /b !ErrorLevel!
git reset -q --hard 322f2aae18ba9a822ac905c7a4711612658bcef2
if ErrorLevel 1 popd & exit /b !ErrorLevel!
echo Restoring Nuget
%this_dir%.nuget\nuget.exe restore
popd
exit /b 0