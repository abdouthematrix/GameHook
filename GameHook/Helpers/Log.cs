using System;
using System.IO;

namespace GameHook
{
    public static class Log
    {
        private static readonly object _lock = new object();
        private static StreamWriter _file;
        private static HookInterface _interface;

        public static void Init(HookInterface hookInterface)
        {
            lock (_lock)
            {
                if (_file != null) return;

                _interface = hookInterface;

                string moduleDir = Path.GetDirectoryName(typeof(Log).Assembly.Location);
                string path = Path.Combine(moduleDir ?? ".", "GameHook.log");

                _file = new StreamWriter(path, append: true) { AutoFlush = true };

                Info("[Log] Initialized logging");
            }
        }

        public static void Info(string message)
        {
            lock (_lock)
            {
                string consoleLine = $"[{DateTime.Now:HH:mm:ss}] {message}";

                // Send to interface if available
                _interface?.Log(consoleLine);

                if (_file != null)
                {
                    string fileLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                    _file.WriteLine(fileLine);
                }
            }
        }

        // Overload to support formatted strings like C++ printf
        public static void Info(string fmt, params object[] args)
        {
            Info(string.Format(fmt, args));
        }
    }
}
