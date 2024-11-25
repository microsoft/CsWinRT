@echo off

set this_dir=%~dp0

rd /q/s "%this_dir%\C++ Components\Alpha\Generated Files"
rd /q/s "%this_dir%\C++ Components\Alpha\x64"
rd /q/s "%this_dir%\C++ Components\Alpha\x86"

rd /q/s "%this_dir%\C++ Components\Beta\Generated Files"
rd /q/s "%this_dir%\C++ Components\Beta\x64"
rd /q/s "%this_dir%\C++ Components\Beta\x86"

rd /q/s "%this_dir%\C++ Components\Gamma\Generated Files"
rd /q/s "%this_dir%\C++ Components\Gamma\x64"
rd /q/s "%this_dir%\C++ Components\Gamma\x86"

rd /q/s "%this_dir%\Net8App\bin"
rd /q/s "%this_dir%\Net8App\obj"

rd /q/s "%this_dir%\NetCore3App\bin"
rd /q/s "%this_dir%\NetCore3App\obj"

rd /q/s "%this_dir%\TestEmbeddedLibrary\bin"
rd /q/s "%this_dir%\TestEmbeddedLibrary\obj"

rd /q/s "%this_dir%\x64"
rd /q/s "%this_dir%\x86"

rd /q/s "%this_dir%\UnitTestEmbedded\bin"
rd /q/s "%this_dir%\UnitTestEmbedded\obj"

msbuild %this_dir%\TestEmbedded.sln /t:restore

nuget restore TestEmbedded.sln