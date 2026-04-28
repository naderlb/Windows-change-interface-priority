using System;
using System.Net.NetworkInformation;
using InterfaceMetricSelector.Models;

namespace InterfaceMetricSelector.Services
{
    internal static class InterfaceGlyphHelper
    {
        /// <summary>
        /// Segoe MDL2 Assets glyph + short label for the interface row.
        /// </summary>
        public static (InterfaceUiKind Kind, string Glyph, string ShortLabel) Classify(NetworkInterface? ni, string alias, string description)
        {
            string d = description ?? "";
            string a = $"{alias}|{d}|{ni?.Name}|".ToLowerInvariant();

            if (ni?.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                return (InterfaceUiKind.Loopback, "\uE72A", "Loopback");

            if (a.Contains("bluetooth", StringComparison.Ordinal))
                return (InterfaceUiKind.Bluetooth, "\uE702", "Bluetooth");

            if (ni?.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                ni?.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet)
                return (InterfaceUiKind.Ethernet, "\uE839", "Ethernet");

            if (ni?.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                a.Contains("wi-fi", StringComparison.Ordinal) ||
                a.Contains("wi fi", StringComparison.Ordinal) ||
                a.Contains("wireless", StringComparison.Ordinal) ||
                a.Contains("wlan", StringComparison.Ordinal) ||
                a.Contains("802.11", StringComparison.Ordinal))
                return (InterfaceUiKind.WiFi, "\uE701", "Wi-Fi");

            if (ni?.NetworkInterfaceType == NetworkInterfaceType.Ppp ||
                ni?.NetworkInterfaceType == NetworkInterfaceType.Slip)
                return (InterfaceUiKind.Ppp, "\uE825", "PPP");

            if (ni?.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                a.Contains("tunnel", StringComparison.Ordinal) ||
                a.Contains("teredo", StringComparison.Ordinal) ||
                a.Contains("isatap", StringComparison.Ordinal))
                return (InterfaceUiKind.TunnelOrVpnName, "\uE83E", "Tunnel");

            if (a.Contains("vpn", StringComparison.Ordinal))
                return (InterfaceUiKind.TunnelOrVpnName, "\uE83E", "VPN");

            if (a.Contains("virtual", StringComparison.Ordinal) ||
                a.Contains("hyper-v", StringComparison.Ordinal) ||
                a.Contains("wintun", StringComparison.Ordinal) ||
                a.Contains("tap ", StringComparison.Ordinal) ||
                a.Contains("pseudo", StringComparison.Ordinal) ||
                a.Contains("vmware", StringComparison.Ordinal))
                return (InterfaceUiKind.VirtualNic, "\uE9B5", "Virtual");

            return (InterfaceUiKind.Other, "\uE839", "Network");
        }
    }
}
