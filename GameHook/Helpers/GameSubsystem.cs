using System;
using System.Runtime.InteropServices;

namespace GameHook
{
    public static class GameSubsystem
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetSubsystemDelegate(int id);

        private static GetSubsystemDelegate _getSubsystem;

        public static void Initialize()
        {
            _getSubsystem = (GetSubsystemDelegate)Marshal.GetDelegateForFunctionPointer(
                Targets.GetSubsystem, typeof(GetSubsystemDelegate));
        }

        public static IntPtr Get(int id)
        {
            return _getSubsystem(id);
        }

        public static bool IsMapGroupReady()
        {
            return Get(0xE) != IntPtr.Zero;
        }
    }
}
