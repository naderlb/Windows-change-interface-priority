using System;
using System.Windows;
using System.Windows.Threading;
using InterfaceMetricSelector.Services;

namespace InterfaceMetricSelector
{
    public partial class App : Application
    {
        void OnStartup(object sender, StartupEventArgs e)
        {
            AppLog.Init();
            AppLog.Write("APP", "Started Interface Metric Selector");
        }

        void OnDispatcherUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            AppLog.Exception("UI", e.Exception);
            MessageBox.Show(
                "Something went wrong. Details were written to the logs folder next to the application.",
                "Interface Metric Selector",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            e.Handled = true;
        }
    }
}
