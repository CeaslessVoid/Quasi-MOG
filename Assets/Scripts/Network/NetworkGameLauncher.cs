using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Networking
{
    public static class NetworkGameLauncher
    {
        public const ushort DefaultPort = 7777;

        public static bool StartHost(ushort port = DefaultPort)
        {
            var transport = GetTransport();
            if (transport == null) return false;

            transport.SetConnectionData("0.0.0.0", port);
            return NetworkManager.Singleton.StartHost();
        }

        public static bool StartClient(string address, int port)
        {
            var transport = GetTransport();
            if (transport == null) return false;

            transport.SetConnectionData(address, (ushort)port);
            return NetworkManager.Singleton.StartClient();
        }

        public static void Shutdown()
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && (nm.IsHost || nm.IsClient || nm.IsServer))
                nm.Shutdown();
        }

        private static UnityTransport GetTransport()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("NetworkGameLauncher: no NetworkManager.Singleton found. Make sure the persistent NetworkManager object is in the Main Menu scene.");
                return null;
            }
            return NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        }
    }
}
