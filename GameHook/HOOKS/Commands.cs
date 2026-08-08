using System;
using System.Runtime.InteropServices;

namespace GameHook
{
    // Thin wrapper around a CUser* pointer. Not a real struct-of-fields —
    // CUser's members are virtual methods, so each call still resolves a
    // vtable slot and invokes it. This just gives call sites a name instead
    // of passing raw IntPtrs through free functions.
    internal readonly struct CUser
    {
        private readonly IntPtr _ptr;

        private CUser(IntPtr ptr) => _ptr = ptr;

        // ctx from the command dispatcher is +0x10 off the real CUser object.
        public static CUser FromContext(IntPtr ctx) => new CUser(IntPtr.Add(ctx, 0x10));
        public static CUser FromObject(IntPtr obj) => new CUser(obj);

        private const int VTBL_IsGM = 0x230;
        private const int VTBL_Reply = 0x358;
        private const int VTBL_GetX = 0x40;
        private const int VTBL_GetY = 0x48;
        private const int VTBL_GetMapId = 0x168;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate bool BoolMethod(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void ReplyMethod(IntPtr self, string msg);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int IntMethod(IntPtr self);

        private IntPtr VtableSlot(int offset) => Marshal.ReadIntPtr(Marshal.ReadIntPtr(_ptr), offset);

        public bool IsGM()
        {
            var fn = (BoolMethod)Marshal.GetDelegateForFunctionPointer(VtableSlot(VTBL_IsGM), typeof(BoolMethod));
            return fn(_ptr);
        }

        public void Reply(string msg)
        {
            var fn = (ReplyMethod)Marshal.GetDelegateForFunctionPointer(VtableSlot(VTBL_Reply), typeof(ReplyMethod));
            fn(_ptr, msg);
        }

        public int GetX()
        {
            var fn = (IntMethod)Marshal.GetDelegateForFunctionPointer(VtableSlot(VTBL_GetX), typeof(IntMethod));
            return fn(_ptr);
        }

        public int GetY()
        {
            var fn = (IntMethod)Marshal.GetDelegateForFunctionPointer(VtableSlot(VTBL_GetY), typeof(IntMethod));
            return fn(_ptr);
        }

        public int GetMapId()
        {
            var fn = (IntMethod)Marshal.GetDelegateForFunctionPointer(VtableSlot(VTBL_GetMapId), typeof(IntMethod));
            return fn(_ptr);
        }

        // Lets a CUser be passed anywhere an IntPtr target is expected (e.g. Teleport).
        public static implicit operator IntPtr(CUser u) => u._ptr;
    }

    public static class Commands
    {
        // ---- Delegates for native functions ----
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void TeleportDelegate(IntPtr target, int mapId, int x, int y, byte flags);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetSubsystemDelegate(int id);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr FindPlayerDelegate(IntPtr manager, string name, int flags);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetPlayerManagerFromSubsystemDelegate(IntPtr subsystem);

        // Command handler delegate (six args, matching MsgServer dispatcher)
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate void CommandHandler(
            IntPtr p1, IntPtr p2, string args, IntPtr p4, IntPtr p5, IntPtr ctx);

        // ---- Native pointers exposed via Targets.cs ----
        private static readonly TeleportDelegate Teleport =
            (TeleportDelegate)Marshal.GetDelegateForFunctionPointer(Targets.Teleport, typeof(TeleportDelegate));

        private static readonly GetSubsystemDelegate GetSubsystem =
            (GetSubsystemDelegate)Marshal.GetDelegateForFunctionPointer(Targets.GetSubsystem, typeof(GetSubsystemDelegate));

        private static readonly GetPlayerManagerFromSubsystemDelegate GetPlayerManagerFromSubsystem =
            (GetPlayerManagerFromSubsystemDelegate)Marshal.GetDelegateForFunctionPointer(
                Targets.GetPlayerManagerFromSubsystem, typeof(GetPlayerManagerFromSubsystemDelegate));

        // ---- Helpers ----
        private static IntPtr FindPlayerByName(string name)
        {
            IntPtr subsystem = GetSubsystem(0xe);
            if (subsystem == IntPtr.Zero) return IntPtr.Zero;
            IntPtr manager = GetPlayerManagerFromSubsystem(subsystem);
            if (manager == IntPtr.Zero) return IntPtr.Zero;

            IntPtr fnPtr = Marshal.ReadIntPtr(Marshal.ReadIntPtr(manager), 0x90);
            var findPlayer = (FindPlayerDelegate)Marshal.GetDelegateForFunctionPointer(fnPtr, typeof(FindPlayerDelegate));
            return findPlayer(manager, name, 0);
        }

        // ---- Command Handlers ----
        public static void Cmd_Scroll(IntPtr p1, IntPtr p2, string args, IntPtr p4, IntPtr p5, IntPtr ctx)
        {
            try
            {
                var user = CUser.FromContext(ctx);
                if (!user.IsGM()) return;

                if (string.IsNullOrEmpty(args))
                {
                    user.Reply("scroll destinations: tc, pc, ac, dc, bi, ma");
                    return;
                }

                var dests = new[]
                {
                    new { Code = "tc", Map = 0x3ea, X = 0x12d, Y = 0x118 },
                    new { Code = "pc", Map = 0x3f3, X = 0xb8,  Y = 0x113 },
                    new { Code = "ac", Map = 0x3fc, X = 0x237, Y = 0x237 },
                    new { Code = "dc", Map = 0x3e8, X = 0x1f6, Y = 0x28f },
                    new { Code = "bi", Map = 0x3f7, X = 0x2d5, Y = 0x23e },
                    new { Code = "ma", Map = 0x40c, X = 0xd3,  Y = 0xc4 },
                };

                foreach (var d in dests)
                {
                    if (args.StartsWith(d.Code, StringComparison.OrdinalIgnoreCase))
                    {
                        // Teleport wants the outer ctx pointer, not the CUser object itself.
                        Teleport(ctx, d.Map, d.X, d.Y, 0);
                        return;
                    }
                }
                user.Reply("scroll destinations: tc, pc, ac, dc, bi, ma");
            }
            catch (Exception ex)
            {
                Log.Info($"[Cmd_Scroll] Exception: {ex}");
            }
        }

        public static void Cmd_Bring(IntPtr p1, IntPtr p2, string args, IntPtr p4, IntPtr p5, IntPtr ctx)
        {
            try
            {
                var self = CUser.FromContext(ctx);
                if (!self.IsGM()) return;

                if (string.IsNullOrEmpty(args)) { self.Reply("Usage: /bring <playername>"); return; }

                IntPtr target = FindPlayerByName(args);
                if (target == IntPtr.Zero) { self.Reply("Target not found."); return; }

                Teleport(target, self.GetMapId(), self.GetX(), self.GetY(), 0);
            }
            catch (Exception ex)
            {
                Log.Info($"[Cmd_Bring] Exception: {ex}");
            }
        }

        public static void Cmd_Goto(IntPtr p1, IntPtr p2, string args, IntPtr p4, IntPtr p5, IntPtr ctx)
        {
            try
            {
                var self = CUser.FromContext(ctx);
                if (!self.IsGM()) return;
                if (string.IsNullOrEmpty(args)) { self.Reply("Usage: /goto <playername>"); return; }

                IntPtr target = FindPlayerByName(args);
                if (target == IntPtr.Zero) { self.Reply("Target not found."); return; }

                var targetUser = CUser.FromObject(target);
                Teleport(ctx, targetUser.GetMapId(), targetUser.GetX(), targetUser.GetY(), 0);
            }
            catch (Exception ex)
            {
                Log.Info($"[Cmd_Goto] Exception: {ex}");
            }
        }

        public static void Cmd_HideActions(IntPtr p1, IntPtr p2, string args, IntPtr p4, IntPtr p5, IntPtr ctx)
        {
            try
            {
                var user = CUser.FromContext(ctx);
                if (!user.IsGM()) return;
                PatchBytes(Targets.PmsGateAddress, Targets.HideActionsBytes);
                user.Reply("Actions are now hidden from PMS");
            }
            catch (Exception ex)
            {
                Log.Info($"[Cmd_HideActions] Exception: {ex}");
            }
        }

        public static void Cmd_ShowActions(IntPtr p1, IntPtr p2, string args, IntPtr p4, IntPtr p5, IntPtr ctx)
        {
            try
            {
                var user = CUser.FromContext(ctx);
                if (!user.IsGM()) return;
                PatchBytes(Targets.PmsGateAddress, Targets.ShowActionsBytes);
                user.Reply("Actions are now visible to PMS");
            }
            catch (Exception ex)
            {
                Log.Info($"[Cmd_ShowActions] Exception: {ex}");
            }
        }

        // ---- Utility: PatchBytes ----
        private static void PatchBytes(IntPtr address, byte[] bytes)
        {
            uint oldProtect;
            VirtualProtect(address, (UIntPtr)bytes.Length, PAGE_EXECUTE_READWRITE, out oldProtect);
            Marshal.Copy(bytes, 0, address, bytes.Length);
            VirtualProtect(address, (UIntPtr)bytes.Length, oldProtect, out _);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        // ---- Registration ----
        public static void RegisterAll(IntPtr table)
        {
            var registerFn = (Targets.RegisterCommandDelegate)Marshal.GetDelegateForFunctionPointer(
                Targets.RegisterCommand, typeof(Targets.RegisterCommandDelegate));

            void Reg(string name, CommandHandler handler)
            {
                int ok = registerFn(table, name, handler);
                Log.Info(ok != 0 ? $"  [OK] {name}" : $"  [FAIL] {name} (already registered?)");
            }

            Reg("scroll", Cmd_Scroll);
            Reg("bring", Cmd_Bring);
            Reg("goto", Cmd_Goto);
            Reg("hideactions", Cmd_HideActions);
            Reg("showactions", Cmd_ShowActions);
        }
    }
}