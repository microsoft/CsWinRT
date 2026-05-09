// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Maps Windows Runtime API contracts to their first available Windows SDK platform version.
/// Mirrors the C++ <c>contract_mappings</c> table in <c>helpers.h</c>.
/// </summary>
internal static class ContractPlatforms
{
    private static readonly Dictionary<string, List<(int Version, string Platform)>> s_table = Build();

    /// <summary>
    /// Returns the platform version (e.g., "10.0.17763.0") that introduced the given contract version,
    /// or empty string if not found.
    /// </summary>
    public static string GetPlatform(string contractName, int contractVersion)
    {
        if (!s_table.TryGetValue(contractName, out List<(int Version, string Platform)>? versions))
        {
            return string.Empty;
        }
        // Find the first version >= contractVersion (mirrors std::lower_bound)
        for (int i = 0; i < versions.Count; i++)
        {
            if (versions[i].Version >= contractVersion)
            {
                return versions[i].Platform;
            }
        }
        return string.Empty;
    }

    private static Dictionary<string, List<(int, string)>> Build()
    {
        Dictionary<string, List<(int, string)>> t = new();

        void Add(string name, params (int v, string p)[] vs)
        {
            List<(int, string)> list = new();
            foreach (var (v, p) in vs) { list.Add((v, p)); }
            t[name] = list;
        }

        Add("Windows.AI.MachineLearning.MachineLearningContract",
            (1, "10.0.17763.0"), (2, "10.0.18362.0"), (3, "10.0.19041.0"), (4, "10.0.20348.0"), (5, "10.0.22000.0"));
        Add("Windows.AI.MachineLearning.Preview.MachineLearningPreviewContract",
            (1, "10.0.17134.0"), (2, "10.0.17763.0"));
        Add("Windows.ApplicationModel.Calls.Background.CallsBackgroundContract",
            (1, "10.0.17763.0"), (2, "10.0.18362.0"), (3, "10.0.20348.0"), (4, "10.0.22621.0"));
        Add("Windows.ApplicationModel.Calls.CallsPhoneContract",
            (4, "10.0.17763.0"), (5, "10.0.18362.0"), (6, "10.0.20348.0"), (7, "10.0.22621.0"));
        Add("Windows.ApplicationModel.Calls.CallsVoipContract",
            (1, "10.0.10586.0"), (2, "10.0.16299.0"), (3, "10.0.17134.0"), (4, "10.0.17763.0"), (5, "10.0.26100.0"));
        Add("Windows.ApplicationModel.CommunicationBlocking.CommunicationBlockingContract",
            (2, "10.0.17763.0"));
        Add("Windows.ApplicationModel.SocialInfo.SocialInfoContract",
            (1, "10.0.14393.0"), (2, "10.0.15063.0"));
        Add("Windows.ApplicationModel.StartupTaskContract",
            (2, "10.0.16299.0"), (3, "10.0.17134.0"));
        Add("Windows.Devices.Custom.CustomDeviceContract",
            (1, "10.0.16299.0"));
        Add("Windows.Devices.DevicesLowLevelContract",
            (2, "10.0.14393.0"), (3, "10.0.15063.0"));
        Add("Windows.Devices.Printers.PrintersContract",
            (1, "10.0.10586.0"));
        Add("Windows.Devices.SmartCards.SmartCardBackgroundTriggerContract",
            (3, "10.0.16299.0"));
        Add("Windows.Devices.SmartCards.SmartCardEmulatorContract",
            (5, "10.0.16299.0"), (6, "10.0.17763.0"));
        Add("Windows.Foundation.FoundationContract",
            (1, "10.0.10240.0"), (2, "10.0.10586.0"), (3, "10.0.15063.0"), (4, "10.0.19041.0"));
        Add("Windows.Foundation.UniversalApiContract",
            (1, "10.0.10240.0"), (2, "10.0.10586.0"), (3, "10.0.14393.0"), (4, "10.0.15063.0"), (5, "10.0.16299.0"),
            (6, "10.0.17134.0"), (7, "10.0.17763.0"), (8, "10.0.18362.0"), (10, "10.0.19041.0"), (12, "10.0.20348.0"),
            (14, "10.0.22000.0"), (15, "10.0.22621.0"), (19, "10.0.26100.0"));
        Add("Windows.Foundation.VelocityIntegration.VelocityIntegrationContract",
            (1, "10.0.17134.0"));
        Add("Windows.Gaming.XboxLive.StorageApiContract",
            (1, "10.0.16299.0"));
        Add("Windows.Graphics.Printing3D.Printing3DContract",
            (2, "10.0.10586.0"), (3, "10.0.14393.0"), (4, "10.0.16299.0"));
        Add("Windows.Networking.Connectivity.WwanContract",
            (1, "10.0.10240.0"), (2, "10.0.17134.0"), (3, "10.0.26100.0"));
        Add("Windows.Networking.Sockets.ControlChannelTriggerContract",
            (3, "10.0.17763.0"));
        Add("Windows.Security.Isolation.IsolatedWindowsEnvironmentContract",
            (1, "10.0.19041.0"), (3, "10.0.20348.0"), (4, "10.0.22621.0"), (5, "10.0.26100.0"));
        Add("Windows.Services.Maps.GuidanceContract",
            (3, "10.0.17763.0"));
        Add("Windows.Services.Maps.LocalSearchContract",
            (4, "10.0.17763.0"));
        Add("Windows.Services.Store.StoreContract",
            (1, "10.0.14393.0"), (2, "10.0.15063.0"), (3, "10.0.17134.0"), (4, "10.0.17763.0"));
        Add("Windows.Services.TargetedContent.TargetedContentContract",
            (1, "10.0.15063.0"));
        Add("Windows.Storage.Provider.CloudFilesContract",
            (4, "10.0.19041.0"), (6, "10.0.20348.0"), (7, "10.0.22621.0"));
        Add("Windows.System.Profile.ProfileHardwareTokenContract",
            (1, "10.0.14393.0"));
        Add("Windows.System.Profile.ProfileRetailInfoContract",
            (1, "10.0.20348.0"));
        Add("Windows.System.Profile.ProfileSharedModeContract",
            (1, "10.0.14393.0"), (2, "10.0.15063.0"));
        Add("Windows.System.Profile.SystemManufacturers.SystemManufacturersContract",
            (3, "10.0.17763.0"));
        Add("Windows.System.SystemManagementContract",
            (6, "10.0.17763.0"), (7, "10.0.19041.0"));
        Add("Windows.UI.UIAutomation.UIAutomationContract",
            (1, "10.0.20348.0"), (2, "10.0.22000.0"));
        Add("Windows.UI.ViewManagement.ViewManagementViewScalingContract",
            (1, "10.0.14393.0"));
        Add("Windows.UI.Xaml.Core.Direct.XamlDirectContract",
            (1, "10.0.17763.0"), (2, "10.0.18362.0"), (3, "10.0.20348.0"), (5, "10.0.22000.0"));

        return t;
    }
}

/// <summary>
/// Static lookup for namespaces with addition files. Mirrors C++ <c>has_addition_to_type</c>.
/// </summary>
internal static class AdditionTypes
{
    private static readonly Dictionary<string, HashSet<string>> s_table = new(System.StringComparer.Ordinal)
    {
        { "Microsoft.UI.Xaml", new(System.StringComparer.Ordinal) { "Thickness" } },
        { "Microsoft.UI.Xaml.Controls.Primitives", new(System.StringComparer.Ordinal) { "GeneratorPosition" } },
        { "Microsoft.UI.Xaml.Media", new(System.StringComparer.Ordinal) { "Matrix" } },
        { "Microsoft.UI.Xaml.Media.Animation", new(System.StringComparer.Ordinal) { "KeyTime" } },
        { "Windows.UI", new(System.StringComparer.Ordinal) { "Color" } },
        { "Windows.UI.Xaml", new(System.StringComparer.Ordinal) { "Thickness" } },
        { "Windows.UI.Xaml.Controls.Primitives", new(System.StringComparer.Ordinal) { "GeneratorPosition" } },
        { "Windows.UI.Xaml.Media", new(System.StringComparer.Ordinal) { "Matrix" } },
        { "Windows.UI.Xaml.Media.Animation", new(System.StringComparer.Ordinal) { "KeyTime" } },
    };

    /// <summary>Mirrors C++ <c>has_addition_to_type</c>.</summary>
    public static bool HasAdditionToType(string typeNamespace, string typeName)
    {
        if (s_table.TryGetValue(typeNamespace, out HashSet<string>? names) && names.Contains(typeName))
        {
            return true;
        }
        return false;
    }
}
