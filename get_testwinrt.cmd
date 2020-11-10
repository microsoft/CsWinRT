rem clone TestWinRT and sync to specific commit
@echo off
setlocal EnableDelayedExpansion
if not exist TestWinRT\. (
	echo Cloning TestWinRT
	git clone https://github.com/microsoft/TestWinRT
	if ErrorLevel 1 popd & exit /b !ErrorLevel!
)
pushd TestWinRT
echo Syncing TestWinRT
git checkout -f master
if ErrorLevel 1 popd & exit /b !ErrorLevel!
git fetch -f
if ErrorLevel 1 popd & exit /b !ErrorLevel!
git reset -q --hard 5df065fd3bccf314079e39e1bf2644fd239615c1
if ErrorLevel 1 popd & exit /b !ErrorLevel!
echo Restoring Nuget
..\.nuget\nuget.exe restore
popd
exit /b 0
