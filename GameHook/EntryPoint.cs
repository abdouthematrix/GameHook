using System;
using System.Threading;
using EasyHook;

namespace GameHook
{
    public class Main : IEntryPoint
    {
        private HookInterface _interface;
        private GameConfig _cfg;

        public Main(RemoteHooking.IContext context, string channelName)
        {            
        }

        public void Run(RemoteHooking.IContext context, string channelName)
        {
            // Connect to host IPC channel
            _interface = RemoteHooking.IpcConnectClient<HookInterface>(channelName);
            // Initialize logging with the interface
            Log.Init(_interface);
            Log.Info("[Entry] CleanGameHook DLL loaded (C# EasyHook)");

            _cfg = Config.Load();                        

            if (!SafetyGuard.IsKnownMsgServerBuild())
            {
                Log.Info("[Entry] Aborting - unknown MsgServer.exe build, server will not start.");
                return;
            }

            ConfigPatches.Apply(_cfg);

            Log.Info("[HookManager] Installing early hooks...");
            ConnectRedirect.Install();
            KeyServer.StartBackgroundThread();
            IdentityCheck.RunIfEnabled(_cfg);
            SecretRandomizer.Apply();

            Log.Info("[Entry] Waiting for MapGroup initialization...");
            RemoteHooking.WakeUpProcess();
            GameSubsystem.Initialize();
            while (!GameSubsystem.IsMapGroupReady())
                Thread.Sleep(100);
            Log.Info("MapGroup->Initialized");

            GameHooks.InstallAll(_cfg);

            Log.Info("----Commands----");
            Log.Info("scroll destinations: tc, pc, ac, dc, bi, ma");
            Log.Info("Usage: /bring <playername>, /goto <playername>, /hideactions, /showactions");
            // Keep thread alive
            try
            {
                while (true) Thread.Sleep(100);
            }
            catch
            {
                
            }
            ConnectRedirect.UnInstall();
            GameHooks.UnInstallAll();
            // Finalise cleanup of hooks
            LocalHook.Release();
        }
    }
}
