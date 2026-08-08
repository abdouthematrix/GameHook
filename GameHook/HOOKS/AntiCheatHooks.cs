using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using EasyHook;

namespace GameHook
{
    /// <summary>
    /// Hooks three anticheat.dll functions purely for diagnostic logging, to
    /// find out why legitimate players are being jailed:
    ///
    ///   - UserAntiCheat_VerifyClientChecksum   (RVA 0xDC60)  -- per-tick sequence check
    ///   - CAntiCheatMgr_StartJailPunishmentByUserId (RVA 0x5AD0) -- the actual jail call,
    ///         shows userId, delaySeconds, and the reason string for EVERY jailing,
    ///         regardless of which code path triggered it
    ///   - UserAntiCheat_ReportCheatByUserId    (RVA 0x59C0)  -- shows userId + cheat-type
    ///         code for every cheat report, cross-reference against g_CheatTypeNames
    ///         (index 7 = _TYPE_INVALID_CLIENT, etc.)
    ///
    /// All three detours call through to the original implementation unmodified --
    /// this only observes, it never changes pass/fail or suppresses punishment.
    /// </summary>
    public static class AntiCheatHooks
    {
        private const int VerifyChecksumRva = 0xDC60;
        private const int StartJailPunishmentByUserIdRva = 0x5AD0;
        private const int ReportCheatByUserIdRva = 0x59C0;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte VerifyClientChecksumDelegate(IntPtr self, int clientChecksum);

        // void CAntiCheatMgr::StartJailPunishmentByUserId(this, userId, delaySeconds, reason)
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void StartJailPunishmentByUserIdDelegate(
            IntPtr self, uint userId, uint delaySeconds, IntPtr reasonPtr);

        // void CAntiCheatMgr::ReportCheatByUserId(this, userId, idCheatKind)
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ReportCheatByUserIdDelegate(
            IntPtr self, uint userId, uint idCheatKind);

        private static LocalHook _hookVerify;
        private static LocalHook _hookJail;
        private static LocalHook _hookReport;

        private static VerifyClientChecksumDelegate _originalVerify;
        private static StartJailPunishmentByUserIdDelegate _originalJail;
        private static ReportCheatByUserIdDelegate _originalReport;

        public static bool Install()
        {
            try
            {
                // Poll until anticheat.dll is loaded
                IntPtr moduleBase = IntPtr.Zero;
                const int pollIntervalMs = 500;
                const int timeoutMs = 30000;
                int waited = 0;

                while (moduleBase == IntPtr.Zero && waited < timeoutMs)
                {
                    moduleBase = GetModuleBaseAddress("anticheat.dll");
                    if (moduleBase == IntPtr.Zero)
                    {
                        Thread.Sleep(pollIntervalMs);
                        waited += pollIntervalMs;
                    }
                }

                if (moduleBase == IntPtr.Zero)
                {
                    Log.Info("[AntiCheat] anticheat.dll not found after timeout.");
                    return false;
                }

                IntPtr verifyAddr = IntPtr.Add(moduleBase, VerifyChecksumRva);
                IntPtr jailAddr = IntPtr.Add(moduleBase, StartJailPunishmentByUserIdRva);
                IntPtr reportAddr = IntPtr.Add(moduleBase, ReportCheatByUserIdRva);

                _hookVerify = LocalHook.Create(
                    verifyAddr, new VerifyClientChecksumDelegate(DetourVerifyClientChecksum), null);
                _hookJail = LocalHook.Create(
                    jailAddr, new StartJailPunishmentByUserIdDelegate(DetourStartJailPunishmentByUserId), null);
                _hookReport = LocalHook.Create(
                    reportAddr, new ReportCheatByUserIdDelegate(DetourReportCheatByUserId), null);

                _hookVerify.ThreadACL.SetExclusiveACL(new int[0]);
                _hookJail.ThreadACL.SetExclusiveACL(new int[0]);
                _hookReport.ThreadACL.SetExclusiveACL(new int[0]);

                _originalVerify = (VerifyClientChecksumDelegate)Marshal.GetDelegateForFunctionPointer(
                    _hookVerify.HookBypassAddress, typeof(VerifyClientChecksumDelegate));
                _originalJail = (StartJailPunishmentByUserIdDelegate)Marshal.GetDelegateForFunctionPointer(
                    _hookJail.HookBypassAddress, typeof(StartJailPunishmentByUserIdDelegate));
                _originalReport = (ReportCheatByUserIdDelegate)Marshal.GetDelegateForFunctionPointer(
                    _hookReport.HookBypassAddress, typeof(ReportCheatByUserIdDelegate));

                Log.Info($"[AntiCheat] Hooked VerifyClientChecksum @ 0x{verifyAddr.ToInt64():X}");
                Log.Info($"[AntiCheat] Hooked StartJailPunishmentByUserId @ 0x{jailAddr.ToInt64():X}");
                Log.Info($"[AntiCheat] Hooked ReportCheatByUserId @ 0x{reportAddr.ToInt64():X}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Info($"[AntiCheat] Failed to install hooks: {ex}");
                return false;
            }
        }

        private static byte DetourVerifyClientChecksum(IntPtr self, int clientChecksum)
        {
            byte result = _originalVerify(self, clientChecksum);

            try
            {
                int expected = Marshal.ReadInt32(self, 0x1f8);
                uint userId = (uint)Marshal.ReadInt32(self, 0x18);

                Log.Info($"[AntiCheat][Verify] userId={userId} client=0x{clientChecksum:X8} expected=0x{expected:X8} match={(result != 0)}");
            }
            catch (Exception ex)
            {
                Log.Info($"[AntiCheat][Verify] logging error: {ex.Message}");
            }

            return result;
        }

        private static void DetourStartJailPunishmentByUserId(
            IntPtr self, uint userId, uint delaySeconds, IntPtr reasonPtr)
        {
            try
            {
                string reason = reasonPtr != IntPtr.Zero
                    ? DecodeGbkOrAnsi(reasonPtr)
                    : "(null)";

                Log.Info($"[AntiCheat][Jail] userId={userId} delaySeconds={delaySeconds} reason=\"{reason}\"");
            }
            catch (Exception ex)
            {
                Log.Info($"[AntiCheat][Jail] logging error: {ex.Message}");
            }

            _originalJail(self, userId, delaySeconds, reasonPtr);
        }

        private static void DetourReportCheatByUserId(IntPtr self, uint userId, uint idCheatKind)
        {
            Log.Info($"[AntiCheat][Report] userId={userId} idCheatKind={idCheatKind}");
            _originalReport(self, userId, idCheatKind);
        }

        // The server console rendered this reason text as readable Chinese, which
        // suggests GBK encoding rather than the default ANSI codepage. Try GBK
        // first; fall back to ANSI if the codepage isn't registered/available.
        private static string DecodeGbkOrAnsi(IntPtr ptr)
        {
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0) len++;
            byte[] bytes = new byte[len];
            Marshal.Copy(ptr, bytes, 0, len);

            try
            {
                // Requires: NuGet package System.Text.Encoding.CodePages, and
                // System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
                // called once at process startup.
                return System.Text.Encoding.GetEncoding(936).GetString(bytes); // GBK
            }
            catch (NotSupportedException)
            {
                return Marshal.PtrToStringAnsi(ptr);
            }
        }

        private static IntPtr GetModuleBaseAddress(string moduleName)
        {
            foreach (ProcessModule mod in Process.GetCurrentProcess().Modules)
            {
                if (string.Equals(mod.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return mod.BaseAddress;
                }
            }
            return IntPtr.Zero;
        }
    }
}