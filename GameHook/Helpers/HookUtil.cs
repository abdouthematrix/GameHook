using EasyHook;
using System;
using System.Runtime.InteropServices;

namespace GameHook
{
    public static class HookUtil
    {
        public static bool CreateAndEnable<T>(
            IntPtr target,
            T detour,
            out T original,
            out LocalHook hookOut,
            string name) where T : Delegate
        {
            try
            {
                var hook = LocalHook.Create(target, detour, null);
                hook.ThreadACL.SetExclusiveACL(new int[0]); // hook all threads

                // Use bypass address to avoid recursion
                original = (T)Marshal.GetDelegateForFunctionPointer(hook.HookBypassAddress, typeof(T));
                hookOut = hook;

                Log.Info($"  [OK] {name}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Info($"  [FAIL] {name}: {ex.Message}");
                original = null;
                hookOut = null;
                return false;
            }
        }
    }

}
