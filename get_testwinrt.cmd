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
git pull -f
if ErrorLevel 1 popd & exit /b !ErrorLevel!
git reset -q --hard b27c5c43c039dcba55ce5dfb8f14f7a5b7ac1d81
if ErrorLevel 1 popd & exit /b !ErrorLevel!
where nuget
if %ErrorLevel% equ 0 (
	echo Restoring Nuget
	nuget restore
)
popd
exit /b 0
