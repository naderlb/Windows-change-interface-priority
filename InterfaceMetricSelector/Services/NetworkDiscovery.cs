using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace InterfaceMetricSelector.Services
{
    public sealed class NetworkSnapshot
    {
        public List<AdapterVm> Adapters { get; } = new();
        public DateTime TakenAtUtc { get; set; } = DateTime.UtcNow;
    }

    public static class NetworkDiscovery
    {
        public const string ProbeAddress = "8.8.8.8";

        public static NetworkSnapshot Capture()
        {
            var snap = new NetworkSnapshot();

            var ipRows = QueryMsftNetIpInterfaces();
            var virtualIx = QueryMsftVirtual();
            var defRoutes = QueryDefaultRoutes();
            var adapterDescByIndex = QueryMsftNetAdapterDescriptions();

            uint bestIf = 0;
            NativeIpHlp.TryGetBestIfForProbe(ProbeAddress, out bestIf);
            int winner = PickWinningIx(ipRows, defRoutes);

            var niByName = NetworkInterface.GetAllNetworkInterfaces()
                .GroupBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);

            foreach (IpRow row in ipRows.OrderBy(r => r.Metric).ThenBy(r => r.Alias))
            {
                niByName.TryGetValue(row.Alias, out NetworkInterface? ni);

                virtualIx.TryGetValue(row.Index, out bool virtWmi);
                bool virtHeur = VirtHeuristic(row.Alias, ni);
                RouteSlice? rr = BestRouteSlice(row.Index, defRoutes);
                int? combo = rr.HasValue ? row.Metric + rr.Value.RouteMetric : (int?)null;

                var (kind, glyph, label) = InterfaceGlyphHelper.Classify(ni, row.Alias, ni?.Description ?? "");

                string connDisplay = FormatConnectionDisplay(row.ConnectionStateCode, ni);
                bool ipActive = InferIpInterfaceActive(row.ConnectionStateCode, ni);

                string connectUsing;
                if (adapterDescByIndex.TryGetValue(row.Index, out string? wmiDriver) && !string.IsNullOrWhiteSpace(wmiDriver))
                    connectUsing = wmiDriver.Trim();
                else
                    connectUsing = ni?.Description ?? row.Alias;

                snap.Adapters.Add(new AdapterVm(
                    row.Index,
                    row.Alias,
                    row.Metric,
                    connDisplay,
                    ipActive,
                    ni?.OperationalStatus.ToString() ?? "",
                    ni != null ? FormatIpv4(ni) : "—",
                    ni?.GetIPProperties().DnsSuffix ?? "",
                    ni != null ? HexMac(ni.GetPhysicalAddress()) : "—",
                    FormatInterfaceType(ni),
                    connectUsing,
                    virtWmi || virtHeur,
                    ni?.NetworkInterfaceType == NetworkInterfaceType.Loopback,
                    WifiGuess(ni, row.Alias),
                    rr?.RouteMetric,
                    combo,
                    bestIf > 0 && (int)bestIf == row.Index,
                    winner == row.Index,
                    kind,
                    glyph,
                    label));
            }

            snap.TakenAtUtc = DateTime.UtcNow;
            return snap;
        }

        private sealed class IpRow
        {
            public int Index;
            public string Alias = "";
            public int Metric;
            /// <summary>WMI ConnectionState: 0 disconnected, 1 connected; null if unknown.</summary>
            public int? ConnectionStateCode;
        }

        private struct RouteSlice
        {
            public int InterfaceIndex;
            public int RouteMetric;
        }

        private static List<IpRow> QueryMsftNetIpInterfaces()
        {
            var rows = new List<IpRow>();
            try
            {
                const string q = "SELECT InterfaceIndex, InterfaceAlias, InterfaceMetric, ConnectionState FROM MSFT_NetIPInterface WHERE AddressFamily = 2";
                using var s = new ManagementObjectSearcher(@"root\StandardCimv2", q);
                foreach (ManagementObject o in s.Get())
                {
                    try
                    {
                        rows.Add(new IpRow
                        {
                            Index = ToInt(o["InterfaceIndex"]),
                            Alias = o["InterfaceAlias"]?.ToString()?.Trim() ?? "",
                            Metric = ToIntNz(o["InterfaceMetric"], 25),
                            ConnectionStateCode = ConnectionStateCodeFrom(o["ConnectionState"]),
                        });
                    }
                    catch { /* */ }
                }
            }
            catch { /* */ }

            rows.RemoveAll(r => r.Alias.Length == 0);
            return rows;
        }

        private static Dictionary<int, bool> QueryMsftVirtual()
        {
            var d = new Dictionary<int, bool>();
            try
            {
                using var s = new ManagementObjectSearcher(@"root\StandardCimv2", "SELECT InterfaceIndex, Virtual FROM MSFT_NetAdapter");
                foreach (ManagementObject o in s.Get())
                {
                    try
                    {
                        d[ToInt(o["InterfaceIndex"])] = o["Virtual"] is bool b && b;
                    }
                    catch { /* */ }
                }
            }
            catch { /* */ }

            return d;
        }

        /// <summary>Driver / hardware description per interface index ("Connect using" in Windows).</summary>
        private static Dictionary<int, string> QueryMsftNetAdapterDescriptions()
        {
            var d = new Dictionary<int, string>();
            try
            {
                using var s = new ManagementObjectSearcher(@"root\StandardCimv2",
                    "SELECT InterfaceIndex, InterfaceDescription FROM MSFT_NetAdapter");
                foreach (ManagementObject o in s.Get())
                {
                    try
                    {
                        int ix = ToInt(o["InterfaceIndex"]);
                        string desc = o["InterfaceDescription"]?.ToString()?.Trim() ?? "";
                        if (desc.Length > 0)
                            d[ix] = desc;
                    }
                    catch { /* */ }
                }
            }
            catch { /* */ }

            return d;
        }

        private static string FormatInterfaceType(NetworkInterface? ni)
        {
            if (ni == null)
                return "Unknown";

            return ni.NetworkInterfaceType switch
            {
                NetworkInterfaceType.Unknown => "Unknown",
                NetworkInterfaceType.Ethernet => "Ethernet",
                NetworkInterfaceType.GigabitEthernet => "Gigabit Ethernet",
                NetworkInterfaceType.Wireless80211 => "Wi‑Fi (802.11)",
                NetworkInterfaceType.Tunnel => "Tunnel",
                NetworkInterfaceType.Ppp => "PPP",
                NetworkInterfaceType.Fddi => "FDDI",
                NetworkInterfaceType.TokenRing => "Token Ring",
                NetworkInterfaceType.Loopback => "Loopback",
                NetworkInterfaceType.Slip => "SLIP",
                NetworkInterfaceType.Atm => "ATM",
                _ => ni.NetworkInterfaceType.ToString(),
            };
        }

        private static List<RouteSlice> QueryDefaultRoutes()
        {
            var list = new List<RouteSlice>();
            try
            {
                const string q = "SELECT InterfaceIndex, RouteMetric FROM MSFT_NetRoute WHERE DestinationPrefix='0.0.0.0/0' AND AddressFamily=2";
                using var s = new ManagementObjectSearcher(@"root\StandardCimv2", q);
                foreach (ManagementObject o in s.Get())
                {
                    try
                    {
                        list.Add(new RouteSlice
                        {
                            InterfaceIndex = ToInt(o["InterfaceIndex"]),
                            RouteMetric = ToIntNz(o["RouteMetric"], 0),
                        });
                    }
                    catch { /* */ }
                }
            }
            catch { /* */ }

            return list;
        }

        private static RouteSlice? BestRouteSlice(int ifaceIndex, List<RouteSlice> routes)
        {
            RouteSlice? best = null;
            foreach (RouteSlice r in routes.Where(r => r.InterfaceIndex == ifaceIndex))
            {
                if (!best.HasValue || r.RouteMetric < best.Value.RouteMetric) best = r;
            }

            return best;
        }

        private static int PickWinningIx(List<IpRow> ipRows, List<RouteSlice> routes)
        {
            var byIx = ipRows.ToDictionary(r => r.Index, r => r.Metric);
            int bestIx = -1;
            int bestSum = int.MaxValue;
            foreach (RouteSlice r in routes)
            {
                if (!byIx.TryGetValue(r.InterfaceIndex, out int im)) im = 256;
                int sum = im + r.RouteMetric;
                if (sum < bestSum)
                {
                    bestSum = sum;
                    bestIx = r.InterfaceIndex;
                }
            }

            return bestIx;
        }

        private static string FormatIpv4(NetworkInterface ni)
        {
            var parts = ni.GetIPProperties().UnicastAddresses
                .Where(u => u.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(u => $"{u.Address}/{u.PrefixLength}").ToArray();
            return parts.Length == 0 ? "—" : string.Join(", ", parts);
        }

        private static string HexMac(PhysicalAddress pa)
        {
            byte[] b = pa.GetAddressBytes();
            if (b.Length == 0) return "—";
            return string.Join("-", b.Select(x => x.ToString("X2", CultureInfo.InvariantCulture)));
        }

        private static bool VirtHeuristic(string alias, NetworkInterface? ni)
        {
            string s = $"{alias}|{ni?.Description}|{ni?.Name}".ToLowerInvariant();
            return s.Contains("virtual", StringComparison.Ordinal)
                   || s.Contains("hyper-v", StringComparison.Ordinal)
                   || s.Contains("wintun", StringComparison.Ordinal)
                   || s.Contains("tap ", StringComparison.Ordinal)
                   || s.Contains("pseudo", StringComparison.Ordinal);
        }

        private static bool WifiGuess(NetworkInterface? ni, string alias)
        {
            string s = $"{ni?.Description}{alias}".ToUpperInvariant();
            return s.Contains("WI-FI", StringComparison.Ordinal)
                   || s.Contains("WIRELESS", StringComparison.Ordinal)
                   || s.Contains("WLAN", StringComparison.Ordinal)
                   || s.Contains("802.11", StringComparison.Ordinal)
                   || ni?.NetworkInterfaceType == NetworkInterfaceType.Wireless80211;
        }

        /// <summary>WMI packs <c>Connected</c> / <c>Disconnected</c> as uint8: 0 = disconnected, 1 = connected,
        /// sometimes as string literals.</summary>
        private static int? ConnectionStateCodeFrom(object? raw)
        {
            if (raw is null)
                return null;

            if (raw is string s)
            {
                s = s.Trim();
                if (s.Length == 0)
                    return null;
                if (s.Equals("Connected", StringComparison.OrdinalIgnoreCase))
                    return 1;
                if (s.Equals("Disconnected", StringComparison.OrdinalIgnoreCase))
                    return 0;

                return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int p) ? p : null;
            }

            try
            {
                return Convert.ToInt32(raw, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private static string FormatConnectionDisplay(int? wmiConn, NetworkInterface? ni)
        {
            if (wmiConn.HasValue)
            {
                return wmiConn.Value switch
                {
                    1 => "Connected",
                    0 => "Disconnected",
                    _ => $"State {wmiConn.Value}",
                };
            }

            string s = ni?.OperationalStatus.ToString() ?? "";
            return string.IsNullOrEmpty(s) ? "—" : s;
        }

        private static bool InferIpInterfaceActive(int? wmiConn, NetworkInterface? ni)
        {
            if (wmiConn.HasValue)
                return wmiConn.Value == 1;

            if (ni == null)
                return false;

            return ni.OperationalStatus == OperationalStatus.Up;
        }

        private static int ToInt(object? o) => Convert.ToInt32(o, CultureInfo.InvariantCulture);

        private static int ToIntNz(object? o, int fallback)
        {
            try { return Convert.ToInt32(o, CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }
    }

    internal static class NativeIpHlp
    {
        internal static bool TryGetBestIfForProbe(string dotted, out uint ifIndex)
        {
            ifIndex = 0;
            try
            {
                byte[] b = IPAddress.Parse(dotted).GetAddressBytes();
                if (b.Length != 4) return false;
                uint addr = (uint)b[0] | ((uint)b[1] << 8) | ((uint)b[2] << 16) | ((uint)b[3] << 24);
                uint no = (uint)((b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]);
                if (GetBestInterface(addr, out ifIndex) == 0 && ifIndex != 0) return true;
                ifIndex = 0;
                if (GetBestInterface(no, out ifIndex) == 0 && ifIndex != 0) return true;
            }
            catch { /* */ }

            ifIndex = 0;
            return false;
        }

        [DllImport("iphlpapi.dll")]
        private static extern int GetBestInterface(uint dwDestAddr, out uint pdwBestIfIndex);
    }
}
