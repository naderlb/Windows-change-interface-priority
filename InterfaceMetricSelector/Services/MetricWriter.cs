using System;
using System.Globalization;
using System.Management;
using System.Security.Principal;

namespace InterfaceMetricSelector.Services
{
    public static class MetricWriter
    {
        public static bool IsCurrentProcessElevated()
        {
            using var id = WindowsIdentity.GetCurrent();
            var p = new WindowsPrincipal(id);
            return p.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>Sets IPv4 interface metric via WMI (same as Set-NetIPInterface). Requires elevation.</summary>
        public static void SetIpv4InterfaceMetric(int interfaceIndex, int metric)
        {
            if (metric < 0 || metric > 9999)
                throw new ArgumentOutOfRangeException(nameof(metric), "Metric must be between 0 and 9999.");

            const string q = "SELECT * FROM MSFT_NetIPInterface WHERE InterfaceIndex = {0} AND AddressFamily = 2";
            using var s = new ManagementObjectSearcher("root\\StandardCimv2", string.Format(CultureInfo.InvariantCulture, q, interfaceIndex));
            ManagementObject? found = null;
            foreach (ManagementObject o in s.Get())
            {
                found = o;
                break;
            }

            if (found == null)
                throw new InvalidOperationException($"No IPv4 MSFT_NetIPInterface with index {interfaceIndex}.");

            try
            {
                found["InterfaceMetric"] = metric;
                found.Put();
            }
            finally
            {
                found.Dispose();
            }
        }
    }
}
