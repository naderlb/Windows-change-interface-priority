using System;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using InterfaceMetricSelector.Services;

namespace InterfaceMetricSelector
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<AdapterVm> Adapters { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += async (_, __) => await RefreshAsync();
        }

        async void Refresh_OnClick(object sender, RoutedEventArgs e) => await RefreshAsync();

        async Task RefreshAsync()
        {
            BusyOverlay.Visibility = Visibility.Visible;
            RefreshBtn.IsEnabled = false;
            try
            {
                AppLog.Write("UI", "refresh");
                var snap = await Task.Run(() => NetworkDiscovery.Capture());
                Adapters.Clear();
                foreach (var a in snap.Adapters)
                    Adapters.Add(a);
                LastUpdatedText.Text = $"Updated {snap.TakenAtUtc.ToLocalTime():T}";
            }
            catch (Exception ex)
            {
                AppLog.Exception("refresh", ex);
                MessageBox.Show(ex.Message, "Interface Metric Selector", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BusyOverlay.Visibility = Visibility.Collapsed;
                RefreshBtn.IsEnabled = true;
            }
        }

        void ApplyMetric_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b)
                return;
            if (b.DataContext is not AdapterVm vm)
                return;

            if (!int.TryParse(vm.DraftMetric.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int m))
            {
                MessageBox.Show("Enter a whole number between 0 and 9999.", "Interface Metric Selector",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!MetricWriter.IsCurrentProcessElevated())
            {
                MessageBox.Show("Changing the IPv4 metric requires running this application as Administrator.",
                    "Interface Metric Selector", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                MetricWriter.SetIpv4InterfaceMetric(vm.InterfaceIndex, m);
                vm.RefreshMetricFromOs(m);
                AppLog.Write("UI", $"set metric iface {vm.InterfaceIndex} = {m}");
            }
            catch (Exception ex)
            {
                AppLog.Exception("set metric", ex);
                MessageBox.Show(ex.Message, "Interface Metric Selector", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void CopyIp_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b)
                return;
            if (b.DataContext is not AdapterVm vm)
                return;

            string s = FirstIpOnly(vm.IPv4Addresses);
            if (string.IsNullOrEmpty(s))
            {
                MessageBox.Show("No IPv4 address available to copy.", "Interface Metric Selector",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            TrySetClipboard(s);
        }

        void CopyMac_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b)
                return;
            if (b.DataContext is not AdapterVm vm)
                return;

            if (vm.Mac.Length == 0 || vm.Mac == "—")
            {
                MessageBox.Show("No MAC address available to copy.", "Interface Metric Selector",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            TrySetClipboard(vm.Mac);
        }

        static string FirstIpOnly(string ipv4Field)
        {
            if (string.IsNullOrWhiteSpace(ipv4Field) || ipv4Field == "—")
                return "";

            string first = ipv4Field.Split(',')[0].Trim();
            int slash = first.IndexOf('/');
            if (slash > 0)
                first = first[..slash];
            return first.Trim();
        }

        static void TrySetClipboard(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not copy to the clipboard: " + ex.Message, "Interface Metric Selector",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        void About_OnClick(object sender, RoutedEventArgs e)
        {
            new AboutWindow { Owner = this }.ShowDialog();
        }

        void FooterLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
