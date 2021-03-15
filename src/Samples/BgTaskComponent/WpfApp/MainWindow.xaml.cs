using System.Windows;
using Windows.ApplicationModel.Background;

namespace WpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string exampleTaskName = "ToastBgTask";

        public MainWindow()
        {
            InitializeComponent();
            LoadTasks();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var builder = new BackgroundTaskBuilder
            {
                Name = exampleTaskName,
                TaskEntryPoint = "BgTaskComponent.ToastBgTask"
            };
            builder.SetTrigger(new SystemTrigger(SystemTriggerType.TimeZoneChange, false));
            builder.Register();
            LoadTasks();
        }

        private void UnregisterButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == exampleTaskName)
                {
                    task.Value.Unregister(true);
                }
            }
            LoadTasks();
        }

        private void LoadTasks()
        {
            var registeredTasks = BackgroundTaskRegistration.AllTasks;

            if (registeredTasks.Count == 0)
            {
                InfoBGTask.Text = "No BG Tasks Registered";
                RegisterButton.IsEnabled = true;
                UnregisterButton.IsEnabled = false;
                return;
            }
            RegisterButton.IsEnabled = false;
            UnregisterButton.IsEnabled = true;
            InfoBGTask.Text = "Bg Task Registered: " + exampleTaskName;
        }
    }
}
