# C#/WinRT Contributor's Guide

Below is our guidance for how to build the repo, report issues, propose new features, and submit contributions via Pull Requests (PRs). 

## Building the C#/WinRT repo

C#/WinRT currently requires the following packages, or newer, to build:

- [Visual Studio 17.0](https://visualstudio.microsoft.com/downloads/)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [nuget.exe 5.9](https://www.nuget.org/downloads)
- Microsoft.WindowsAppSDK.Foundation 1.8.250831001

The [`build.cmd`](src/build.cmd) script takes care of all related configuration steps and is the simplest way to get started building C#/WinRT. It installs prerequisites such as `nuget.exe` and the .NET 6 SDK, configures the environment to use .NET 6 (creating a `global.json` if necessary), builds the compiler, and builds and executes the unit tests. To build C#/WinRT, follow these steps: 

- Open a Visual Studio Developer command prompt pointing at the repo.
- Run `src\build.cmd`. 
- To launch the project in Visual Studio, run `devenv src\cswinrt.sln` from the same command prompt. This will inherit the necessary environment.

**Note:**  By default, the projects for various [Projections](src/Projections) only generate source files for Release configurations, where `cswinrt.exe` can execute in seconds.  To generate sources for the [Projections](src/Projections) projects on Debug configurations, set the project property `GenerateTestProjection` to `true`. This configuration permits a faster inner loop in Visual Studio. In either case, existing projection sources under the "Generated Files" folder will still be compiled into the projection assembly.

### Customizing `build.cmd` options

There are several settings that can be set with `build.cmd` that you might not know about without studying the code. The build settings and defaults are as follows. 

```cmd
build.cmd [Platform] [Configuration] [VersionNumber] [VersionString] [AssemblyVersion]
```

| Parameter | Value(s) |
|-|-|
| Platform | *x64 \| x86 | Default is `x64`
| Configuration | *Release \| Debug | 
| VersionNumber | *0.0.0.0 |
| VersionString | *0.0.0-private.0 |
| AssemblyVersion | *0.0.0.0 |
\*Default value

**Examples**

- Building in Release mode for platform x64, and creates a 0.0.0-private.0.nupkg
    ```cmd
    build.cmd
    ```
- Building in Debug mode for platform x86, and creates a 0.0.0-private.0.nupkg
    ```cmd
    build.cmd x86 Debug
    ```
- Building in Debug mode for platform x64, and creates a 2.0.0-mycswinrt.0.nupkg that has `WinRT.Runtime` with AssemblyVersion of 2.0.0.0
    ```cmd
    build.cmd x64 Debug 2.0.0.0 2.0.0-mycswinrt.0 2.0.0.0
    ```
    This is useful if you want to quickly confirm that your private .nupkg is being used by checking the `WinRT.Runtime` assembly version.  

## Before you start, file an issue

Please follow this simple rule to help us eliminate any unnecessary wasted effort & frustration, and ensure an efficient and effective use of everyone's time - yours, ours, and other community members':

> 👉 If you have a question, think you've discovered an issue, would like to propose a new feature, etc., then find/file an issue **BEFORE** starting work to fix/implement it.

### Search Existing Issues First

Before filing a new issue, search existing open and closed issues first: It is likely someone else has found the problem you're seeing, and someone may be working on or have already contributed a fix!

If no existing item describes your issue/feature, great - please file a new issue:

### File a New Issue

* Don't know whether you're reporting an issue or requesting a feature? File an issue
* Have a question that you don't see answered in docs, videos, etc.? File an issue
* Want to know if we're planning on building a particular feature? File an issue
* Found an existing issue that describes yours? Great - upvote and add additional commentary / info / repro-steps / etc.

### Include Issue Details

**Please include as much information as possible in your issue**. The more information you provide, the more likely your issue/ask will be understood and implemented. Helpful information includes:

* What versions of .NET, C#/WinRT, and the SDK Projection you're using
* What tools and apps you're using (e.g. VS 2019, VSCode, etc.)
* What build of Windows your device is running
* Don't assume we're experts in setting up YOUR environment and don't assume we are experts in YOUR workflow. Teach us to help you!
* **We LOVE detailed repro steps!** What steps do we need to take to reproduce the issue? Assume we love to read repro steps. As much detail as you can stand is probably _barely_ enough detail for us!
* Prefer error message text where possible or screenshots of errors if text cannot be captured
* **If you intend to implement the fix/feature yourself then say so!** If you do not indicate otherwise we will assume that the issue is ours to solve, or may label the issue as `Help-Wanted`.

### DO NOT post "+1" comments

> ⚠ DO NOT post "+1", "me too", or similar comments - they just add noise to an issue.

If you don't have any additional info/context to add but would like to indicate that you're affected by the issue, upvote the original issue by clicking its [+😊] button and hitting 👍 (+1) icon. This way we can actually measure how impactful an issue is.

---

## Development

### Fork, Clone, Branch and Create your PR

Once you've discussed your proposed feature/fix/etc. with a team member, and you've agreed on an approach or a spec has been written and approved, it's time to start development:

1. Fork the repo if you haven't already
1. Clone your fork locally
1. Create & push a feature branch
1. Create a Pull Request
1. Work on your changes

### Code Review

When you'd like the team to take a look, (even if the work is not yet fully complete), mark the PR as 'Ready For Review' so that the team can review your work and provide comments, suggestions, and request changes. It may take several cycles, but the end result will be solid, testable, conformant code that is safe for us to merge.

### Merge

Once your code has been reviewed and approved by the requisite number of team members, it will be merged into the master branch. Once merged, your PR will be automatically closed.

---

## Thank you

Thank you in advance for your contribution! 
