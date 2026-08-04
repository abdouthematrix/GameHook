using System;
using System.Runtime.InteropServices;

namespace GameHook
{
    public static class ConfigPatches
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        private static bool WriteProtected(IntPtr address, byte[] data)
        {
            if (!VirtualProtect(address, (UIntPtr)data.Length, PAGE_EXECUTE_READWRITE, out uint oldProtect))
                return false;

            Marshal.Copy(data, 0, address, data.Length);

            VirtualProtect(address, (UIntPtr)data.Length, oldProtect, out _);
            return true;
        }

        private static bool WriteInt32(IntPtr address, int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return WriteProtected(address, bytes);
        }

        public static void Apply(GameConfig cfg)
        {
            if (WriteInt32(Targets.Data_QualitySocket, cfg.QualitySocket))
                Log.Info($"[ConfigPatches] QualitySocket patched to {cfg.QualitySocket}.");

            if (WriteInt32(Targets.Data_MeteorFirstSocket, cfg.MeteorFirstSocket))
                Log.Info($"[ConfigPatches] MeteorFirstSocket patched to {cfg.MeteorFirstSocket}.");

            if (WriteInt32(Targets.Data_MeteorSecondSocket, cfg.MeteorSecondSocket))
                Log.Info($"[ConfigPatches] MeteorSecondSocket patched to {cfg.MeteorSecondSocket}.");

            if (!string.IsNullOrEmpty(cfg.BlowfishKey))
            {
                byte[] key = new byte[16];
                byte[] src = System.Text.Encoding.ASCII.GetBytes(cfg.BlowfishKey);
                Array.Copy(src, key, Math.Min(src.Length, key.Length));

                if (WriteProtected(Targets.Data_BlowfishKey, key))
                    Log.Info($"[ConfigPatches] BlowfishKey patched ({cfg.BlowfishKey.Length} bytes used).");
            }
        }
    }
}
