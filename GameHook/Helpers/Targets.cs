using System;
using System.Runtime.InteropServices;

namespace GameHook
{
    public static class Targets
    {
        // ---- Hook targets (absolute addresses in MsgServer.exe) ----
        public static readonly IntPtr CUser_GetBusiness = new IntPtr(0x140372ed0);
        public static readonly IntPtr CSlotMgr_GetRewardTime = new IntPtr(0x140255cf0);
        public static readonly IntPtr CommandTableRegister = new IntPtr(0x1402a3280);
        public static readonly IntPtr CItemMgr_LockItem = new IntPtr(0x1403f24a0);

        // ---- RegisterCommand API ----
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate int RegisterCommandDelegate(IntPtr table, string name, Delegate handler);
        public static readonly IntPtr RegisterCommand = new IntPtr(0x1402a1f10);

        // ---- Config patch addresses ----
        public static readonly IntPtr Data_MeteorFirstSocket = new IntPtr(0x140358472);
        public static readonly IntPtr Data_MeteorSecondSocket = new IntPtr(0x140358572);
        public static readonly IntPtr Data_QualitySocket = new IntPtr(0x1403577d7);
        public static readonly IntPtr Data_BlowfishKey = new IntPtr(0x14079ae58);

        // ---- Secret command prefixes ----
        public static readonly IntPtr Data_SecretString1 = new IntPtr(0x1406e8b00);
        public static readonly IntPtr Data_SecretString2 = new IntPtr(0x1406e8b18);

        // ---- ConnectRedirect port rules ----
        public const ushort KeyServerPort = 9869;       // 0x268d
        public const ushort RedirectAbovePort = 10000;  // 0x2710

        // ---- Commands helpers ----
        public static readonly IntPtr Teleport = new IntPtr(0x1401c55b0);
        public static readonly IntPtr GetSubsystem = new IntPtr(0x140008520);
        public static readonly IntPtr GetPlayerManagerFromSubsystem = new IntPtr(0x140087650);

        // ---- PMS gate patch ----
        public static readonly IntPtr PmsGateAddress = new IntPtr(0x140146154);
        public static readonly byte[] HideActionsBytes = { 0xE9, 0x83, 0x01, 0x00, 0x00, 0x90 };
        public static readonly byte[] ShowActionsBytes = { 0x0F, 0x84, 0x82, 0x01, 0x00, 0x00 };
    }
}
