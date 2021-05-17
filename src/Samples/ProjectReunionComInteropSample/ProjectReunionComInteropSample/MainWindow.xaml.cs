using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using WinRT;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUIComInteropSample
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private async void userConsentVerifierButton_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = ((Object)this).As<WinRT.Interop.IWindowNative>().WindowHandle;
            await Windows.Security.Credentials.UI.UserConsentVerifierInterop.RequestVerificationForWindowAsync(hwnd, "Test Message");;
        }

        private async void filePickerButton_Click(object sender, RoutedEventArgs e)
        {
            // casting to (Object) is a workaround - cswinrt issue #485 with .As<> 
            var hwnd = ((object)this).As<WinRT.Interop.IWindowNative>().WindowHandle;
            var folderPicker = new FolderPicker();
            var initializeWithWindow = ((object)folderPicker).As<WinRT.Interop.IInitializeWithWindow>();
            initializeWithWindow.Initialize(hwnd);
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            //filePickerButton.Content = folder != null ? folder.Path : string.Empty;
        }
    }
}
