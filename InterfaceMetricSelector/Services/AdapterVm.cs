using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using InterfaceMetricSelector.Models;

namespace InterfaceMetricSelector.Services
{
    /// <summary>
    /// View row for one IPv4 interface; supports draft metric + apply.
    /// </summary>
    public sealed class AdapterVm : INotifyPropertyChanged
    {
        int _metric;

        public AdapterVm(
            int interfaceIndex,
            string alias,
            int metric,
            string connection,
            bool ipInterfaceActive,
            string operationalState,
            string ipv4Addresses,
            string dnsSuffix,
            string mac,
            string interfaceTypeDisplay,
            string connectUsing,
            bool isVirtualAdapter,
            bool isLoopback,
            bool appearsWireless,
            int? routeMetricAddition,
            int? combinedIpv4DefaultMetric,
            bool isBestOutbound,
            bool winsDefaultComparison,
            InterfaceUiKind uiKind,
            string iconGlyph,
            string kindLabel)
        {
            InterfaceIndex = interfaceIndex;
            Alias = alias;
            _metric = metric;
            Connection = connection;
            IsActive = ipInterfaceActive;
            OperationalState = operationalState;
            IPv4Addresses = ipv4Addresses;
            DnsSuffix = dnsSuffix;
            Mac = mac;
            InterfaceTypeDisplay = interfaceTypeDisplay;
            ConnectUsing = connectUsing;
            IsVirtualAdapter = isVirtualAdapter;
            IsLoopback = isLoopback;
            AppearsWireless = appearsWireless;
            RouteMetricAddition = routeMetricAddition;
            CombinedIpv4DefaultMetric = combinedIpv4DefaultMetric;
            IsBestOutbound = isBestOutbound;
            WinsDefaultComparison = winsDefaultComparison;
            UiKind = uiKind;
            IconGlyph = iconGlyph;
            KindLabel = kindLabel;
            _draftMetric = metric.ToString(CultureInfo.InvariantCulture);
        }

        public int InterfaceIndex { get; }
        public string Alias { get; }
        public int Metric
        {
            get => _metric;
            private set
            {
                if (_metric == value) return;
                _metric = value;
                OnPropertyChanged();
            }
        }

        string _draftMetric;
        public string DraftMetric
        {
            get => _draftMetric;
            set
            {
                if (_draftMetric == value) return;
                _draftMetric = value;
                OnPropertyChanged();
            }
        }

        public string Connection { get; }
        /// <summary>WMI ConnectionState (IPv4 stack) is 1 when connected; fallback uses NIC operational status.</summary>
        public bool IsActive { get; }
        public string OperationalState { get; }
        public string IPv4Addresses { get; }
        public string DnsSuffix { get; }
        public string Mac { get; }
        /// <summary>Derived from .NET network interface type enum (human-readable).</summary>
        public string InterfaceTypeDisplay { get; }
        /// <summary>WMI MSFT_NetAdapter.InterfaceDescription (same line as &quot;Connect using&quot; in Windows).</summary>
        public string ConnectUsing { get; }
        public bool IsVirtualAdapter { get; }
        public bool IsLoopback { get; }
        public bool AppearsWireless { get; }
        public int? RouteMetricAddition { get; }
        public int? CombinedIpv4DefaultMetric { get; }
        public bool IsBestOutbound { get; }
        public bool WinsDefaultComparison { get; }

        public InterfaceUiKind UiKind { get; }
        public string IconGlyph { get; }
        public string KindLabel { get; }

        public string ActiveLabel => IsActive ? "Active" : "Not active";

        public void RefreshMetricFromOs(int newMetric)
        {
            Metric = newMetric;
            DraftMetric = newMetric.ToString(CultureInfo.InvariantCulture);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
