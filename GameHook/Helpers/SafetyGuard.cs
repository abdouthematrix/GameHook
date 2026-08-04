using System;
using System.IO;
using System.Security.Cryptography;

namespace GameHook
{
    public static class SafetyGuard
    {
        // Expected SHA256 of the known MsgServer.exe build
        private const string ExpectedSha256 = "0d80d25a586e8a5616f6d9f720ee27410f1421519521f9f7beaf9d5375e5fb89";

        private static bool ComputeSelfExeSha256(out string hex)
        {
            try
            {
                string path = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                using (var fs = File.OpenRead(path))
                using (var sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(fs);
                    hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    return true;
                }
            }
            catch
            {
                hex = string.Empty;
                return false;
            }
        }

        public static bool IsKnownMsgServerBuild()
        {
            if (!ComputeSelfExeSha256(out string hex))
            {
                Log.Info("[SafetyGuard] Could not hash host process executable - refusing to hook.");
                return false;
            }

            if (!string.Equals(hex, ExpectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                Log.Info("[SafetyGuard] Host executable does not match the known MsgServer.exe build.");
                Log.Info("[SafetyGuard] Expected {0}, got {1}.", ExpectedSha256, hex);
                Log.Info("[SafetyGuard] Refusing to install any hooks or memory patches.");
                return false;
            }

            Log.Info("[SafetyGuard] Host executable matches the known MsgServer.exe build.");
            return true;
        }
    }
}
