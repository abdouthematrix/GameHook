using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using EasyHook;

namespace GameHook
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SockAddrIn
    {
        public short sin_family;
        public ushort sin_port;      // network byte order
        public uint sin_addr;        // network byte order
        public long sin_zero;
    }
    public static class ConnectRedirect
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public delegate int ConnectDelegate(IntPtr s, ref SockAddrIn name, int namelen);

        [DllImport("ws2_32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern int connect(IntPtr s, ref SockAddrIn name, int namelen);

        [DllImport("ws2_32.dll", CharSet = CharSet.Ansi)]
        private static extern int inet_pton(int family, string ipString, out uint buf);

        private static LocalHook _hook;        

        public static bool Install()
        {
            try
            {
                _hook = LocalHook.Create(
                    LocalHook.GetProcAddress("ws2_32.dll", "connect"),
                    new ConnectDelegate(Detour),
                    null);
                _hook.ThreadACL.SetExclusiveACL(new int[0]); // hook all threads

                Log.Info("  [OK] ConnectRedirect (ws2_32.connect)");
                return true;
            }
            catch (Exception ex)
            {
                Log.Info($"  [FAIL] ConnectRedirect: {ex.Message}");
                return false;
            }
        }

        public static void UnInstall()
        {
            _hook.Dispose();
        }

        private static int Detour(IntPtr s, ref SockAddrIn name, int namelen)
        {
            try
            {
                short family = name.sin_family;
                if (family == (short)AddressFamily.InterNetwork)
                {
                    int port = IPAddress.NetworkToHostOrder((short)name.sin_port) & 0xFFFF;

                    if (port == Targets.KeyServerPort)
                    {
                        inet_pton(AddressFamily.InterNetwork.GetHashCode(), "127.0.0.1", out uint addr);
                        name.sin_addr = addr;   
                        Log.Info($"[ConnRedirect] KeyServer -> 127.0.0.1:{port}");
                    }
                    else if (port > Targets.RedirectAbovePort)
                    {
                        inet_pton(AddressFamily.InterNetwork.GetHashCode(), "127.0.0.1", out uint addr);
                        name.sin_addr = addr;
                        Log.Info($"[ConnRedirect] Port {port} -> 127.0.0.1");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Info($"[ConnRedirect] Exception: {ex.Message}");
            }

            return connect(s, ref name, namelen);
        }
    }
}
