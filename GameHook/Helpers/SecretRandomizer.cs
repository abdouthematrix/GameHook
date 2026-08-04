using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace GameHook
{
    public static class SecretRandomizer
    {
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        private static string RandomAlnumString(int length)
        {
            char[] result = new char[length + 1];
            result[0] = '/';

            byte[] raw = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(raw);
            }

            for (int i = 0; i < length; i++)
            {
                result[i + 1] = Alphabet[raw[i] % Alphabet.Length];
            }

            return new string(result);
        }

        private static void WriteString(IntPtr address, string str, int bufferSize)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(str + "\0");

            if (VirtualProtect(address, (UIntPtr)bufferSize, PAGE_EXECUTE_READWRITE, out uint oldProtect))
            {
                Marshal.Copy(bytes, 0, address, Math.Min(bytes.Length, bufferSize));
                VirtualProtect(address, (UIntPtr)bufferSize, oldProtect, out _);
            }
        }

        public static void Apply()
        {
            string s1 = RandomAlnumString(17);
            WriteString(Targets.Data_SecretString1, s1, 18);

            string s2 = RandomAlnumString(22);
            WriteString(Targets.Data_SecretString2, s2, 24);

            Log.Info("  [OK] ServerTQCommands (secret command prefixes randomized)");
        }
    }
}
