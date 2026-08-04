using System;
using System.Runtime.InteropServices;

namespace GameHook
{
    public static class Commands
    {
        // ---- Delegates for native functions ----
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate bool BoolMethod(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void ReplyMethod(IntPtr self, string msg);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int IntMethod(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void TeleportDelegate(IntPtr target, int mapId, int x, int y, int flags);

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
        private static IntPtr Inner(IntPtr ctx) => IntPtr.Add(ctx, 0x10);

        private static IntPtr Vtable(IntPtr obj) => Marshal.ReadIntPtr(obj);

        private static bool HasGmPermission(IntPtr ctx)
        {
            IntPtr inner = Inner(ctx);
            IntPtr fnPtr = Marshal.ReadIntPtr(Vtable(inner), 0x230);
            var fn = (BoolMethod)Marshal.GetDelegateForFunctionPointer(fnPtr, typeof(BoolMethod));
            return fn(inner);
        }

        private static void Reply(IntPtr ctx, string msg)
        {
            IntPtr inner = Inner(ctx);
            IntPtr fnPtr = Marshal.ReadIntPtr(Vtable(inner), 0x358);
            var fn = (ReplyMethod)Marshal.GetDelegateForFunctionPointer(fnPtr, typeof(ReplyMethod));
            fn(inner, msg);
        }

        private static int GetX(IntPtr inner)
        {
            IntPtr fnPtr = Marshal.ReadIntPtr(Vtable(inner), 0x40);
            var fn = (IntMethod)Marshal.GetDelegateForFunctionPointer(fnPtr, typeof(IntMethod));
            return fn(inner);
        }

        private static int GetY(IntPtr inner)
        {
            IntPtr fnPtr = Marshal.ReadIntPtr(Vtable(inner), 0x48);
            var fn = (IntMethod)Marshal.GetDelegateForFunctionPointer(fnPtr, typeof(IntMethod));
            return fn(inner);
        }

        private static int GetMapId(IntPtr inner)
        {
            IntPtr fnPtr = Marshal.ReadIntPtr(Vtable(inner), 0x168);
            var fn = (IntMethod)Marshal.GetDelegateForFunctionPointer(fnPtr, typeof(IntMethod));
            return fn(inner);
        }

        private static IntPtr FindPlayerByName(string name)
        {
            IntPtr subsystem = GetSubsystem(0xe);
            if (subsystem == IntPtr.Zero) return IntPtr.Zero;
            IntPtr manager = GetPlayerManagerFromSubsystem(subsystem);
            if (manager == IntPtr.Zero) return IntPtr.Zero;

            IntPtr fnPtr = Marshal.ReadIntPtr(Vtable(manager), 0x90);
            var fn = (FindPlayerDelegate)Marshal.GetDelegateForFunctionPointer(fnPtr, typeof(FindPlayerDelegate));
            return fn(manager, name, 0);
        }

        // ---- Command Handlers ----
        public static void Cmd_Scroll(IntPtr p1, IntPtr p2, string args, IntPtr p4, IntPtr p5, IntPtr ctx)
        {
            try
            {
                if (!HasGmPermission(ctx)) return;
                IntPtr inner = Inner(ctx);

                if (string.IsNullOrEmpty(args))
                {
                    Reply(ctx, "scroll destinations: tc, pc, ac, dc, bi, ma");
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
                        Teleport(inner, d.Map, d.X, d.Y, 0);
                        return;
                    }
                }
                Reply(ctx, "scroll destinations: tc, pc, ac, dc, bi, ma");
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
                if (!HasGmPermission(ctx)) return;
                IntPtr inner = Inner(ctx);

                if (string.IsNullOrEmpty(args)) { Reply(ctx, "Usage: /bring <playername>"); return; }

                IntPtr target = FindPlayerByName(args);
                if (target == IntPtr.Zero) { Reply(ctx, "Target not found."); return; }

                Teleport(target, GetMapId(inner), GetX(inner), GetY(inner), 0);
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
                if (!HasGmPermission(ctx)) return;
                if (string.IsNullOrEmpty(args)) { Reply(ctx, "Usage: /goto <playername>"); return; }

                IntPtr target = FindPlayerByName(args);
                if (target == IntPtr.Zero) { Reply(ctx, "Target not found."); return; }

                IntPtr targetInner = Inner(target);
                Teleport(ctx, GetMapId(targetInner), GetX(targetInner), GetY(targetInner), 0);
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
                if (!HasGmPermission(ctx)) return;
                PatchBytes(Targets.PmsGateAddress, Targets.HideActionsBytes);
                Reply(ctx, "Actions are now hidden from PMS");
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
                if (!HasGmPermission(ctx)) return;
                PatchBytes(Targets.PmsGateAddress, Targets.ShowActionsBytes);
                Reply(ctx, "Actions are now visible to PMS");
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

            registerFn(table, "scroll", new CommandHandler(Cmd_Scroll));
            registerFn(table, "bring", new CommandHandler(Cmd_Bring));
            registerFn(table, "goto", new CommandHandler(Cmd_Goto));
            registerFn(table, "hideactions", new CommandHandler(Cmd_HideActions));
            registerFn(table, "showactions", new CommandHandler(Cmd_ShowActions));

            Log.Info("[Commands] Registered 5 in-game commands");
        }
    }
}
