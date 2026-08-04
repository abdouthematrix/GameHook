using System;
using System.Net.Http;
using Microsoft.Win32;

namespace GameHook
{
    public static class IdentityCheck
    {
        private static string ReadMachineGuid()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                {
                    return key?.GetValue("MachineGuid")?.ToString() ?? string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string QueryPublicIp()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    string ip = client.GetStringAsync("http://checkip.amazonaws.com").Result.Trim();
                    return ip;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        public static void RunIfEnabled(GameConfig cfg)
        {
            if (!cfg.IdentityCheckEnabled)
            {
                Log.Info("[Identity] Check disabled (Identity::EnableCheck=false in config).");
                return;
            }

            string hwid = ReadMachineGuid();
            if (!string.IsNullOrEmpty(hwid) && !string.IsNullOrEmpty(cfg.AllowedHwid) &&
                string.Equals(hwid, cfg.AllowedHwid, StringComparison.OrdinalIgnoreCase))
            {
                Log.Info($"[Identity] HWID = {hwid}  [OK]");
                return;
            }

            string ip = QueryPublicIp();
            if (!string.IsNullOrEmpty(ip) && !string.IsNullOrEmpty(cfg.AllowedIp) &&
                string.Equals(ip, cfg.AllowedIp, StringComparison.OrdinalIgnoreCase))
            {
                Log.Info($"[Identity] IP   = {ip}  [OK]");
                return;
            }

            Log.Info($"[Identity] FAILED - neither HWID ({(string.IsNullOrEmpty(hwid) ? "?" : hwid)}) nor IP ({(string.IsNullOrEmpty(ip) ? "?" : ip)}) matched the configured allowlist");
        }
    }
}
