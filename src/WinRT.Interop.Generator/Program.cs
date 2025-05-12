// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using ConsoleAppFramework;
using WindowsRuntime.InteropGenerator.Generation;

// The default timeout is just 5 seconds, but it's reasonable to take more than that.
// Users can cancel running this tool from Visual Studio, so a higher timeout is fine.
ConsoleApp.Timeout = TimeSpan.FromMinutes(1);

// Run the interop generator with all parsed arguments
ConsoleApp.Run(args, InteropGenerator.Run);
