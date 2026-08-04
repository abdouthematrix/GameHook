using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using EasyHook;

namespace GameHook
{
    public static class GameHooks
    {
        private static GameConfig _cfg;
        private static List<LocalHook> _hooks = new List<LocalHook>();

        // Delegates matching native signatures
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int GetBusinessDelegate(IntPtr self);
        private static GetBusinessDelegate _originalGetBusiness;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate byte GetRewardTimeDelegate(IntPtr self, uint key, IntPtr outPtr);
        private static GetRewardTimeDelegate _originalGetRewardTime;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate byte RegisterTableDelegate(IntPtr table);
        private static RegisterTableDelegate _originalRegisterTable;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate byte LockItemDelegate(IntPtr self);
        private static LockItemDelegate _originalLockItem;

        public static bool InstallAll(GameConfig cfg)
        {
            _cfg = cfg;
            Log.Info("[HookManager] Applying game hooks...");

            int hooked = 0, failed = 0;

            if (HookUtil.CreateAndEnable(Targets.CUser_GetBusiness, new GetBusinessDelegate(Detour_GetBusiness), out _originalGetBusiness, "CUser::GetBusiness"))
                hooked++;
            else failed++;

            if (HookUtil.CreateAndEnable(Targets.CSlotMgr_GetRewardTime, new GetRewardTimeDelegate(Detour_GetRewardTime), out _originalGetRewardTime, "CSlotMgr::GetRewardTime"))
                hooked++;
            else failed++;

            if (HookUtil.CreateAndEnable(Targets.CommandTableRegister, new RegisterTableDelegate(Detour_RegisterTable), out _originalRegisterTable, "Game::PMCommands"))
                hooked++;
            else failed++;

            if (HookUtil.CreateAndEnable(Targets.CItemMgr_LockItem, new LockItemDelegate(Detour_LockItem), out _originalLockItem, "CItemMgr::LockItem"))
                hooked++;
            else failed++;

            Log.Info($"[HookManager] Complete: {hooked} hooked, {failed} failed");
            return failed == 0;
        }

        public static void UnInstallAll()
        {
            foreach (var hook in _hooks)
            {
                try
                {
                    hook.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Info($"[HookManager] Failed to uninstall hook: {ex.Message}");
                }
            }
            _hooks.Clear();
        }

        private static int Detour_GetBusiness(IntPtr self)
        {
            if (_cfg.ForceMerchant)
                return 0xff;
            return _originalGetBusiness(self);
        }

        private static byte Detour_GetRewardTime(IntPtr self, uint key, IntPtr outPtr)
        {
            byte result = _originalGetRewardTime(self, key, outPtr);
            var bytes = new byte[24];
            Marshal.Copy(outPtr, bytes, 0, bytes.Length);

            if (_cfg.PrizeRewardMultiply != 300)
                BitConverter.GetBytes(_cfg.PrizeRewardMultiply).CopyTo(bytes, 8);

            if (_cfg.ExpRewardTime != 100)
            {
                int baseVal = BitConverter.ToInt32(bytes, 8);
                int newExp = (baseVal / 100) * _cfg.ExpRewardTime;
                BitConverter.GetBytes(newExp).CopyTo(bytes, 0x10);
            }

            Marshal.Copy(bytes, 0, outPtr, bytes.Length);
            return result;
        }

        private static byte Detour_RegisterTable(IntPtr table)
        {
            Commands.RegisterAll(table);
            return _originalRegisterTable(table);
        }

        private static byte Detour_LockItem(IntPtr self)
        {
            if (_cfg.LockAlwaysSucceeds)
                return 1;
            return _originalLockItem(self);
        }        
    }
}
