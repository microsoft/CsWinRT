// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ConsoleAppFramework;
using WindowsRuntime.InteropGenerator.Generation;

args = [@"C:\Users\kythant\staging\debug-repro.zip"];

// Run the interop generator with all parsed arguments
ConsoleApp.Run(args, InteropGenerator.Run);