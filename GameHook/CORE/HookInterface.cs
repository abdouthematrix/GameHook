using System;

namespace GameHook
{
    public class HookInterface : MarshalByRefObject
    {
        public event Action<string> MessageReceived;

        public override object InitializeLifetimeService() => null;

        public void Log(string message)
        {
            try
            {
                MessageReceived?.Invoke($"[DLL] {message}");               
            }
            catch { }
        }
    }

}
