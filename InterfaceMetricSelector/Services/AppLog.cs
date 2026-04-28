using System;
using System.IO;

namespace InterfaceMetricSelector.Services
{
    public static class AppLog
    {
        static readonly object Sync = new();

        public static void Init()
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(dir);
            }
            catch { /* */ }
        }

        public static void Write(string category, string message)
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, $"InterfaceMetricSelector_{DateTime.UtcNow:yyyyMMdd}.log");
                string line = $"{DateTime.UtcNow:O}\t{category}\t{message}{Environment.NewLine}";
                lock (Sync) File.AppendAllText(file, line);
            }
            catch { /* */ }
        }

        public static void Exception(string where, Exception ex) =>
            Write("EX", $"{where}: {ex.GetType().Name} {ex.Message}{Environment.NewLine}{ex.StackTrace}");
    }
}
