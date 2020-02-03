rem clone TestWinRT and sync to specific commit
@echo off
if not exist TestWinRT\. (
	echo Cloning TestWinRT
	git clone https://github.com/microsoft/TestWinRT
)
pushd TestWinRT
echo Syncing TestWinRT
git pull -f
git reset -q --hard 2ba4438ff8eaa6481acea03ca2380810667bf1aa
nuget restore
popd
