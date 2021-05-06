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
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ComInterop
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private async void FilePicker_Click(object sender, RoutedEventArgs e)
        {
            //var hwnd = ((object)this).As<WinRT.Interop.IWindowNative>().WindowHandle;
            
            //var folderPicker = new FolderPicker();
            //var initializeWithWindow = folderPicker.As<WinRT.Interop.IInitializeWithWindow>();
            //initializeWithWindow.Initialize(hwnd);

            //folderPicker.FileTypeFilter.Add("*");
            //var folder = await folderPicker.PickSingleFolderAsync();
            //myText.Text = folder != null ? folder.Path : string.Empty;

            // Message Dialog Sample
            //MessageDialog dialog = new MessageDialog("Hellow WinRT");
            //var initializeWithWindow = ((object)dialog).As<WinRT.Interop.IInitializeWithWindow>();
            //initializeWithWindow.Initialize(hwnd);
            //await dialog.ShowAsync();
        }

        private async void UserConsentVerifier_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = ((Object)this).As<WinRT.Interop.IWindowNative>().WindowHandle;
            await Windows.Security.Credentials.UI.UserConsentVerifierInterop.RequestVerificationForWindowAsync(hwnd, "Test Message");
        }
    }
}
