using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace InterfaceMetricSelector
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
