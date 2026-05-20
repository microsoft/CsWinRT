// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Win32;
using WindowsRuntime.ProjectionWriter.Errors;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Expands a Windows metadata token (e.g. <c>"sdk"</c>, <c>"sdk+"</c>, <c>"local"</c>,
/// <c>"10.0.26100.0"</c>, or a literal path) into the set of <c>.winmd</c> files for that input.
/// </summary>
public static partial class WindowsMetadataExpander
{
    /// <summary>
    /// Matches an SDK version string like <c>"10.0.26100.0"</c> or <c>"10.0.26100.0+"</c>
    /// (the trailing <c>+</c> indicates that extension SDKs should also be included).
    /// </summary>
    [GeneratedRegex(@"^(\d+\.\d+\.\d+\.\d+)\+?$")]
    private static partial Regex SdkVersionRegex { get; }

    /// <summary>
    /// Expands a single Windows metadata token to the resulting set of .winmd file paths
    /// (or directory paths that should be recursively scanned by the writer).
    /// </summary>
    /// <param name="token">The token to expand (path, "local", "sdk", "sdk+", or a version string).</param>
    /// <returns>A list of paths suitable for <see cref="ProjectionWriterOptions.InputPaths"/>.</returns>
    public static List<string> Expand(string token)
    {
        List<string> result = [];

        if (string.IsNullOrWhiteSpace(token))
        {
            return result;
        }

        // Existing file or directory: pass through as-is (the writer handles both).
        if (File.Exists(token) || Directory.Exists(token))
        {
            result.Add(token);
            return result;
        }

        // "local" => %WinDir%\System32\WinMetadata (or SysNative on x86 process)
        if (string.Equals(token, "local", StringComparison.Ordinal))
        {
            string winDir = Environment.GetEnvironmentVariable("windir") ?? @"C:\Windows";
            string subdir = Environment.Is64BitProcess ? "System32" : "SysNative";
            string local = Path.Combine(winDir, subdir, "WinMetadata");

            if (Directory.Exists(local))
            {
                result.Add(local);
            }

            return result;
        }

        // "sdk" / "sdk+" => current Windows SDK version
        // "10.0.X.Y" / "10.0.X.Y+" => specific version
        bool includeExtensions = token.EndsWith('+');
        string sdkVersion = string.Empty;

        if (token is "sdk" or "sdk+")
        {
            sdkVersion = TryGetCurrentSdkVersion();
        }
        else
        {
            Match m = SdkVersionRegex.Match(token);

            if (m.Success)
            {
                sdkVersion = m.Groups[1].Value;
            }
        }

        if (!string.IsNullOrEmpty(sdkVersion))
        {
            string sdkPath = TryGetSdkPath();

            if (string.IsNullOrEmpty(sdkPath))
            {
                throw WellKnownProjectionWriterExceptions.WindowsSdkNotFound();
            }

            string platformXml = Path.Combine(sdkPath, "Platforms", "UAP", sdkVersion, "Platform.xml");
            AddFilesFromPlatformXml(result, sdkVersion, platformXml, sdkPath);

            if (includeExtensions)
            {
                string extensionSdks = Path.Combine(sdkPath, "Extension SDKs");

                if (Directory.Exists(extensionSdks))
                {
                    foreach (string item in Directory.EnumerateDirectories(extensionSdks))
                    {
                        string xml = Path.Combine(item, sdkVersion, "SDKManifest.xml");

                        if (File.Exists(xml))
                        {
                            AddFilesFromPlatformXml(result, sdkVersion, xml, sdkPath);
                        }
                    }
                }
            }

            return result;
        }

        // No expansion matched - return the token as-is so the writer's "file not found" error
        // surfaces with the original token in the message.
        result.Add(token);
        return result;
    }
    private static string TryGetSdkPath()
    {
        if (!OperatingSystem.IsWindows())
        {
            return string.Empty;
        }

        // HKLM\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10
        // Try the WOW64 view first (the SDK installer registers under the 32-bit hive), then default view.
        const string subKey = @"SOFTWARE\Microsoft\Windows Kits\Installed Roots";
        try
        {
            using RegistryKey? wow = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subKey);

            if (wow?.GetValue("KitsRoot10") is string p1 && !string.IsNullOrEmpty(p1))
            {
                return p1;
            }

            using RegistryKey? def = Registry.LocalMachine.OpenSubKey(subKey);

            if (def?.GetValue("KitsRoot10") is string p2 && !string.IsNullOrEmpty(p2))
            {
                return p2;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            // Both views can fail with permission errors on hardened machines, or with I/O errors
            // when the registry hive is being modified concurrently by an installer. Treat any of
            // those as "no SDK detected" and let the caller fall back to the path-not-found error.
        }

        return string.Empty;
    }

    private static string TryGetCurrentSdkVersion()
    {
        string sdkPath = TryGetSdkPath();

        if (string.IsNullOrEmpty(sdkPath))
        {
            return string.Empty;
        }

        string platforms = Path.Combine(sdkPath, "Platforms", "UAP");

        if (!Directory.Exists(platforms))
        {
            return string.Empty;
        }

        // Find the highest installed version that has a Platform.xml file.
        Version best = new(0, 0, 0, 0);
        string bestStr = string.Empty;
        foreach (string dir in Directory.EnumerateDirectories(platforms))
        {
            string name = Path.GetFileName(dir);

            if (!Version.TryParse(name, out Version? v))
            {
                continue;
            }

            string xml = Path.Combine(dir, "Platform.xml");

            if (!File.Exists(xml))
            {
                continue;
            }

            if (v > best)
            {
                best = v;
                bestStr = name;
            }
        }
        return bestStr;
    }
    private static void AddFilesFromPlatformXml(List<string> result, string sdkVersion, string xmlPath, string sdkPath)
    {
        if (!File.Exists(xmlPath))
        {
            throw WellKnownProjectionWriterExceptions.CannotReadWindowsSdkXml(xmlPath);
        }

        XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Ignore, IgnoreWhitespace = true };
        using XmlReader reader = XmlReader.Create(xmlPath, settings);
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "ApiContract")
            {
                continue;
            }

            string? name = reader.GetAttribute("name");
            string? version = reader.GetAttribute("version");

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
            {
                continue;
            }

            // <KitsRoot10>\References\<sdk_version>\<contract_name>\<contract_version>\<contract_name>.winmd
            string winmd = Path.Combine(sdkPath, "References", sdkVersion, name, version, name + ".winmd");
            result.Add(winmd);
        }
    }
}
