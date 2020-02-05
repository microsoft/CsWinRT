rem clone TestWinRT and sync to specific commit
@echo off
if not exist TestWinRT\. (
	echo Cloning TestWinRT
	git clone https://github.com/microsoft/TestWinRT
)
pushd TestWinRT
echo Syncing TestWinRT
git pull -f
git reset -q --hard b27c5c43c039dcba55ce5dfb8f14f7a5b7ac1d81
nuget restore
popd
