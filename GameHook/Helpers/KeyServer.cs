using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GameHook
{
    public static class KeyServer
    {
        private static string KDataPath()
        {
            string moduleDir = Path.GetDirectoryName(typeof(KeyServer).Assembly.Location);
            return Path.Combine(moduleDir ?? ".", "kdata.dat");
        }

        private static void HandleClient(Socket client)
        {
            string path = KDataPath();
            if (!File.Exists(path))
            {
                Log.Info($"[KeyServer] {path} not found");
                client.Close();
                return;
            }

            byte[] fileBytes = File.ReadAllBytes(path);
            int size = fileBytes.Length;

            // Send length prefix
            byte[] lenPrefix = BitConverter.GetBytes(size);
            client.Send(lenPrefix);

            int sent = 0;
            while (sent < size)
            {
                Thread.Sleep(80); // pacing
                int chunk = Math.Min(1416, size - sent);
                client.Send(fileBytes, sent, chunk, SocketFlags.None);
                sent += chunk;
            }

            Log.Info($"[KeyServer] sent {sent}/{size} bytes");

            Thread.Sleep(5000);
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        private static void ServerLoop()
        {
            try
            {
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, Targets.KeyServerPort);
                listener.Bind(endPoint);
                listener.Listen(10);

                Log.Info($"[KeyServer] listening on 0.0.0.0:{Targets.KeyServerPort}");

                while (true)
                {
                    Socket client = listener.Accept();
                    Log.Info("[KeyServer] client connected");
                    HandleClient(client);
                }
            }
            catch (Exception ex)
            {
                Log.Info($"[KeyServer] Exception: {ex.Message}");
            }
        }

        public static void StartBackgroundThread()
        {
            new Thread(ServerLoop) { IsBackground = true }.Start();
        }
    }
}
