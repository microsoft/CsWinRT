// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Maps Windows Runtime API contracts to their first available Windows SDK platform version.
/// </summary>
internal static class ContractPlatforms
{
    /// <summary>
    /// The mapping of all platforms and contract versions.
    /// </summary>
    private static readonly FrozenDictionary<string, (int Version, string Platform)[]> Table = new Dictionary<string, (int, string)[]>
    {
        ["Windows.AI.Actions.ActionsContract"] =
            [(8, "10.0.26100.0")],
        ["Windows.AI.Agents.AgentsContract"] =
            [(2, "10.0.26100.0")],
        ["Windows.AI.MachineLearning.MachineLearningContract"] =
            [(1, "10.0.17763.0"), (2, "10.0.18362.0"), (3, "10.0.19041.0"), (4, "10.0.20348.0"), (5, "10.0.22000.0")],
        ["Windows.AI.MachineLearning.Preview.MachineLearningPreviewContract"] =
            [(1, "10.0.17134.0"), (2, "10.0.17763.0")],
        ["Windows.ApplicationModel.Activation.ActivatedEventsContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.ApplicationModel.Activation.ActivationCameraSettingsContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.ApplicationModel.Activation.ContactActivatedEventsContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.ApplicationModel.Activation.WebUISearchActivatedEventsContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.ApplicationModel.Background.BackgroundAlarmApplicationContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.ApplicationModel.Calls.Background.CallsBackgroundContract"] =
            [(1, "10.0.10586.0"), (2, "10.0.18362.0"), (3, "10.0.20348.0"), (4, "10.0.22621.0")],
        ["Windows.ApplicationModel.Calls.CallsPhoneContract"] =
            [(4, "10.0.17763.0"), (5, "10.0.18362.0"), (6, "10.0.20348.0"), (7, "10.0.22621.0")],
        ["Windows.ApplicationModel.Calls.CallsVoipContract"] =
            [(1, "10.0.10586.0"), (2, "10.0.16299.0"), (3, "10.0.17134.0"), (4, "10.0.17763.0"), (5, "10.0.26100.0")],
        ["Windows.ApplicationModel.Calls.LockScreenCallContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.ApplicationModel.CommunicationBlocking.CommunicationBlockingContract"] =
            [(2, "10.0.17763.0")],
        ["Windows.ApplicationModel.FullTrustAppContract"] =
            [(1, "10.0.15063.0"), (2, "10.0.22000.0")],
        ["Windows.ApplicationModel.Preview.InkWorkspace.PreviewInkWorkspaceContract"] =
            [(1, "10.0.15063.0")],
        ["Windows.ApplicationModel.Preview.Notes.PreviewNotesContract"] =
            [(2, "10.0.15063.0")],
        ["Windows.ApplicationModel.Preview.StartupAppsPreviewContract"] =
            [(1, "10.0.26100.0")],
        ["Windows.ApplicationModel.Resources.Management.ResourceIndexerContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.17763.0")],
        ["Windows.ApplicationModel.Search.Core.SearchCoreContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.ApplicationModel.Search.SearchContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.ApplicationModel.SocialInfo.SocialInfoContract"] =
            [(1, "10.0.14393.0"), (2, "10.0.15063.0")],
        ["Windows.ApplicationModel.StartupTaskContract"] =
            [(1, "10.0.15063.0"), (2, "10.0.16299.0"), (3, "10.0.17134.0")],
        ["Windows.ApplicationModel.Wallet.WalletContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.22621.0")],
        ["Windows.Devices.Custom.CustomDeviceContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.Devices.DevicesLowLevelContract"] =
            [(2, "10.0.14393.0"), (3, "10.0.15063.0")],
        ["Windows.Devices.Portable.PortableDeviceContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.Devices.Power.PowerGridApiContract"] =
            [(1, "10.0.26100.0")],
        ["Windows.Devices.Printers.Extensions.ExtensionsContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.10586.0")],
        ["Windows.Devices.Printers.PrintersContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.Devices.Scanners.ScannerDeviceContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.Devices.SmartCards.SmartCardBackgroundTriggerContract"] =
            [(3, "10.0.16299.0")],
        ["Windows.Devices.SmartCards.SmartCardEmulatorContract"] =
            [(5, "10.0.16299.0"), (6, "10.0.17763.0")],
        ["Windows.Devices.Sms.LegacySmsApiContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.Foundation.FoundationContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.10586.0"), (3, "10.0.15063.0"), (4, "10.0.19041.0")],
        ["Windows.Foundation.UniversalApiContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.10586.0"), (3, "10.0.14393.0"), (4, "10.0.15063.0"), (5, "10.0.16299.0"),
             (6, "10.0.17134.0"), (7, "10.0.17763.0"), (8, "10.0.18362.0"), (10, "10.0.19041.0"), (12, "10.0.20348.0"),
             (14, "10.0.22000.0"), (15, "10.0.22621.0"), (19, "10.0.26100.0")],
        ["Windows.Foundation.VelocityIntegration.VelocityIntegrationContract"] =
            [(1, "10.0.17134.0")],
        ["Windows.Gaming.Input.GamingInputPreviewContract"] =
            [(1, "10.0.15063.0"), (2, "10.0.22621.0")],
        ["Windows.Gaming.Preview.GamesEnumerationContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.16299.0")],
        ["Windows.Gaming.UI.GameChatOverlayContract"] =
            [(1, "10.0.15063.0")],
        ["Windows.Gaming.UI.GamingUIProviderContract"] =
            [(1, "10.0.16299.0")],
        ["Windows.Gaming.XboxLive.StorageApiContract"] =
            [(1, "10.0.16299.0")],
        ["Windows.Globalization.GlobalizationJapanesePhoneticAnalyzerContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.Graphics.Printing3D.Printing3DContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.10586.0"), (3, "10.0.14393.0"), (4, "10.0.16299.0")],
        ["Windows.Management.Deployment.Preview.DeploymentPreviewContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.Management.Deployment.SharedPackageContainerContract"] =
            [(1, "10.0.22000.0")],
        ["Windows.Management.Orchestration.OrchestrationContract"] =
            [(1, "10.0.10586.0")],
        ["Windows.Management.Update.Cluster.ClusterNativeUpdatingContract"] =
            [(1, "10.0.28000.0")],
        ["Windows.Management.Update.WindowsUpdateContract"] =
            [(1, "10.0.22621.0"), (2, "10.0.26100.0")],
        ["Windows.Management.Workplace.WorkplaceSettingsContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.Media.AppBroadcasting.AppBroadcastingContract"] =
            [(1, "10.0.16299.0")],
        ["Windows.Media.AppRecording.AppRecordingContract"] =
            [(1, "10.0.16299.0")],
        ["Windows.Media.Capture.AppBroadcastContract"] =
            [(1, "10.0.15063.0"), (2, "10.0.16299.0")],
        ["Windows.Media.Capture.AppCaptureContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.10586.0"), (3, "10.0.14393.0"), (4, "10.0.15063.0")],
        ["Windows.Media.Capture.AppCaptureMetadataContract"] =
            [(1, "10.0.16299.0")],
        ["Windows.Media.Capture.CameraCaptureUIContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.Media.Capture.GameBarContract"] =
            [(1, "10.0.15063.0")],
        ["Windows.Media.Devices.CallControlContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.Media.MediaControlContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.Media.Playlists.PlaylistsContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.Media.Protection.ProtectionRenewalContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.Networking.Connectivity.WwanContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.17134.0"), (3, "10.0.26100.0")],
        ["Windows.Networking.NetworkOperators.LegacyNetworkOperatorsContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.Networking.Sockets.ControlChannelTriggerContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.15063.0"), (3, "10.0.17134.0")],
        ["Windows.Networking.XboxLive.XboxLiveSecureSocketsContract"] =
            [(1, "10.0.16299.0")],
        ["Windows.Perception.Automation.Core.PerceptionAutomationCoreContract"] =
            [(1, "10.0.15063.0")],
        ["Windows.Security.EnterpriseData.EnterpriseDataContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.10586.0"), (3, "10.0.14393.0"), (4, "10.0.15063.0"), (5, "10.0.16299.0")],
        ["Windows.Security.ExchangeActiveSyncProvisioning.EasContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.Security.Isolation.IsolatedWindowsEnvironmentContract"] =
            [(1, "10.0.19041.0"), (3, "10.0.20348.0"), (4, "10.0.22621.0"), (5, "10.0.26100.0")],
        ["Windows.Services.Maps.GuidanceContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.10586.0"), (3, "10.0.16299.0")],
        ["Windows.Services.Maps.LocalSearchContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.10586.0"), (3, "10.0.15063.0"), (4, "10.0.16299.0")],
        ["Windows.Services.Store.StoreContract"] =
            [(1, "10.0.14393.0"), (2, "10.0.15063.0"), (3, "10.0.17134.0"), (4, "10.0.17763.0")],
        ["Windows.Services.TargetedContent.TargetedContentContract"] =
            [(1, "10.0.15063.0")],
        ["Windows.Storage.Provider.CloudFilesContract"] =
            [(1, "10.0.16299.0"), (2, "10.0.17134.0"), (3, "10.0.17763.0"), (4, "10.0.19041.0"), (6, "10.0.20348.0"),
             (7, "10.0.22621.0")],
        ["Windows.System.Power.Thermal.PowerThermalApiContract"] =
            [(1, "10.0.26100.0")],
        ["Windows.System.Profile.PlatformAutomaticAppSignInContract"] =
            [(1, "10.0.19041.0")],
        ["Windows.System.Profile.ProfileHardwareTokenContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.System.Profile.ProfileRetailInfoContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.System.Profile.ProfileSharedModeContract"] =
            [(1, "10.0.14393.0"), (2, "10.0.15063.0")],
        ["Windows.System.Profile.SystemManufacturers.SystemManufacturersContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.16299.0"), (3, "10.0.17763.0")],
        ["Windows.System.SystemManagementContract"] =
            [(6, "10.0.17763.0"), (7, "10.0.19041.0")],
        ["Windows.System.UserProfile.UserProfileContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.16299.0")],
        ["Windows.System.UserProfile.UserProfileLockScreenContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.UI.ApplicationSettings.ApplicationsSettingsContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.UI.Core.AnimationMetrics.AnimationMetricsContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.UI.Core.CoreWindowDialogsContract"] =
            [(1, "10.0.10240.0")],
        ["Windows.UI.Input.Preview.Text.PreviewTextContract"] =
            [(1, "10.0.26100.0")],
        ["Windows.UI.Shell.CompanionWindows.CompanionWindowsContract"] =
            [(1, "10.0.26100.0")],
        ["Windows.UI.Shell.SecurityAppManagerContract"] =
            [(1, "10.0.17763.0")],
        ["Windows.UI.Shell.Tasks.AppTaskContract"] =
            [(2, "10.0.26100.0")],
        ["Windows.UI.Shell.WindowTabManagerContract"] =
            [(1, "10.0.22621.0")],
        ["Windows.UI.UIAutomation.UIAutomationContract"] =
            [(1, "10.0.20348.0"), (2, "10.0.22000.0")],
        ["Windows.UI.ViewManagement.ViewManagementViewScalingContract"] =
            [(1, "10.0.14393.0")],
        ["Windows.UI.Xaml.Core.Direct.XamlDirectContract"] =
            [(1, "10.0.17763.0"), (2, "10.0.18362.0"), (3, "10.0.20348.0"), (5, "10.0.22000.0")],
        ["Windows.UI.Xaml.Hosting.HostingContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.16299.0"), (3, "10.0.17763.0"), (4, "10.0.18362.0"), (5, "10.0.22000.0")],
        ["Windows.Web.Http.Diagnostics.HttpDiagnosticsContract"] =
            [(1, "10.0.10240.0"), (2, "10.0.15063.0")],
    }.ToFrozenDictionary();

    /// <summary>
    /// Returns the platform version (e.g., "10.0.17763.0") that introduced the given contract version,
    /// or empty string if not found.
    /// </summary>
    /// <param name="contractName">The contract name to use.</param>
    /// <param name="contractVersion">The contract version to use.</param>
    /// <param name="platform">The resulting platform, if found.</param>
    public static bool TryGetPlatform(string contractName, int contractVersion, [NotNullWhen(true)] out string? platform)
    {
        if (!Table.TryGetValue(contractName, out (int Version, string Platform)[]? versions))
        {
            platform = null;

            return false;
        }

        // Find the first version greater than the input contract version
        for (int i = 0; i < versions.Length; i++)
        {
            if (versions[i].Version >= contractVersion)
            {
                platform = versions[i].Platform;

                return true;
            }
        }

        platform = null;

        return false;
    }
}