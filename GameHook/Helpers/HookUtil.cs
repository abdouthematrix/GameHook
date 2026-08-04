using EasyHook;
using System;
using System.Runtime.InteropServices;

namespace GameHook
{
    public static class HookUtil
    {
        /// <summary>
        /// Creates and enables a hook at the given target address.
        /// </summary>
        public static bool CreateAndEnable<T>(IntPtr target, T detour, out T original, string name) where T : Delegate
        {
            try
            {
                var hook = LocalHook.Create(target, detour, null);
                hook.ThreadACL.SetExclusiveACL(new int[0]); // hook all threads

                original = (T)Marshal.GetDelegateForFunctionPointer(target, typeof(T));

                Log.Info($"  [OK] {name}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Info($"  [FAIL] {name}: {ex.Message}");
                original = null;
                return false;
            }
        }
    }
}
