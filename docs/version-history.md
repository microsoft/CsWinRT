## Version History

The following table maps the CsWinRT version used in corresponding .NET SDK and Windows App SDK versions. The Windows SDK package update version is the version of the Microsoft.Windows.SDK.NET.Ref package implicitly referenced by the .NET SDK version(s). 

A minor (or major) version update to the CsWinRT version (e.g., 1.5.0 -> 1.6.1) indicates an Assembly Version change. There are 2 scenarios where app consumers or component authors will be affected by Assembly Version changes: 

1. Windows App SDK Consumers: Referencing or updating to a new Windows App SDK version requires app consumers to upgrade to a .NET SDK version that uses that version of CsWinRT, or alternatively override their Windows SDK package update version if the .NET SDK version is not yet available (using \<WindowsSdkPackageVersion>10.0.\<sdk-version>.\<package-update-version>\</WindowsSdkPackageVersion>).

    For example: Updating from Windows App SDK 1.0.0 to 1.1.0-preview1 involves an AssemblyVersion change. For 1.1.0-preview1, one of the following .NET SDK versions is required at a minimum: 6.0.201, 6.0.103, 5.0.406, or 5.0.212.

2. Component authors: Component authors referencing a new CsWinRT version with an Assembly Version change **AND** an  API surface change for projections need to update all dependent projections to that CsWinRT version.

**Notes for the table below:**

\*Indicates an API surface change for projections.

Windows SDK package update version refers to the assembly file version for **Microsoft.Windows.SDK.NET.dll**. The assembly file version takes the form: `10.0.<windows_build>.<package_update>`.

| CsWinRT version* | Windows SDK <br> package update version | .NET SDK version(s) | Windows App SDK version(s) | 
|-|-|-|-|
| 1.6.3 | 25 | TBA | |
| 1.6.1* | 24 | 6.0.202 <br> 6.0.104 <br> 5.0.407 <br> 5.0.213 | 1.1.0-preview2 <br> 0.8.7 |
| 1.5.0 | 23 | 6.0.201 <br> 6.0.103 <br> 5.0.406 <br> 5.0.212 | 1.1.0-preview1 <br> |
| 1.5.0-prerelease.220124.4 | 23-preview|  N/A | 0.8.7-preview1
| 1.4.1* | 22 | 5.0.210 <br> 5.0.404 <br> 6.0.101 | 0.8.6 |
| 1.4.0-prerelease.211028.1 | 22-preview | N/A | 0.8.6-preview
| 1.3.5 | 21 | 5.0.402 <br> 5.0.208 | 1.0.0 |
| 1.3.3 | 20 | N/A |  |
| 1.3.1 | 19 | 5.0.400 <br> 5.0.303 <br> 5.0.206 | 1.0.0-preview3 |
| 1.3.0 | 18 | 5.0.205 <br> 5.0.302 |  |
| 1.2.6 | 17 | 5.0.204 <br> 5.0.301 |  |
| 1.2.2 | 16 | 5.0.300 | |
| 1.1.4 | 15 | 5.0.202 | |
| 1.1.2 | 14 | 5.0.201 <br> 5.0.104 | |
| 1.1.1 | 13 | 5.0.200 <br> 5.0.103 | |
| 1.1.0 | 12 | 5.0.101 | |
| 1.0.1 | 10 | 5.0.100 | |
