﻿using System.Windows;
using Windows.ApplicationModel.Background;

namespace WpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var exampleTaskName = "ToastBgTask";

            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == exampleTaskName)
                {
                    break;
                }
            }

            var builder = new BackgroundTaskBuilder
            {
                Name = exampleTaskName,
                TaskEntryPoint = "BgTaskComponent.ToastBgTask"
            };
            builder.SetTrigger(new SystemTrigger(SystemTriggerType.TimeZoneChange, false));
            builder.Register();
        }
    }
}
