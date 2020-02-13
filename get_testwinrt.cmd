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
git reset -q --hard b9b413dba2c1058b87400ea1b080f3a1f3b7cbea
if ErrorLevel 1 popd & exit /b !ErrorLevel!
where nuget
if %ErrorLevel% equ 0 (
	echo Restoring Nuget
	nuget restore
)
popd
exit /b 0
