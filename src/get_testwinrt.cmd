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
git reset -q --hard 24ce20296dd656813b1857115f4e134eab91c2cf
if ErrorLevel 1 popd & exit /b !ErrorLevel!
echo Restoring Nuget
..\.nuget\nuget.exe restore
popd
exit /b 0