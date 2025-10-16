# C#/WinRT Contributor's Guide

This guide explains how to report issues, propose features, build the repository, and submit PRs.

1. [Find or File an Issue First](#find-or-file-an-issue-first)
2. [Working on a Fix or Feature After Approvals](#working-on-a-fix-or-feature-after-approvals)
3. [Building the repo](#building-the-repo)

## Find or File an Issue First

Before starting any work to submit a PR, **find or file an issue first**. This ensures no duplicated effort and keeps collaboration efficient.

Check both open and closed issues before filing a new one. If none matches your case, file a new issue.

### When to File an Issue

* Unsure if it’s a bug or feature request
    * → [File an Issue](https://github.com/microsoft/CsWinRT/issues/new/choose)
* Have a question not answered in the docs
    * → [Post a Discussion](https://github.com/microsoft/CsWinRT/discussions)
* Want to propose or confirm a planned feature
    * → [Post a Discussion](https://github.com/microsoft/CsWinRT/discussions)

### What to Describe

* Versions of .NET, C#/WinRT, and SDK projections
* Tools and IDEs used (e.g., VS 2022, VS Code)
* Build version of Windows used
* **Detailed reproduction steps** (most important)
* Full error text or screenshots
* Note if you plan to implement the fix/feature yourself

> [!IMPORTANT]
> **DO NOT** post "+1", "me too", or similar comments ー they just add noise to an issue.
> If you don't have any additional info/context to add but would like to indicate that you're affected by the issue, upvote the original issue by reacting with 😊 or 👍 (+1) emoji and post your context if necessary. This way we can actually measure how impactful an issue is.

## Working on a Fix or Feature After Approvals

### 1. Fork and Branch

After your proposal is discussed and approved:

1. Fork the repo
2. Clone your fork
3. Create and push a feature branch
4. Implement your changes
5. Test your changes on your local
6. Submit a Pull Request (PR)

### 2. Agree with CLA

Most contributions require signing a [Contributor License Agreement (CLA)](https://cla.opensource.microsoft.com), confirming that you own the rights to your contribution and grant Microsoft permission to use it.

When you open a pull request, the CLA bot automatically checks whether a signature is required and updates the PR with a status or comment.
Follow the bot’s instructions if prompted. You only need to complete this process once for all Microsoft repositories that use the CLA bot.

### 3. Code Review

Mark your PR as "Ready for Review" when it’s ready for feedback from the team and the community. It may take several review cycles to ensure correctness and stability.

### 4. Merge

After approvals, your PR will be merged into the main branch and automatically closed.

Thank you in advance for your contribution!

## Building the repo

### Prerequisites

- [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet)

### Build steps

1. Open a Visual Studio Developer command prompt in the repo root.
2. Run a script to install dependencies, set up the environment, build the compiler, and run unit tests.
    ```
    src\build.cmd
    ```
3. (Optional) Open the solution in Visual Studio. This will inherit the necessary environment.
    ```
    devenv src\cswinrt.sln
    ```

> [!NOTE]
> Projection projects under `src/Projections` only generate sources in `Release` by default. To generate them in `Debug`, set the property `GenerateTestProjection` to `true`. This configuration will enable a faster inner loop in Visual Studio. "Generated Files" remain under the Generated Files folder regardless of configuration.

### Customizing `build.cmd` options

You can specify platform, configuration, and version parameters:

```cmd
build.cmd [Platform] [Configuration] [VersionNumber] [VersionString] [AssemblyVersion]
```

| Parameter | Values | Default
|-|-|-|
| Platform | `x64 \| x86` | `x64` |
| Configuration | `Release \| Debug` | `Release` |
| VersionNumber | \* | `0.0.0.0` |
| VersionString | \* | `0.0.0-private.0` |
| AssemblyVersion |\* | `0.0.0.0` |

Building in `Release` for `x64`, and creates `0.0.0-private.0.nupkg`
```cmd
build.cmd
```

Building in `Debug` for `x86`, and creates `0.0.0-private.0.nupkg`
```cmd
build.cmd x86 Debug
```

Building in `Debug` for `x64`, and creates `2.0.0-mycswinrt.0.nupkg` that has `WinRT.Runtime` with AssemblyVersion of `2.0.0.0`
```cmd
build.cmd x64 Debug 2.0.0.0 2.0.0-mycswinrt.0 2.0.0.0
```

This is useful if you want to quickly confirm that your private .nupkg is being used by checking the `WinRT.Runtime` assembly version.
