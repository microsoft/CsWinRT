// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Maps Windows Runtime API contracts to their first available Windows SDK platform version.
/// </summary>
internal static class ContractPlatforms
{
    private static readonly FrozenDictionary<string, (int Version, string Platform)[]> Table = Build();

    /// <summary>
    /// Returns the platform version (e.g., "10.0.17763.0") that introduced the given contract version,
    /// or empty string if not found.
    /// </summary>
    public static string GetPlatform(string contractName, int contractVersion)
    {
        if (!Table.TryGetValue(contractName, out (int Version, string Platform)[]? versions))
        {
            return string.Empty;
        }

        // Find the first version >= contractVersion.
        for (int i = 0; i < versions.Length; i++)
        {
            if (versions[i].Version >= contractVersion)
            {
                return versions[i].Platform;
            }
        }
        return string.Empty;
    }

    private static FrozenDictionary<string, (int Version, string Platform)[]> Build()
    {
        Dictionary<string, (int Version, string Platform)[]> t = new(StringComparer.Ordinal)
        {
            ["Windows.AI.MachineLearning.MachineLearningContract"] =
                [(1, "10.0.17763.0"), (2, "10.0.18362.0"), (3, "10.0.19041.0"), (4, "10.0.20348.0"), (5, "10.0.22000.0")],
            ["Windows.AI.MachineLearning.Preview.MachineLearningPreviewContract"] =
                [(1, "10.0.17134.0"), (2, "10.0.17763.0")],
            ["Windows.ApplicationModel.Calls.Background.CallsBackgroundContract"] =
                [(1, "10.0.17763.0"), (2, "10.0.18362.0"), (3, "10.0.20348.0"), (4, "10.0.22621.0")],
            ["Windows.ApplicationModel.Calls.CallsPhoneContract"] =
                [(4, "10.0.17763.0"), (5, "10.0.18362.0"), (6, "10.0.20348.0"), (7, "10.0.22621.0")],
            ["Windows.ApplicationModel.Calls.CallsVoipContract"] =
                [(1, "10.0.10586.0"), (2, "10.0.16299.0"), (3, "10.0.17134.0"), (4, "10.0.17763.0"), (5, "10.0.26100.0")],
            ["Windows.ApplicationModel.CommunicationBlocking.CommunicationBlockingContract"] =
                [(2, "10.0.17763.0")],
            ["Windows.ApplicationModel.SocialInfo.SocialInfoContract"] =
                [(1, "10.0.14393.0"), (2, "10.0.15063.0")],
            ["Windows.ApplicationModel.StartupTaskContract"] =
                [(2, "10.0.16299.0"), (3, "10.0.17134.0")],
            ["Windows.Devices.Custom.CustomDeviceContract"] =
                [(1, "10.0.16299.0")],
            ["Windows.Devices.DevicesLowLevelContract"] =
                [(2, "10.0.14393.0"), (3, "10.0.15063.0")],
            ["Windows.Devices.Printers.PrintersContract"] =
                [(1, "10.0.10586.0")],
            ["Windows.Devices.SmartCards.SmartCardBackgroundTriggerContract"] =
                [(3, "10.0.16299.0")],
            ["Windows.Devices.SmartCards.SmartCardEmulatorContract"] =
                [(5, "10.0.16299.0"), (6, "10.0.17763.0")],
            ["Windows.Foundation.FoundationContract"] =
                [(1, "10.0.10240.0"), (2, "10.0.10586.0"), (3, "10.0.15063.0"), (4, "10.0.19041.0")],
            ["Windows.Foundation.UniversalApiContract"] =
                [(1, "10.0.10240.0"), (2, "10.0.10586.0"), (3, "10.0.14393.0"), (4, "10.0.15063.0"), (5, "10.0.16299.0"),
                 (6, "10.0.17134.0"), (7, "10.0.17763.0"), (8, "10.0.18362.0"), (10, "10.0.19041.0"), (12, "10.0.20348.0"),
                 (14, "10.0.22000.0"), (15, "10.0.22621.0"), (19, "10.0.26100.0")],
            ["Windows.Foundation.VelocityIntegration.VelocityIntegrationContract"] =
                [(1, "10.0.17134.0")],
            ["Windows.Gaming.XboxLive.StorageApiContract"] =
                [(1, "10.0.16299.0")],
            ["Windows.Graphics.Printing3D.Printing3DContract"] =
                [(2, "10.0.10586.0"), (3, "10.0.14393.0"), (4, "10.0.16299.0")],
            ["Windows.Networking.Connectivity.WwanContract"] =
                [(1, "10.0.10240.0"), (2, "10.0.17134.0"), (3, "10.0.26100.0")],
            ["Windows.Networking.Sockets.ControlChannelTriggerContract"] =
                [(3, "10.0.17763.0")],
            ["Windows.Security.Isolation.IsolatedWindowsEnvironmentContract"] =
                [(1, "10.0.19041.0"), (3, "10.0.20348.0"), (4, "10.0.22621.0"), (5, "10.0.26100.0")],
            ["Windows.Services.Maps.GuidanceContract"] =
                [(3, "10.0.17763.0")],
            ["Windows.Services.Maps.LocalSearchContract"] =
                [(4, "10.0.17763.0")],
            ["Windows.Services.Store.StoreContract"] =
                [(1, "10.0.14393.0"), (2, "10.0.15063.0"), (3, "10.0.17134.0"), (4, "10.0.17763.0")],
            ["Windows.Services.TargetedContent.TargetedContentContract"] =
                [(1, "10.0.15063.0")],
            ["Windows.Storage.Provider.CloudFilesContract"] =
                [(4, "10.0.19041.0"), (6, "10.0.20348.0"), (7, "10.0.22621.0")],
            ["Windows.System.Profile.ProfileHardwareTokenContract"] =
                [(1, "10.0.14393.0")],
            ["Windows.System.Profile.ProfileRetailInfoContract"] =
                [(1, "10.0.20348.0")],
            ["Windows.System.Profile.ProfileSharedModeContract"] =
                [(1, "10.0.14393.0"), (2, "10.0.15063.0")],
            ["Windows.System.Profile.SystemManufacturers.SystemManufacturersContract"] =
                [(3, "10.0.17763.0")],
            ["Windows.System.SystemManagementContract"] =
                [(6, "10.0.17763.0"), (7, "10.0.19041.0")],
            ["Windows.UI.UIAutomation.UIAutomationContract"] =
                [(1, "10.0.20348.0"), (2, "10.0.22000.0")],
            ["Windows.UI.ViewManagement.ViewManagementViewScalingContract"] =
                [(1, "10.0.14393.0")],
            ["Windows.UI.Xaml.Core.Direct.XamlDirectContract"] =
                [(1, "10.0.17763.0"), (2, "10.0.18362.0"), (3, "10.0.20348.0"), (5, "10.0.22000.0")],
        };
        return t.ToFrozenDictionary(StringComparer.Ordinal);
    }
}